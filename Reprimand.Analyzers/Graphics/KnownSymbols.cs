// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Graphics;

internal sealed class KnownSymbols {
	public INamedTypeSymbol? SpriteBatch { get; }
	public INamedTypeSymbol? Draw { get; }
	public IPropertySymbol? DrawSpriteBatchProperty { get; }

	public KnownSymbols(Compilation comp) {
		SpriteBatch = comp.GetTypeByMetadataName(KnownMetadataNames.SpriteBatch);
		Draw = comp.GetTypeByMetadataName(KnownMetadataNames.Draw);
		DrawSpriteBatchProperty = Draw
			?.GetMembers()
			.OfType<IPropertySymbol>()
			.FirstOrDefault(static p => p.Name == KnownMetadataNames.DrawSpriteBatchProperty)
			?.OriginalDefinition;
	}
}
