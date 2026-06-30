// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

namespace Reprimand.Analyzers.Graphics;

internal static class KnownMetadataNames {
	public const string SpriteBatch = "Microsoft.Xna.Framework.Graphics.SpriteBatch";
	public const string GlobalSpriteBatch = "Reprimand.Graphics.GlobalSpriteBatch";
	public const string GlobalSpriteBatchProperty = "SpriteBatch";

	public const string Draw = "Monocle.Draw";
	public const string DrawSpriteBatchProperty = "SpriteBatch";
}
