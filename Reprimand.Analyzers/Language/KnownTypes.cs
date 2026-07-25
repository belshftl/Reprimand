// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Language;

internal sealed class KnownTypes(Compilation comp) {
	public INamedTypeSymbol? Exception { get; } = comp.GetTypeByMetadataName(KnownTypeMetadataNames.Exception);
}
