// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers;

internal sealed class MethodDefinitionComparer : IEqualityComparer<IMethodSymbol> {
	public static MethodDefinitionComparer Instance { get; } = new();

	private MethodDefinitionComparer() {
	}

	public bool Equals(IMethodSymbol? x, IMethodSymbol? y) => SymbolEqualityComparer.Default.Equals(Canonicalize(x), Canonicalize(y));
	public int GetHashCode(IMethodSymbol? obj) => Canonicalize(obj) is {} m ? SymbolEqualityComparer.Default.GetHashCode(m) : 0;

	[return: NotNullIfNotNull(nameof(method))]
	public static IMethodSymbol? Canonicalize(IMethodSymbol? method) {
		if (method is null)
			return null;
		// bounded so that a malformed symbol can't loop forever
		for (int i = 0; i < 8; i++) {
			IMethodSymbol previous = method;
			method = method.OriginalDefinition;
			method = method.PartialDefinitionPart ?? method;
			method = method.ReducedFrom ?? method;
			method = method.AssociatedExtensionImplementation ?? method;
			if (SymbolEqualityComparer.Default.Equals(method, previous))
				break;
		}
		return method;
	}
}
