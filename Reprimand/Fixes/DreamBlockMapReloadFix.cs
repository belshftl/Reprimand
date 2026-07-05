// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using MonoMod.Cil;
using Reprimand.Lifecycle;
using Reprimand.MonoMod;

namespace Reprimand.Fixes;

internal static class DreamBlockMapReloadFix {
	[OnLoad(UndoMethod = nameof(UnregisterHooks))]
	internal static void RegisterHooks() {
		IL.Celeste.Dust.Burst_Vector2_float_int_ParticleType += il_Dust_Burst;
		IL.Celeste.Dust.BurstFG += il_Dust_Burst;
	}

	internal static void UnregisterHooks() {
		IL.Celeste.Dust.Burst_Vector2_float_int_ParticleType -= il_Dust_Burst;
		IL.Celeste.Dust.BurstFG -= il_Dust_Burst;
	}

	private static void il_Dust_Burst(ILContext il) {
		ILCursor c = new(il);
		ILLabel skip = c.DefineLabel();
		c.RequireGotoNext(MoveType.After, static i => i.MatchCall<Monocle.Engine>("get_Scene"), static i => i.MatchIsinst<global::Celeste.Level>());
		c.EmitDup();
		c.EmitBrtrue(skip);
		c.EmitPop();
		c.EmitRet();
		c.MarkLabel(skip);
	}
}
