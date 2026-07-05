// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Language;

internal sealed class KnownTypes(Compilation comp) {
	public INamedTypeSymbol? Exception { get; } = comp.GetTypeByMetadataName(KnownTypeMetadataNames.Exception);
}
