// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.RuntimeDetour;
using Reprimand.CodeAnalysis;
using Reprimand.Lifecycle;

namespace Reprimand.Graphics;

/// <summary>
/// Tracks and manages the game's global <see cref="Draw.SpriteBatch"/>.
/// </summary>
/// <remarks>
/// <para>
/// Managed (scoped) batches may be nested. Beginning a nested batch ends the current batch, begins
/// the nested batch, and restores the displaced batch when the nested scope is disposed.
/// </para>
/// <para>
/// Beginning/ending the <see cref="Draw.SpriteBatch"/> normally during a managed batch scope is
/// prohibited; you're expected to use the managed API for your own drawing code and not "leak" it
/// into code that uses regular spritebatch APIs. If you must call some method that starts/ends the
/// batch on its own in the middle of a managed batch, then you can use a suspension, which allows
/// unscoped batch work to happen and then safely resumes the managed batch; see <see cref="Suspend()"/>.
/// </para>
/// <para>
/// This class is not thread safe; all operations must occur on the thread that owns the
/// <see cref="GraphicsDevice"/>, i.e. the main game thread.
/// </para>
/// </remarks>
public static class GlobalSpriteBatch {
	private enum ScopeKind : byte {
		Batch,
		Suspension,
	}

	private enum LifecycleOperation : byte {
		None,
		Begin,
		End,
	}

	internal readonly struct ScopeToken(ulong generation, ulong sequence) {
		public ulong Generation { get; } = generation;
		public ulong Sequence { get; } = sequence;
	}

	private readonly struct ScopeFrame(ScopeToken token, ScopeKind kind, BatchParameters? previousParameters) {
		public ScopeToken Token { get; } = token;
		public ScopeKind Kind { get; } = kind;
		public BatchParameters? PreviousParameters { get; } = previousParameters;
	}

	private delegate void orig_SpriteBatch_Begin(
		SpriteBatch self,
		SpriteSortMode sortMode,
		BlendState? blendState,
		SamplerState? samplerState,
		DepthStencilState? depthStencilState,
		RasterizerState? rasterizerState,
		Effect? effect,
		Matrix transformMatrix
	);

	private delegate void orig_SpriteBatch_End(SpriteBatch self);

	private static readonly List<ScopeFrame> scopeStack = new();

	private static SpriteBatch? trackedBatch;

	private static BatchParameters? currParams;

	private static ulong scopeGeneration = 1;
	private static ulong nextScopeSeq;

	private static bool transitioning;
	private static LifecycleOperation expectedLifecycleOp;
	private static Exception? poisonReason;

	private static Hook? beginHook;
	private static Hook? endHook;

