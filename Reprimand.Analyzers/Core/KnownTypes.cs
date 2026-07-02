// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Core;

internal sealed class KnownTypes(Compilation comp) {
	public INamedTypeSymbol? OnLoadAttribute { get; } = comp.GetTypeByMetadataName(KnownTypeMetadataNames.OnLoadAttribute);
	public INamedTypeSymbol? OnLoadOneshotAttribute { get; } = comp.GetTypeByMetadataName(KnownTypeMetadataNames.OnLoadOneshotAttribute);
	public INamedTypeSymbol? OnLoadIfOptionalDepAttribute { get; } = comp.GetTypeByMetadataName(KnownTypeMetadataNames.OnLoadIfOptionalDepAttribute);
	public INamedTypeSymbol? OnLoadIfOptionalDepOneshotAttribute { get; } = comp.GetTypeByMetadataName(KnownTypeMetadataNames.OnLoadIfOptionalDepOneshotAttribute);
	public INamedTypeSymbol? EverestModule { get; } = comp.GetTypeByMetadataName(KnownTypeMetadataNames.EverestModule);
}
