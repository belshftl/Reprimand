// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using MonoMod.Cil;
using Reprimand.Lifecycle;
using Reprimand.MonoMod;

namespace Reprimand.Fixes;

internal static class FontLoadFixer {
	// loading custom fonts just crashes for some reason
	// i figured where the crash is coming from was something that could be harmlessly patched out,
	// so i did exactly that

	[OnLoad(UndoMethod = nameof(UnregisterHooks))]
	internal static void RegisterHooks() {
		IL.Monocle.PixelFont.AddFontSize_string_XmlElement_Atlas_bool += il_PixelFont_AddFontSize;
	}

	internal static void UnregisterHooks() {
		IL.Monocle.PixelFont.AddFontSize_string_XmlElement_Atlas_bool -= il_PixelFont_AddFontSize;
	}

	private static void il_PixelFont_AddFontSize(ILContext il) {
		ILCursor c = new(il);
		c.RequireGotoNext(MoveType.After, static i => i.MatchLdfld<Monocle.PixelFontCharacter>(nameof(Monocle.PixelFontCharacter.Kerning)));
		c.RequireGotoNext(MoveType.Before, static i => i.MatchCallvirt<Dictionary<int, int>>(nameof(Dictionary<,>.Add)));
		if (c.Next is null)
			throw new InternalStateException("expected ILCursor.Next to be nonnull after a successful MoveType.Before match");
		c.Next.Operand = il.Module.ImportReference(
			typeof(Dictionary<int, int>).GetProperty("Item")?.GetSetMethod() ??
			throw new MissingMethodException(nameof(Dictionary<,>), "set_Item")
		);
	}
}