	/// <summary>
	/// The effective parameters of a spritebatch.
	/// </summary>
	/// <param name="sortMode">
	/// The order in which submitted sprites are rendered.
	/// </param>
	/// <param name="blendState">
	/// The blend state, or <see langword="null"/> for <see cref="BlendState.AlphaBlend"/>.
	/// </param>
	/// <param name="samplerState">
	/// The sampler state, or <see langword="null"/> for <see cref="SamplerState.LinearClamp"/>.
	/// </param>
	/// <param name="depthStencilState">
	/// The depth/stencil state, or <see langword="null"/> for <see cref="DepthStencilState.None"/>.
	/// </param>
	/// <param name="rasterizerState">
	/// The rasterizer state, or <see langword="null"/> for <see cref="RasterizerState.CullCounterClockwise"/>.
	/// Even though you usually don't want culling in a 2D game, this mirrors FNA's default.
	/// </param>
	/// <param name="effect">
	/// The custom effect, if any.
	/// </param>
	/// <param name="transformMatrix">
	/// The transformation matrix, or <see langword="null"/> for <see cref="Matrix.Identity"/>.
	/// </param>
	/// <remarks>
	/// Null render states are replaced with FNA's normal default states. A default-initialized
	/// value also represents the default <see cref="SpriteBatch.Begin()"/> parameters.
	/// </remarks>
	public readonly struct BatchParameters(
		SpriteSortMode sortMode = SpriteSortMode.Deferred,
		BlendState? blendState = null,
		SamplerState? samplerState = null,
		DepthStencilState? depthStencilState = null,
		RasterizerState? rasterizerState = null,
		Effect? effect = null,
		Matrix? transformMatrix = null
	) {
		private readonly bool inited = true;
		private readonly SpriteSortMode sortMode = sortMode;
		private readonly BlendState? blendState = blendState ?? BlendState.AlphaBlend;
		private readonly SamplerState? samplerState = samplerState ?? SamplerState.LinearClamp;
		private readonly DepthStencilState? depthStencilState = depthStencilState ?? DepthStencilState.None;
		private readonly RasterizerState? rasterizerState = rasterizerState ?? RasterizerState.CullCounterClockwise;
		private readonly Effect? effect = effect;
		private readonly Matrix transformMatrix = transformMatrix ?? Matrix.Identity;

		/// <summary>
		/// The sprite sorting mode.
		/// </summary>
		public SpriteSortMode SortMode => inited ? sortMode : SpriteSortMode.Deferred;

		/// <summary>
		/// The effective blend state.
		/// </summary>
		public BlendState BlendState => blendState ?? BlendState.AlphaBlend;

		/// <summary>
		/// The effective sampler state.
		/// </summary>
		public SamplerState SamplerState => samplerState ?? SamplerState.LinearClamp;

		/// <summary>
		/// The effective depth/stencil state.
		/// </summary>
		public DepthStencilState DepthStencilState => depthStencilState ?? DepthStencilState.None;

		/// <summary>
		/// The effective rasterizer state.
		/// </summary>
		public RasterizerState RasterizerState => rasterizerState ?? RasterizerState.CullCounterClockwise;

		/// <summary>
		/// The custom effect, if any.
		/// </summary>
		public Effect? Effect => effect;

		/// <summary>
		/// The transformation matrix.
		/// </summary>
		public Matrix TransformMatrix => inited ? transformMatrix : Matrix.Identity;

		internal BatchParameters Normalize() =>
			new(SortMode, BlendState, SamplerState, DepthStencilState, RasterizerState, Effect, TransformMatrix);
		internal void Apply(SpriteBatch spriteBatch) =>
			spriteBatch.Begin(SortMode, BlendState, SamplerState, DepthStencilState, RasterizerState, Effect, TransformMatrix);
	}

	/// <summary>
	/// A managed spritebatch scope.
	/// </summary>
	/// <remarks>
	/// Scopes must be disposed in reverse creation order. Copies of this value do not share
	/// disposed state; disposing a copy throws.
	/// </remarks>
	public readonly ref struct BatchScope {
		private readonly ScopeToken token;
		internal BatchScope(ScopeToken token, BatchParameters parameters) {
			this.token = token;
			Parameters = parameters;
		}

		/// <summary>
		/// The parameters used by this scope.
		/// </summary>
		public BatchParameters Parameters { get; }

		/// <summary>
		/// Ends this scope's batch and restores the batch displaced when the scope was created.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the scope is not the innermost active scope, has already been disposed, or
		/// the <see cref="GlobalSpriteBatch"/> class is poisoned.
		/// </exception>
		public void Dispose() => disposeScope(token, ScopeKind.Batch);
	}

	/// <summary>
	/// A temporary suspension of an active spritebatch.
	/// </summary>
	/// <remarks>
	/// A <see langword="default"/> value is a no-op. Scopes must be disposed in reverse creation order.
	/// </remarks>
	public readonly ref struct Suspension {
		private readonly ScopeToken token;
		internal Suspension(ScopeToken token, BatchParameters? suspendedParameters) {
			this.token = token;
			SuspendedParameters = suspendedParameters;
		}

		/// <summary>
		/// Whether this scope actually suspended a batch.
		/// </summary>
		public bool WasActive => token.Generation != 0;

		/// <summary>
		/// The parameters of the suspended batch, or <see langword="null"/> for a no-op suspension.
		/// </summary>
		public BatchParameters? SuspendedParameters { get; }

		/// <summary>
		/// Restarts the suspended batch.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the scope is not the innermost active scope, has already been disposed, or
		/// the <see cref="GlobalSpriteBatch"/> class is poisoned.
		/// </exception>
		public void Dispose() => disposeScope(token, ScopeKind.Suspension);
	}

