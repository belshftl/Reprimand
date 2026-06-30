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
		if (SpriteBatch is not null && GlobalSpriteBatch is not null) {
			IPropertySymbol? result = null;
			foreach (ISymbol member in GlobalSpriteBatch.GetMembers(KnownMetadataNames.GlobalSpriteBatchProperty)) {
				if (member is not IPropertySymbol p || !p.IsStatic || !p.Type.IsOrDerivesFrom(SpriteBatch))
					continue;
				if (result is not null)
					return;
				result = p;
			}
			GlobalSpriteBatchProperty = result;
		}

		Draw = comp.GetTypeByMetadataName(KnownMetadataNames.Draw);
		if (Draw is not null) {
			IPropertySymbol? result = null;
			foreach (ISymbol member in Draw.GetMembers(KnownMetadataNames.DrawSpriteBatchProperty)) {
				if (member is not IPropertySymbol p || !p.IsStatic || !p.Type.IsOrDerivesFrom(SpriteBatch))
					continue;
				if (result is not null)
					return;
				result = p;
			}
			DrawSpriteBatchProperty = result;
		}
	}
}
