// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Graphics;

internal sealed class KnownSymbols {
	public INamedTypeSymbol? SpriteBatch { get; }
	public INamedTypeSymbol? GlobalSpriteBatch { get; }
	public IPropertySymbol? GlobalSpriteBatchProperty { get; }

	public INamedTypeSymbol? Draw { get; }
	public IPropertySymbol? DrawSpriteBatchProperty { get; }

	public KnownSymbols(Compilation comp) {
		SpriteBatch = comp.GetTypeByMetadataName(KnownMetadataNames.SpriteBatch);
		GlobalSpriteBatch = comp.GetTypeByMetadataName(KnownMetadataNames.GlobalSpriteBatch);
		GlobalSpriteBatchProperty = GlobalSpriteBatch
			?.GetMembers()
			.OfType<IPropertySymbol>()
			.FirstOrDefault(static p => p.Name == KnownMetadataNames.GlobalSpriteBatchBatchProperty)
			?.OriginalDefinition;

		Draw = comp.GetTypeByMetadataName(KnownMetadataNames.Draw);
		DrawSpriteBatchProperty = Draw
			?.GetMembers()
			.OfType<IPropertySymbol>()
			.FirstOrDefault(static p => p.Name == KnownMetadataNames.DrawSpriteBatchProperty)
			?.OriginalDefinition;
	}
}