	/// <summary>
	/// Whether the spritebatch is currently active.
	/// </summary>
	public static bool IsActive => currParams.HasValue;

	/// <summary>
	/// Whether the spritebatch is currently active and owned by a managed <see cref="BatchScope"/>.
	/// </summary>
	public static bool IsManagedActive => currParams.HasValue && scopeStack.Count != 0 && scopeStack[^1].Kind == ScopeKind.Batch;

	/// <summary>
	/// The tracked global spritebatch.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the global spritebatch has not been initialized or changed after tracking began.
	/// </exception>
	[DontUseInStaticCtor]
	public static SpriteBatch Batch => getTrackedBatch();

	/// <summary>
	/// The effective parameters of the active spritebatch, or <see langword="null"/> if there is
	/// no currently active batch.
	/// </summary>
	public static BatchParameters? CurrentParameters => currParams;

	/// <summary>
	/// Whether an internal invariant failure has disabled managed operations.
	/// </summary>
	public static bool IsPoisoned => poisonReason is not null;

	/// <summary>
	/// The invariant failure which poisoned this class, if any.
	/// </summary>
	public static Exception? PoisonReason => poisonReason;

	[OnLoad(UndoMethod = nameof(UnregisterHooks))]
	internal static void RegisterHooks() {
		try {
			beginHook = new Hook(
				typeof(SpriteBatch).GetMethod(
					nameof(SpriteBatch.Begin),
					BindingFlags.Public | BindingFlags.Instance,
					binder: null,
					types: [
						typeof(SpriteSortMode),
						typeof(BlendState),
						typeof(SamplerState),
						typeof(DepthStencilState),
						typeof(RasterizerState),
						typeof(Effect),
						typeof(Matrix),
					],
					modifiers: null
				) ?? throw new MissingMethodException(nameof(SpriteBatch), nameof(SpriteBatch.Begin)),
				on_SpriteBatch_Begin
			);
			endHook = new Hook(
				typeof(SpriteBatch).GetMethod(
					nameof(SpriteBatch.End),
					BindingFlags.Public | BindingFlags.Instance,
					binder: null,
					types: Type.EmptyTypes,
					modifiers: null
				) ?? throw new MissingMethodException(nameof(SpriteBatch), nameof(SpriteBatch.End)),
				on_SpriteBatch_End
			);
		} catch {
			endHook?.Dispose();
			endHook = null;
			beginHook?.Dispose();
			beginHook = null;
			trackedBatch = null;
			throw;
		}
	}

	internal static void UnregisterHooks() {
		if (transitioning)
			global::Celeste.Mod.Logger.Log(global::Celeste.Mod.LogLevel.Warn, "Reprimand/GlobalSpriteBatch", "unregistering hooks happened during a spritebatch transition");
		if (scopeStack.Count != 0)
			global::Celeste.Mod.Logger.Log(global::Celeste.Mod.LogLevel.Warn, "Reprimand/GlobalSpriteBatch", "unregistering hooks happened while managed scopes are active");
		if (currParams.HasValue)
			global::Celeste.Mod.Logger.Log(global::Celeste.Mod.LogLevel.Warn, "Reprimand/GlobalSpriteBatch", "unregistering hooks happened while the global spritebatch is active");

		try {
			beginHook?.Dispose();
			beginHook = null;
			endHook?.Dispose();
			endHook = null;
		} finally {
			invalidateScopeGeneration();
			trackedBatch = null;
			currParams = null;
			expectedLifecycleOp = LifecycleOperation.None;
			poisonReason = null;
		}
	}


