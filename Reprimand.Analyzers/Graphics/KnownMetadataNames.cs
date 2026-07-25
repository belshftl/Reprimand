// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

namespace Reprimand.Analyzers.Graphics;

internal static class KnownMetadataNames {
	public const string SpriteBatch = "Microsoft.Xna.Framework.Graphics.SpriteBatch";
	public const string GlobalSpriteBatch = "Reprimand.Graphics.GlobalSpriteBatch";
	public const string GlobalSpriteBatchBatchProperty = "Batch";

	public const string Draw = "Monocle.Draw";
	public const string DrawSpriteBatchProperty = "SpriteBatch";
}
