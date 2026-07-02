// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Discouraged;

internal sealed class KnownSymbols {
	public INamedTypeSymbol? Hook { get; }
	public INamedTypeSymbol? ILHook { get; }
	public INamedTypeSymbol? NativeHook { get; }
	public INamedTypeSymbol? DetourConfig { get; }
	public INamedTypeSymbol? ILCursor { get; }
	public INamedTypeSymbol? ILContext { get; }
	public INamedTypeSymbol? Instruction { get; }
	public ImmutableHashSet<IMethodSymbol> EmitDelegateMethods { get; }
	public ImmutableHashSet<IMethodSymbol> RemoveInstructionMethods { get; }
	public ImmutableHashSet<IMethodSymbol> GotoMethods { get; }
	public ImmutableHashSet<ISymbol> InstructionMembers { get; }

	public INamedTypeSymbol? Entity { get; }
	public INamedTypeSymbol? Component { get; }
	public ImmutableHashSet<IMethodSymbol> SceneAsMethods { get; }

	public KnownSymbols(Compilation comp) {
		Hook = comp.GetTypeByMetadataName(KnownMetadataNames.Hook);
		ILHook = comp.GetTypeByMetadataName(KnownMetadataNames.ILHook);
		NativeHook = comp.GetTypeByMetadataName(KnownMetadataNames.NativeHook);
		DetourConfig = comp.GetTypeByMetadataName(KnownMetadataNames.DetourConfig);
		ILCursor = comp.GetTypeByMetadataName(KnownMetadataNames.ILCursor);
		ILContext = comp.GetTypeByMetadataName(KnownMetadataNames.ILContext);
		Instruction = comp.GetTypeByMetadataName(KnownMetadataNames.Instruction);

		EmitDelegateMethods = ILCursor
			?.GetMembers()
			.OfType<IMethodSymbol>()
			.Where(static m => m.Name == "EmitDelegate")
			.Select(static m => m.OriginalDefinition)
			.ToImmutableHashSet<IMethodSymbol>(SymbolEqualityComparer.Default) ?? ImmutableHashSet<IMethodSymbol>.Empty;
		RemoveInstructionMethods = ILCursor
			?.GetMembers()
			.OfType<IMethodSymbol>()
			.Where(static m => m.Name is "Remove" or "RemoveRange")
			.Select(static m => m.OriginalDefinition)
			.ToImmutableHashSet<IMethodSymbol>(SymbolEqualityComparer.Default) ?? ImmutableHashSet<IMethodSymbol>.Empty;
		GotoMethods = ILCursor
			?.GetMembers()
			.OfType<IMethodSymbol>()
			.Where(static m => m.Name is "GotoNext" or "GotoPrev")
			.Select(static m => m.OriginalDefinition)
			.ToImmutableHashSet<IMethodSymbol>(SymbolEqualityComparer.Default) ?? ImmutableHashSet<IMethodSymbol>.Empty;
		InstructionMembers = Instruction
			?.GetMembers()
			.Where(static m => m.Name is "OpCode" or "Operand")
			.Select(static m => m.OriginalDefinition)
			.ToImmutableHashSet(SymbolEqualityComparer.Default) ?? ImmutableHashSet<ISymbol>.Empty;

		Entity = comp.GetTypeByMetadataName(KnownMetadataNames.Entity);
		Component = comp.GetTypeByMetadataName(KnownMetadataNames.Component);
		SceneAsMethods = Entity
			?.GetMembers()
			.OfType<IMethodSymbol>()
			.Where(static m => m.Name == KnownMetadataNames.SceneAsMethod)
			.Select(static m => m.OriginalDefinition)
			.Concat(
				Component
				?.GetMembers()
				.OfType<IMethodSymbol>()
				.Where(static m => m.Name == KnownMetadataNames.SceneAsMethod)
				.Select(static m => m.OriginalDefinition) ?? ImmutableArray<IMethodSymbol>.Empty
			)
			.ToImmutableHashSet<IMethodSymbol>(SymbolEqualityComparer.Default) ?? ImmutableHashSet<IMethodSymbol>.Empty;
	}
}