	/// <summary>
	/// Attempts to obtain the effective parameters of the active spritebatch.
	/// </summary>
	/// <param name="params">
	/// If the method returned <see langword="true"/>, the active parameters.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the tracked spritebatch is active; otherwise, <see langword="false"/>.
	/// </returns>
	public static bool TryGetCurrentParameters(out BatchParameters @params) {
		if (currParams is { } curr) {
			@params = curr;
			return true;
		}
		@params = default;
		return false;
	}

	/// <summary>
	/// Begins a managed spritebatch scope.
	/// </summary>
	/// <param name="parameters">
	/// The parameters for the new batch.
	/// </param>
	/// <returns>
	/// A scope which ends this batch and restores the displaced batch when disposed.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown if a transition is already in progress, the class is poisoned, or the global
	/// spritebatch is unavailable.
	/// </exception>
	[DontUseInStaticCtor]
	public static BatchScope Begin(scoped in BatchParameters parameters) {
		throwIfManagedOpUnavailable();
		SpriteBatch batch = getTrackedBatch();
		BatchParameters effectiveParams = parameters.Normalize();
		BatchParameters? previousParams = currParams;
		ScopeToken token = nextScopeToken();
		transitioning = true;
		try {
			if (previousParams.HasValue) {
				Exception? endEx = tryEnd(batch);
				if (endEx is not null) {
					Exception? restoreException = tryBegin(batch, previousParams.Value);
					if (restoreException is not null) {
						abortCurrentGeneration(batch);
						throw new AggregateException(
							"ending the displaced SpriteBatch failed and restoring it also failed",
							endEx,
							restoreException
						);
					}
					rethrow(endEx);
				}
			}

			Exception? beginEx = tryBegin(batch, effectiveParams);
			if (beginEx is not null) {
				normalizeInactive(batch);
				if (previousParams.HasValue) {
					Exception? restoreEx = tryBegin(batch, previousParams.Value);
					if (restoreEx is not null) {
						abortCurrentGeneration(batch);
						throw new AggregateException(
							"beginning the nested SpriteBatch failed and restoring the displaced batch also failed",
							beginEx,
							restoreEx
						);
					}
				}
				rethrow(beginEx);
			}

			scopeStack.Add(new ScopeFrame(token, ScopeKind.Batch, previousParams));
			return new BatchScope(token, effectiveParams);
		} finally {
			transitioning = false;
		}
	}

	/// <summary>
	/// Begins a managed spritebatch scope.
	/// </summary>
	/// <param name="sortMode">
	/// The order in which submitted sprites are rendered.
	/// </param>
	/// <param name="blendState">
	/// The blend state, or <see langword="null"/> for <see cref="BlendState.AlphaBlend"/>.
	/// </param>
	/// <param name="samplerState">
	/// The sampler state, or <see langword="null"/> for <see cref="SamplerState.LinearClamp"/>.
	/// </param>
	/// <param name="depthStencilState">
	/// The depth/stencil state, or <see langword="null"/> for <see cref="DepthStencilState.None"/>.
	/// </param>
	/// <param name="rasterizerState">
	/// The rasterizer state, or <see langword="null"/> for <see cref="RasterizerState.CullCounterClockwise"/>.
	/// Even though you usually don't want culling in a 2D game, this mirrors FNA's default.
	/// </param>
	/// <param name="effect">
	/// The custom effect, if any.
	/// </param>
	/// <param name="transformMatrix">
	/// The transformation matrix, or <see langword="null"/> for <see cref="Matrix.Identity"/>.
	/// </param>
	/// <returns>
	/// A scope which ends this batch and restores the displaced batch when disposed.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown if a transition is already in progress, the class is poisoned, or the global
	/// spritebatch is unavailable.
	/// </exception>
	[DontUseInStaticCtor]
	public static BatchScope Begin(
		SpriteSortMode sortMode = SpriteSortMode.Deferred,
		BlendState? blendState = null,
		SamplerState? samplerState = null,
		DepthStencilState? depthStencilState = null,
		RasterizerState? rasterizerState = null,
		Effect? effect = null,
		Matrix? transformMatrix = null
	) => Begin(new BatchParameters(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix));

