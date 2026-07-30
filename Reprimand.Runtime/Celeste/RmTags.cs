// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

using Celeste;
using Monocle;
using MonoMod.Cil;
using Reprimand.Lifecycle;
using Reprimand.MonoMod;

namespace Reprimand.Runtime.Celeste;

/// <summary>
/// Provides additional entity tags or aliases for existing tags.
/// </summary>
public static class RmTags {
	private static BitTag? freezeframeUpdateBacking;

	/// <summary>
	/// Signifies that an entity is to update during freezeframes.
	/// </summary>
	/// <remarks>
	/// <para>
	/// No other update code runs; for instance, for an entity inside <see cref="Level"/>,
	/// <see cref="Level.Update()"/> does not run. Only the <see cref="Entity.Update()"/> methods
	/// of the entities marked with this tag run. This can lead to some unexpected behavior such as
	/// <see cref="Entity.Active"/> / <see cref="Entity.Visible"/> / <see cref="Entity.Collidable"/>
	/// changes or adding/removing entities not taking effect until the freezeframes end and a normal
	/// update runs.
	/// </para>
	/// <para>
	/// <see cref="Engine.DeltaTime"/> and <see cref="Engine.RawDeltaTime"/> do not receive special treatment and
	/// are set to their usual values by the game, as if no freezeframes were active. <see cref="Engine.FreezeTimer"/> is
	/// decremented <b>after</b> all entities with this tag update.
	/// </para>
	/// <para>
	/// Unlike most entity tags, this tag works in scenes other than <see cref="Level"/>.
	/// </para>
	/// </remarks>
	public static BitTag FreezeframeUpdate {
		get {
			ReprimandRuntimeModule.ThrowIfInactive();
			return freezeframeUpdateBacking ?? throw new InternalStateException("FreezeframeUpdate tag unexpectedly still uninitialized");
		}
	}

	/// <summary>
	/// Alias for <see cref="Tags.FrozenUpdate"/>, used to avoid confusion since <c>FrozenUpdate</c>
	/// sounds like "updates during freezeframes".
	/// </summary>
	/// <remarks>
	/// This does <b>not</b> make an entity update during freezeframes; instead, it makes
	/// said entity update while <see cref="Level.Frozen"/> is <see langword="true"/>.
	/// </remarks>
	public static BitTag LevelFrozenUpdate => Tags.FrozenUpdate;

	[OnLoad(UndoMethod = nameof(UnregisterHooks))]
	internal static void RegisterHooks() {
		IL.Monocle.Engine.Update += il_Engine_Update;
		On.Celeste.Tags.Initialize += on_Tags_Initialize;
	}

	internal static void UnregisterHooks() {
		IL.Monocle.Engine.Update -= il_Engine_Update;
		On.Celeste.Tags.Initialize -= on_Tags_Initialize;
	}

	private static void il_Engine_Update(ILContext il) {
		ILCursor c = new(il);
		c.RequireGotoNext(
			MoveType.After,
			static i => i.MatchLdsfld<Engine>(nameof(Engine.FreezeTimer)),
			static i => i.MatchLdcR4(0f),
			static i => i.MatchBleUn(out _)
		);
		c.EmitLdarg0();
		c.EmitDelegate(updateFreezeframeUpdateEntities);
	}

	private static void updateFreezeframeUpdateEntities(Engine self) {
		if (self.scene is null || freezeframeUpdateBacking is null)
			return;
		foreach (Entity e in self.scene[freezeframeUpdateBacking])
			e.Update();
			
	}

	private static void on_Tags_Initialize(On.Celeste.Tags.orig_Initialize orig) {
		// XXX: the ctor for BitTag neglects checking name collision and remaining available ID space;
		// maybe do RuntimeHelpers.GetUninitializedObject and construct it manually
		orig();
		freezeframeUpdateBacking = new BitTag("Reprimand/FreezeframeUpdate");
	}
}
