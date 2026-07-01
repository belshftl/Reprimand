// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

namespace Reprimand.Analyzers.Discouraged;

internal static class KnownTypeMetadataNames {
	public const string Hook = "MonoMod.RuntimeDetour.Hook";
	public const string ILHook = "MonoMod.RuntimeDetour.ILHook";
	public const string NativeHook = "MonoMod.RuntimeDetour.NativeHook";
	public const string DetourConfig = "MonoMod.RuntimeDetour.DetourConfig";
	public const string ILCursor = "MonoMod.Cil.ILCursor";
	public const string ILContext = "MonoMod.Cil.ILContext";
	public const string Instruction = "Mono.Cecil.Cil.Instruction";
}