	/// <summary>
	/// Begins an ordinary, non-scoped spritebatch.
	/// </summary>
	/// <param name="params">
	/// The parameters for the new batch.
	/// </param>
	/// <remarks>
	/// The caller must eventually call <see cref="EndCurrent"/>.
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the spritebatch is already active, a managed batch scope is active, a transition is
	/// already in progress, or the class is poisoned.
	/// </exception>
	[DontUseInStaticCtor]
	public static void BeginUnscoped(in BatchParameters @params) {
		throwIfManagedOpUnavailable();
		throwIfUnscopedLifecycleUnavailable();
		if (currParams.HasValue)
			throw new InvalidOperationException("the global SpriteBatch is already active");
		SpriteBatch batch = getTrackedBatch();
		BatchParameters effectiveParams = @params.Normalize();
		transitioning = true;
		try {
			Exception? beginEx = tryBegin(batch, effectiveParams);
			if (beginEx is null)
				return;
			normalizeInactive(batch);
			rethrow(beginEx);
		} finally {
			transitioning = false;
		}
	}

	/// <summary>
	/// Begins an ordinary, non-scoped spritebatch.
	/// </summary>
	/// <remarks>
	/// The caller must eventually call <see cref="EndCurrent"/>.
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the spritebatch is already active, a managed scope is active, a transition is
	/// already in progress, or the class is poisoned.
	/// </exception>
	[DontUseInStaticCtor]
	public static void BeginUnscoped(
		SpriteSortMode sortMode = SpriteSortMode.Deferred,
		BlendState? blendState = null,
		SamplerState? samplerState = null,
		DepthStencilState? depthStencilState = null,
		RasterizerState? rasterizerState = null,
		Effect? effect = null,
		Matrix? transformMatrix = null
	) => BeginUnscoped(new BatchParameters(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix));

	/// <summary>
	/// Ends the currently active unscoped batch.
	/// </summary>
	/// <returns>
	/// The parameters of the batch which was ended.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown if no batch is active, a managed batch scope is active, a transition is already in
	/// progress, or the class is poisoned.
	/// </exception>
	[DontUseInStaticCtor]
	public static BatchParameters EndCurrent() {
		throwIfManagedOpUnavailable();
		throwIfUnscopedLifecycleUnavailable();
		BatchParameters @params = currParams ?? throw new InvalidOperationException("the global SpriteBatch is not active");
		SpriteBatch batch = getTrackedBatch();
		transitioning = true;
		try {
			Exception? endEx = tryEnd(batch);
			if (endEx is not null)
				rethrow(endEx);
			return @params;
		} finally {
			transitioning = false;
		}
	}

	/// <summary>
	/// Ends and restarts the current unscoped batch using the same parameters.
	/// </summary>
	/// <remarks>
	/// This flushes the current batch and establishes a new batching boundary.
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// Thrown if no batch is active, a managed batch scope is active, a transition is already in
	/// progress, or the class is poisoned.
	/// </exception>
	[DontUseInStaticCtor]
	public static void RestartCurrent() {
		throwIfManagedOpUnavailable();
		throwIfUnscopedLifecycleUnavailable();
		BatchParameters @params = currParams ?? throw new InvalidOperationException("the global SpriteBatch is not active");
		SpriteBatch batch = getTrackedBatch();
		transitioning = true;
		try {
			Exception? endEx = tryEnd(batch);
			if (endEx is not null) {
				Exception? restoreEx = tryBegin(batch, @params);
				if (restoreEx is not null) {
					abortCurrentGeneration(batch);
					throw new AggregateException(
						"ending the SpriteBatch failed and restoring it also failed",
						endEx,
						restoreEx
					);
				}
				rethrow(endEx);
			}

			Exception? beginEx = tryBegin(batch, @params);
			if (beginEx is null)
				return;
			normalizeInactive(batch);

			Exception? retryEx = tryBegin(batch, @params);
			if (retryEx is not null) {
				abortCurrentGeneration(batch);
				throw new AggregateException(
					"restarting the SpriteBatch failed and restoring it also failed",
					beginEx,
					retryEx
				);
			}

			rethrow(beginEx);
		} finally {
			transitioning = false;
		}
	}

