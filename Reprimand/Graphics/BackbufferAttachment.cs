// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;

using Reprimand.Lifecycle;

namespace Reprimand.Graphics;

/// <summary>
/// Specifies how attaching the backbuffer treats its existing contents.
/// </summary>
public enum BackbufferAttachBehavior {
	/// <summary>
	/// Discards and clears the existing color, depth, and stencil contents.
	/// </summary>
	Clear,

	/// <summary>
	/// Requests preservation of the existing color, depth, and stencil contents.
	/// </summary>
	Load,
}

/// <summary>
/// Controls how subsequent backbuffer attachments treat their existing contents.
/// </summary>
/// <remarks>
/// <para>
/// An override affects calls which attach the backbuffer while the corresponding scope is
/// active. Disposing the scope changes only the attachment policy; it does not change the
/// currently bound render target.
/// </para>
/// <para>
/// This class is not thread safe; all operations must occur on the thread that owns the
/// <see cref="GraphicsDevice"/>, i.e. the main game thread.
/// </para>
/// </remarks>
public static class BackbufferAttachment {
	/// <summary>
	/// A scoped backbuffer attachment behavior override.
	/// </summary>
	/// <remarks>
	/// Scopes must be disposed in reverse creation order. Copies of this value do not share
	/// disposed state; disposing a copy throws.
	/// </remarks>
	public readonly ref struct OverrideScope {
		private readonly ulong token;
		internal OverrideScope(ulong token, BackbufferAttachBehavior behavior) {
			this.token = token;
			Behavior = behavior;
		}

		/// <summary>
		/// The attachment behavior selected by this scope.
		/// </summary>
		public BackbufferAttachBehavior Behavior { get; }

		/// <summary>
		/// Restores the previous attachment behavior override.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the scope is not the innermost active scope or has already been disposed.
		/// </exception>
		public void Dispose() => disposeOverride(token);
	}

	private readonly struct OverrideFrame(ulong token, BackbufferAttachBehavior behavior) {
		public ulong Token { get; } = token;
		public BackbufferAttachBehavior Behavior { get; } = behavior;
	}

	private delegate void orig_GraphicsDevice_SetRenderTargets(GraphicsDevice self, RenderTargetBinding[]? renderTargets);

	private static readonly List<OverrideFrame> overrideStack = new();

	private static ulong nextToken;

	private static Hook? setRenderTargetsHook;

	/// <summary>
	/// The active attachment behavior override, or <see langword="null"/> if there is no
	/// active override.
	/// </summary>
	public static BackbufferAttachBehavior? CurrentOverride => overrideStack.Count != 0 ? overrideStack[^1].Behavior : null;

	/// <summary>
	/// The number of active attachment behavior overrides.
	/// </summary>
	internal static int OverrideDepth => overrideStack.Count;

	[OnLoad]
	internal static void RegisterHooks() {
		setRenderTargetsHook = new Hook(
			typeof(GraphicsDevice).GetMethod(
				nameof(GraphicsDevice.SetRenderTargets),
				BindingFlags.Public | BindingFlags.Instance,
				null,
				[
					typeof(RenderTargetBinding[]),
				],
				null
			) ?? throw new MissingMethodException(nameof(GraphicsDevice), nameof(GraphicsDevice.SetRenderTargets)),
			on_GraphicsDevice_SetRenderTargets
		);
	}

	[OnUnload]
	internal static void UnregisterHooks() {
		if (overrideStack.Count != 0)
			global::Celeste.Mod.Logger.Log(global::Celeste.Mod.LogLevel.Warn, "Reprimand/BackbufferAttachment", "unregistering hooks happened while backbuffer attachment overrides are active");
		try {
			setRenderTargetsHook?.Dispose();
			setRenderTargetsHook = null;
		} finally {
			nextToken = 0;
		}
	}

	/// <summary>
	/// Overrides the behavior of backbuffer attachments performed within the returned scope.
	/// </summary>
	/// <param name="behavior">
	/// The new attachment behavior.
	/// </param>
	/// <returns>
	/// A scope which restores the previous override when disposed.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown if <paramref name="behavior"/> is not a valid <see cref="BackbufferAttachBehavior"/>
	/// enum value.
	/// </exception>
	public static OverrideScope SetOverride(BackbufferAttachBehavior behavior) {
		if (!(behavior is BackbufferAttachBehavior.Clear or BackbufferAttachBehavior.Load))
			throw new ArgumentOutOfRangeException(nameof(behavior), behavior, "out of range BackbufferAttachBehavior enum value");
		ReprimandModule.ThrowIfInactive();
		ulong token = checked(++nextToken);
		overrideStack.Add(new OverrideFrame(token, behavior));
		return new OverrideScope(token, behavior);
	}

	/// <summary>
	/// Requests that backbuffer attachments preserve the existing contents.
	/// </summary>
	/// <returns>
	/// A scope which restores the previous override when disposed.
	/// </returns>
	public static OverrideScope SetLoad() => SetOverride(BackbufferAttachBehavior.Load);

	/// <summary>
	/// Requests that backbuffer attachments clear the existing contents.
	/// </summary>
	/// <returns>
	/// A scope which restores the previous override when disposed.
	/// </returns>
	public static OverrideScope SetClear() => SetOverride(BackbufferAttachBehavior.Clear);

	private static void on_GraphicsDevice_SetRenderTargets(orig_GraphicsDevice_SetRenderTargets orig, GraphicsDevice self, RenderTargetBinding[]? renderTargets) {
		GraphicsDevice? dev = Monocle.Engine.Graphics.GraphicsDevice;
		bool isTracked = dev is not null && ReferenceEquals(self, dev);
		bool bindsBackbuffer = renderTargets is null || renderTargets.Length == 0;
		if (!isTracked || !bindsBackbuffer || overrideStack.Count == 0) {
			orig(self, renderTargets);
			return;
		}

		BackbufferAttachBehavior behavior = overrideStack[^1].Behavior;
		PresentationParameters presentParams = self.PresentationParameters;
		RenderTargetUsage prevUsage = presentParams.RenderTargetUsage;
		presentParams.RenderTargetUsage = behavior == BackbufferAttachBehavior.Load ? RenderTargetUsage.PreserveContents : RenderTargetUsage.DiscardContents;
		try {
			orig(self, renderTargets);
		} finally {
			presentParams.RenderTargetUsage = prevUsage;
		}
	}

	private static void disposeOverride(ulong token) {
		if (token == 0)
			return;
		int idx = overrideStack.Count - 1;
		if (idx < 0)
			throw new InvalidOperationException("backbuffer attachment override has already been disposed");
		if (overrideStack[idx].Token != token)
			throw new InvalidOperationException("backbuffer attachment overrides must be disposed in LIFO order");
		overrideStack.RemoveAt(idx);
	}
}
