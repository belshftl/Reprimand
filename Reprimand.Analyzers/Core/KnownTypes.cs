// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Core;

internal sealed class KnownTypes(Compilation comp) {
	public INamedTypeSymbol? OnLoadAttribute { get; } = comp.GetTypeByMetadataName(KnownTypeMetadataNames.OnLoadAttribute);
	public INamedTypeSymbol? OnLoadWithOptionalDepAttribute { get; } = comp.GetTypeByMetadataName(KnownTypeMetadataNames.OnLoadWithOptionalDepAttribute);
	public INamedTypeSymbol? OnUnloadAttribute { get; } = comp.GetTypeByMetadataName(KnownTypeMetadataNames.OnUnloadAttribute);
	public INamedTypeSymbol? OnUnloadWithOptionalDepAttribute { get; } = comp.GetTypeByMetadataName(KnownTypeMetadataNames.OnUnloadWithOptionalDepAttribute);
	public INamedTypeSymbol? EverestModule { get; } = comp.GetTypeByMetadataName(KnownTypeMetadataNames.EverestModule);
}