	/// <summary>
	/// Suspends the active spritebatch by ending it and returning a scope which restarts it when disposed.
	/// </summary>
	/// <returns>
	/// A scope which restores the suspended batch when disposed.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown if no spritebatch is active, a transition is already in progress, or the class is poisoned.
	/// </exception>
	[DontUseInStaticCtor]
	public static Suspension Suspend() {
		throwIfManagedOpUnavailable();
		BatchParameters @params = currParams ?? throw new InvalidOperationException("the global SpriteBatch is not active");
		return suspendCore(@params);
	}

	/// <summary>
	/// Suspends the active spritebatch by ending it and returning a scope which restarts it when disposed.
	/// </summary>
	/// <returns>
	/// A scope which restores the suspended batch when disposed, or a no-op scope if no batch is active.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown if a transition is already in progress or the class is poisoned.
	/// </exception>
	[DontUseInStaticCtor]
	public static Suspension SuspendIfActive() {
		throwIfManagedOpUnavailable();
		return currParams is { } @params ? suspendCore(@params) : default;
	}

	private static Suspension suspendCore(scoped in BatchParameters @params) {
		SpriteBatch batch = getTrackedBatch();
		ScopeToken token = nextScopeToken();
		transitioning = true;
		try {
			Exception? endEx = tryEnd(batch);
			if (endEx is not null) {
				Exception? restoreEx = tryBegin(batch, @params);
				if (restoreEx is not null) {
					abortCurrentGeneration(batch);
					throw new AggregateException(
						"suspending the SpriteBatch failed and restoring it also failed",
						endEx,
						restoreEx
					);
				}
				rethrow(endEx);
			}

			scopeStack.Add(new ScopeFrame(token, ScopeKind.Suspension, @params));
			return new Suspension(token, @params);
		} finally {
			transitioning = false;
		}
	}

	private static void disposeScope(ScopeToken token, ScopeKind expectedKind) {
		InternalStateException ise;
		if (token.Generation == 0)
			return;
		if (token.Generation != scopeGeneration)
			return;
		throwIfManagedOpUnavailable();

		int idx = scopeStack.Count - 1;
		if (idx < 0)
			throw new InvalidOperationException("the SpriteBatch scope has already been disposed");

		ScopeFrame frame = scopeStack[idx];
		if (frame.Token.Generation != token.Generation || frame.Token.Sequence != token.Sequence)
			throw new InvalidOperationException("SpriteBatch scopes must be disposed in LIFO order");
		if (frame.Kind != expectedKind) {
			ise = new InternalStateException("SpriteBatch scope kind doesn't match its stack frame");
			poison(ise);
			throw ise;
		}

		SpriteBatch batch = getTrackedBatch();
		transitioning = true;
		try {
			switch (frame.Kind) {
			case ScopeKind.Batch:
				if (!currParams.HasValue) {
					ise = new InternalStateException("the batch owned by the current scope is unexpectedly inactive");
					poison(ise);
					throw ise;
				}

				Exception? endEx = tryEnd(batch);
				Exception? restoreEx = null;
				if (frame.PreviousParameters.HasValue)
					restoreEx = tryBegin(batch, frame.PreviousParameters.Value);

				if (restoreEx is not null) {
					abortCurrentGeneration(batch);
					if (endEx is not null) {
						throw new AggregateException(
							"ending the scoped SpriteBatch failed and restoring its displaced batch also failed",
							endEx,
							restoreEx
						);
					}
					rethrow(restoreEx);
				}

				scopeStack.RemoveAt(idx);

				if (endEx is not null)
					rethrow(endEx);

				break;
			case ScopeKind.Suspension:
				if (currParams.HasValue)
					throw new InvalidOperationException("can't dispose a SpriteBatch suspension while an unscoped batch is active");

				if (!frame.PreviousParameters.HasValue) {
					ise = new InternalStateException("expected suspension to have PreviousParameters to restore");
					poison(ise);
					throw ise;
				}

				Exception? resumeEx = tryBegin(batch, frame.PreviousParameters.Value);
				if (resumeEx is not null) {
					abortCurrentGeneration(batch);
					rethrow(resumeEx);
				}

				scopeStack.RemoveAt(idx);
				break;
			default:
				ise = new InternalStateException("out of range ScopeKind enum value");
				poison(ise);
				throw ise;
			}
		} finally {
			transitioning = false;
		}
	}

