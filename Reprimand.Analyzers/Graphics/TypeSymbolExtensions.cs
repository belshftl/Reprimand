// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Graphics;

internal static class TypeSymbolExtensions {
	extension([NotNullWhen(true)] ITypeSymbol? type) {
		public bool IsOrDerivesFrom([NotNullWhen(true)] INamedTypeSymbol? candidateBase) {
			if (type is null || candidateBase is null)
				return false;
			for (ITypeSymbol? sym = type; sym is not null; sym = sym.BaseType)
				if (SymbolEqualityComparer.Default.Equals(sym.OriginalDefinition, candidateBase.OriginalDefinition))
					return true;
			return false;
		}
	}
}
