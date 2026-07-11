// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Core;

internal sealed class KnownSymbols(Compilation comp) {
	public INamespaceSymbol? ReprimandExtensionsNs { get; } = getNamespace(comp, KnownMetadataNames.ReprimandExtensionsNs);
	public INamespaceSymbol? ReprimandLifecycleNs { get; } = getNamespace(comp, KnownMetadataNames.ReprimandLifecycleNs);
	public INamespaceSymbol? ReprimandMonoModNs { get; } = getNamespace(comp, KnownMetadataNames.ReprimandMonoModNs);

	public INamedTypeSymbol? OnLoadAttribute { get; } = comp.GetTypeByMetadataName(KnownMetadataNames.OnLoadAttribute);
	public INamedTypeSymbol? OnLoadOneshotAttribute { get; } = comp.GetTypeByMetadataName(KnownMetadataNames.OnLoadOneshotAttribute);
	public INamedTypeSymbol? OnLoadIfOptionalDepAttribute { get; } = comp.GetTypeByMetadataName(KnownMetadataNames.OnLoadIfOptionalDepAttribute);
	public INamedTypeSymbol? OnLoadIfOptionalDepOneshotAttribute { get; } = comp.GetTypeByMetadataName(KnownMetadataNames.OnLoadIfOptionalDepOneshotAttribute);
	public INamedTypeSymbol? EverestModule { get; } = comp.GetTypeByMetadataName(KnownMetadataNames.EverestModule);

	private static INamespaceSymbol? getNamespace(Compilation comp, string qualifiedName) {
		INamespaceSymbol curr = comp.GlobalNamespace;
		foreach (string component in qualifiedName.Split('.')) {
			curr = curr.GetMembers(component).OfType<INamespaceSymbol>().SingleOrDefault();
			if (curr is null)
				return null;
		}
		return curr;
	}
}