	private static void on_SpriteBatch_Begin(
		orig_SpriteBatch_Begin orig,
		SpriteBatch self,
		SpriteSortMode sortMode,
		BlendState? blendState,
		SamplerState? samplerState,
		DepthStencilState? depthStencilState,
		RasterizerState? rasterizerState,
		Effect? effect,
		Matrix transformMatrix
	) {
		bool tracked = isTracked(self);
		bool managedCall = tracked && transitioning && expectedLifecycleOp == LifecycleOperation.Begin;
		if (tracked) {
			if (transitioning && !managedCall) {
				InternalStateException ise = new("reentrant SpriteBatch lifecycle operation happened during managed transition");
				poison(ise);
				throw ise;
			}
			if (!transitioning && !isUnscopedLifecycleAllowed())
				throw new InvalidOperationException("direct SpriteBatch.Begin calls are prohibited while a managed scope is active");
		}
		if (managedCall)
			expectedLifecycleOp = LifecycleOperation.None;
		try {
			orig(self, sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix);
		} catch {
			// a failed immediate mode Begin might've set FNA's internal active flag before failing
			if (tracked && !transitioning && !currParams.HasValue) {
				try {
					self.End();
				} catch {
					// FNA SpriteBatch.End leaves the SpriteBatch inactive whether it fails
					// because the batch was already inactive or because the flush failed
				}
				currParams = null;
			}
			throw;
		}
		if (!tracked)
			return;
		currParams = new BatchParameters(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix);
	}

	private static void on_SpriteBatch_End(orig_SpriteBatch_End orig, SpriteBatch self) {
		bool tracked = isTracked(self);
		bool managedCall = tracked && transitioning && expectedLifecycleOp == LifecycleOperation.End;
		if (tracked) {
			if (transitioning && !managedCall) {
				InternalStateException ise = new("reentrant SpriteBatch lifecycle operation happened during managed transition");
				poison(ise);
				throw ise;
			}
			if (!transitioning && !isUnscopedLifecycleAllowed())
				throw new InvalidOperationException("direct SpriteBatch.End calls are prohibited while a managed scope is active");
		}
		if (managedCall)
			expectedLifecycleOp = LifecycleOperation.None;
		if (!tracked) {
			orig(self);
			return;
		}
		try {
			orig(self);
		} finally {
			// FNA clears its internal active flag before flushing, so the batch is inactive even if
			// said flushing throws
			currParams = null;
		}
	}

	private static void invokeBegin(SpriteBatch batch, in BatchParameters @params) {
		if (expectedLifecycleOp != LifecycleOperation.None) {
			InternalStateException ise = new("already had a pending SpriteBatch lifecycle operation");
			poison(ise);
			throw ise;
		}
		expectedLifecycleOp = LifecycleOperation.Begin;
		try {
			@params.Apply(batch);
		} finally {
			expectedLifecycleOp = LifecycleOperation.None;
		}
	}

