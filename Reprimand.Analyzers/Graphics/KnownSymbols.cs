// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

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
