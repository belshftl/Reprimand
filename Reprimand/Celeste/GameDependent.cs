// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Runtime.CompilerServices;
using Monocle;
using Reprimand.Lifecycle;

namespace Reprimand.Celeste;

internal interface IUntypedGameDependentT {
	void OnLoadContent();
	void OnUnloadContent();
}

internal static class GameDependentTHooks {
	private static readonly ConditionalWeakTable<IUntypedGameDependentT, object> live = new();
	private static readonly object dummy = new();

	private static readonly object lifecycleLock = new(); // this is the cuck .net version without System.Threading.Lock

	private static int everLoaded = 0;
	private static bool loaded = false;

	/// <returns>
	/// Whether the instance should create immediately.
	/// </returns>
	public static bool Add(IUntypedGameDependentT v) {
		lock (lifecycleLock) {
			live.Add(v, dummy);
			return loaded;
		}
	}

	private static List<IUntypedGameDependentT> snapshotLiveLocked() {
		// there's an inherent GC race window with this approach, but it's acceptable here and
		// we can minimize it by materializing to strong references ahead of time like this
		List<IUntypedGameDependentT> strong = new();
		foreach (KeyValuePair<IUntypedGameDependentT, object> kvp in live)
			strong.Add(kvp.Key);
		return strong;
	}

	[OnLoad(UndoMethod = nameof(UnregisterHooks))]
	public static void RegisterHooks() {
		On.Monocle.Engine.LoadContent += on_Engine_LoadContent;
		On.Monocle.Engine.UnloadContent += on_Engine_UnloadContent;
	}

	public static void UnregisterHooks() {
		On.Monocle.Engine.LoadContent -= on_Engine_LoadContent;
		On.Monocle.Engine.UnloadContent -= on_Engine_UnloadContent;
	}

	private static void on_Engine_LoadContent(On.Monocle.Engine.orig_LoadContent orig, Engine self) {
		if (Interlocked.Exchange(ref everLoaded, 1) != 0)
			throw new InvalidOperationException("Engine.LoadContent got invoked more than once; was assuming reload cannot happen");

		orig(self);

		List<IUntypedGameDependentT> strong;
		lock (lifecycleLock) {
			loaded = true;
			strong = snapshotLiveLocked();
		}

		List<Exception>? failures = null;
		foreach (IUntypedGameDependentT v in strong) {
			try {
				v.OnLoadContent();
			} catch (Exception ex) {
				(failures ??= new List<Exception>()).Add(ex);
			}
		}

		if (failures is not null)
			throw new AggregateException("one or more GameDependent<T> factories failed", failures);
	}

	private static void on_Engine_UnloadContent(On.Monocle.Engine.orig_UnloadContent orig, Engine self) {
		List<IUntypedGameDependentT> strong;
		lock (lifecycleLock) {
			loaded = false;
			strong = snapshotLiveLocked();
		}

		List<Exception>? failures = null;
		foreach (IUntypedGameDependentT v in strong) {
			try {
				v.OnUnloadContent();
			} catch (Exception ex) {
				(failures ??= new List<Exception>()).Add(ex);
			}
		}

		// probably unnecessary, but do anyways defensively before orig(...) call
		strong.Clear();

		orig(self);

		if (failures is not null)
			throw new AggregateException("one or more GameDependent<T> teardown callbacks failed", failures);
	}
}

/// <summary>
/// Allows for safely declaring ahead of time a value that depends on the game being initialized to be created.
/// Intended for use in static constructors or static field/property initializers. 
/// </summary>
/// <remarks>
/// <para>
/// <see langword="null"/> is not internally distinguished from "no value currently held"; the factory
/// must return a non-null value.
/// </para>
/// <para>
/// The factory is invoked, as such creating the value, either:
/// <list type="bullet">
/// <item><description>
/// During <see cref="Engine.LoadContent()"/>, after the base game's initialization has completed,
/// </description></item>
/// <item><description>
/// or immediately in the constructor, if the base game's portion of <see cref="Engine.LoadContent()"/>
/// has already completed and <see cref="Engine.UnloadContent()"/> hasn't been invoked yet.
/// </description></item>
/// </list>
/// The ordering between the factory invocations of two or more <see cref="GameDependent{T}"/> instances
/// is undefined and should not be relied on.
/// </para>
/// <para>
/// Similarly, at the beginning of <see cref="Engine.UnloadContent()"/>, before the base game's teardown,
/// the value is taken out of its storing field and optionally has the teardown callback invoked with it
/// before being dropped.
/// </para>
/// </remarks>
public sealed class GameDependent<T> : IUntypedGameDependentT where T : class {
	private readonly Func<T> factory;
	private readonly Action<T>? teardown;
	private readonly object valueLock = new(); // same thing as above about System.Threading.Lock
	private bool creationFiredOrSuppressed;
	private T? val = null;

	/// <summary>
	/// Whether this <see cref="GameDependent{T}"/> instance currently holds a value.
	/// </summary>
	public bool HoldsValue => Volatile.Read(ref val) is not null;

	/// <summary>
	/// The currently held value.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// Thrown if no value is currently held.
	/// </exception>
	public T Value => Volatile.Read(ref val) ?? throw new InvalidOperationException("no value is currently held; most likely, the game is not initialized yet");

	/// <summary>
	/// The currently held value, or <see langword="null"/> if no value is currently held.
	/// </summary>
	public T? ValueOrNull => Volatile.Read(ref val);

	/// <summary>
	/// Creates a new <see cref="GameDependent{T}"/> instance with the given callback(s).
	/// </summary>
	/// <param name="factory">
	/// Factory callback. Must return a non-null value. This delegate instance is retained for the
	/// lifetime of the <see cref="GameDependent{T}"/> object.
	/// </param>
	/// <param name="teardown">
	/// Optional teardown callback. This delegate instance is retained for the lifetime of the
	/// <see cref="GameDependent{T}"/> object.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="factory"/> is <see langword="null"/>.
	/// </exception>
	public GameDependent(Func<T> factory, Action<T>? teardown = null) {
		ArgumentNullException.ThrowIfNull(factory);
		ReprimandModule.ThrowIfInactive();
		this.factory = factory;
		this.teardown = teardown;
		if (GameDependentTHooks.Add(this))
			create();
	}

	private void create() {
		lock (valueLock) {
			if (creationFiredOrSuppressed)
				return;
			creationFiredOrSuppressed = true;
			T v = factory() ?? throw new InvalidOperationException("GameDependent<T> factory method returned null");
			Volatile.Write(ref val, v);
		}
	}
	void IUntypedGameDependentT.OnLoadContent() => create();

	private void destroy() {
		T? v;
		Action<T>? cb;

		lock (valueLock) {
			// if unload sees this object before the create() in the constructor
			// wins the race, permamently suppress it
			creationFiredOrSuppressed = true;

			v = val;
			cb = teardown;
			Volatile.Write(ref val, null);
		}

		if (v is not null && cb is not null)
			cb(v);
	}
	void IUntypedGameDependentT.OnUnloadContent() => destroy();
}