	private static void invokeEnd(SpriteBatch batch) {
		if (expectedLifecycleOp != LifecycleOperation.None) {
			InternalStateException ise = new("already had a pending SpriteBatch lifecycle operation");
			poison(ise);
			throw ise;
		}
		expectedLifecycleOp = LifecycleOperation.End;
		try {
			batch.End();
		} finally {
			expectedLifecycleOp = LifecycleOperation.None;
		}
	}

	private static Exception? tryBegin(SpriteBatch batch, in BatchParameters @params) {
		try {
			invokeBegin(batch, @params);
			return null;
		} catch (Exception ex) {
			return ex;
		}
	}

	private static Exception? tryEnd(SpriteBatch batch) {
		try {
			invokeEnd(batch);
			return null;
		} catch (Exception ex) {
			return ex;
		}
	}

	private static void normalizeInactive(SpriteBatch batch) {
		try {
			invokeEnd(batch);
		} catch {
			// FNA's lifecycle state is set to inactive after End whether it throws because no
			// batch was active or because of an actual failure to flush an active batch
			currParams = null;
		}
	}

	private static void abortCurrentGeneration(SpriteBatch spriteBatch) {
		normalizeInactive(spriteBatch);
		invalidateScopeGeneration();
	}

	private static void invalidateScopeGeneration() {
		scopeStack.Clear();
		checked {
			scopeGeneration++;
		}
		nextScopeSeq = 0;
	}

	private static void poison(Exception ex) {
		poisonReason ??= ex;
		currParams = null;
		expectedLifecycleOp = LifecycleOperation.None;
		invalidateScopeGeneration();
	}

	private static bool isTracked(SpriteBatch batch) {
		SpriteBatch? global = Draw.SpriteBatch;
		if (global is null) {
			if (trackedBatch is not null) {
				InvalidOperationException ex = new("Draw.SpriteBatch became null after tracking began");
				poison(ex);
				throw ex;
			}
			return false;
		}
		if (trackedBatch is null) {
			trackedBatch = global;
		} else if (!ReferenceEquals(trackedBatch, global)) {
			InvalidOperationException ex = new("Draw.SpriteBatch changed after tracking began");
			poison(ex);
			throw ex;
		}
		return ReferenceEquals(batch, global);
	}

	private static SpriteBatch getTrackedBatch() {
		SpriteBatch batch = Draw.SpriteBatch ?? throw new InvalidOperationException("Draw.SpriteBatch is null (uninitialized?)");
		if (trackedBatch is null) {
			trackedBatch = batch;
		} else if (!ReferenceEquals(trackedBatch, batch)) {
			InvalidOperationException ex = new("Draw.SpriteBatch changed after tracking began");
			poison(ex);
			throw ex;
		}
		return batch;
	}

	private static void throwIfManagedOpUnavailable() {
		ReprimandModule.ThrowIfInactive();
		if (poisonReason is not null)
			throw new InvalidOperationException("GlobalSpriteBatch is poisoned by an earlier internal/invariant error, sorry", poisonReason);
		if (!transitioning)
			return;
		InvalidOperationException ex = new("reentrant GlobalSpriteBatch transition was attempted");
		poison(ex);
		throw ex;
	}

	private static void throwIfManagedScopesActive() {
		if (scopeStack.Count != 0)
			throw new InvalidOperationException("this operation is prohibited while managed GlobalSpriteBatch scopes are active");
	}

	private static bool isUnscopedLifecycleAllowed() => scopeStack.Count == 0 || scopeStack[^1].Kind == ScopeKind.Suspension;

	private static void throwIfUnscopedLifecycleUnavailable() {
		if (!isUnscopedLifecycleAllowed())
			throw new InvalidOperationException("unscoped SpriteBatch lifecycle operations are prohibited while a managed batch scope is active.");
	}

	private static ScopeToken nextScopeToken() => new(scopeGeneration, checked(++nextScopeSeq));

	[DoesNotReturn]
	private static void rethrow(Exception exception) {
		ExceptionDispatchInfo.Capture(exception).Throw();
		throw new UnreachableException();
	}
}
