// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Usage;

internal sealed class KnownSymbols {
	public INamedTypeSymbol? DontUseInStaticCtorAttribute { get; }
	public INamedTypeSymbol? IOnLoadLifecycleAttribute { get; }

	public INamedTypeSymbol? Hook { get; }
	public INamedTypeSymbol? ILHook { get; }
	public INamedTypeSymbol? NativeHook { get; }
	public INamedTypeSymbol? DetourConfig { get; }
	public INamedTypeSymbol? DetourContext { get; }
	public INamedTypeSymbol? DetourConfigContext { get; }
	public INamedTypeSymbol? ILCursor { get; }
	public INamedTypeSymbol? ILContext { get; }
	public INamedTypeSymbol? Instruction { get; }
	public ImmutableHashSet<IMethodSymbol> EmitDelegateMethods { get; }
	public ImmutableHashSet<IMethodSymbol> RemoveInstructionMethods { get; }
	public ImmutableHashSet<IMethodSymbol> GotoMethods { get; }
	public ImmutableHashSet<ISymbol> InstructionMembers { get; }

	public IMethodSymbol? DetourContextParamlessUseMethod { get; }

	public INamedTypeSymbol? Entity { get; }
	public INamedTypeSymbol? Component { get; }
	public ImmutableHashSet<IMethodSymbol> SceneAsMethods { get; }

	public INamedTypeSymbol? TrackedAttribute { get; }
	public INamedTypeSymbol? TrackedAsAttribute { get; }

	public INamedTypeSymbol? Tracker { get; }
	public ImmutableHashSet<IMethodSymbol> TrackerExtReplacedMethods { get; }
	public ImmutableHashSet<IMethodSymbol> TrackerEnumerateMethods { get; }
	public ImmutableHashSet<IMethodSymbol> TrackerCountMethods { get; }

	public INamedTypeSymbol? EntityList { get; }
	public ImmutableHashSet<IMethodSymbol> EntityListFindMethods { get; }

	public INamedTypeSymbol? Engine { get; }
	public ImmutableHashSet<IPropertySymbol> NonStaticInitedEngineProperties { get; }

	public INamedTypeSymbol? Draw { get; }
	public ImmutableHashSet<IFieldSymbol> NonStaticInitedDrawFields { get; }
	public ImmutableHashSet<IPropertySymbol> NonStaticInitedDrawProperties { get; }

	public INamedTypeSymbol? Gfx { get; }
	public ImmutableHashSet<IFieldSymbol> NonStaticInitedGfxFields { get; }

	public INamedTypeSymbol? VirtualContent { get; }
	public ImmutableHashSet<IMethodSymbol> NonStaticInitedVirtualContentMethods { get; }

	public KnownSymbols(Compilation comp) {
		DontUseInStaticCtorAttribute = comp.GetTypeByMetadataName(KnownMetadataNames.DontUseInStaticCtorAttribute);
		IOnLoadLifecycleAttribute = comp.GetTypeByMetadataName(KnownMetadataNames.IOnLoadLifecycleAttribute);

		Hook = comp.GetTypeByMetadataName(KnownMetadataNames.Hook);
		ILHook = comp.GetTypeByMetadataName(KnownMetadataNames.ILHook);
		NativeHook = comp.GetTypeByMetadataName(KnownMetadataNames.NativeHook);
		DetourConfig = comp.GetTypeByMetadataName(KnownMetadataNames.DetourConfig);
		DetourContext = comp.GetTypeByMetadataName(KnownMetadataNames.DetourContext);
		DetourConfigContext = comp.GetTypeByMetadataName(KnownMetadataNames.DetourConfigContext);
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

		DetourContextParamlessUseMethod = DetourContext
			?.GetMembers()
			.OfType<IMethodSymbol>()
			.FirstOrDefault(static m => m.Name == KnownMetadataNames.DetourConfigContextUseMethod && m.Parameters.Length == 0)
			?.OriginalDefinition;

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

		TrackedAttribute = comp.GetTypeByMetadataName(KnownMetadataNames.TrackedAttribute);
		TrackedAsAttribute = comp.GetTypeByMetadataName(KnownMetadataNames.TrackedAsAttribute);

		Tracker = comp.GetTypeByMetadataName(KnownMetadataNames.Tracker);
		TrackerExtReplacedMethods = Tracker
			?.GetMembers()
			.OfType<IMethodSymbol>()
			.Where(
				static m =>
					m.Name == KnownMetadataNames.TrackerGetEntityMethod ||
					m.Name == KnownMetadataNames.TrackerGetNearestEntityMethod ||
					m.Name == KnownMetadataNames.TrackerGetEntitiesMethod ||
					m.Name == KnownMetadataNames.TrackerGetEntitiesCopyMethod ||
					m.Name == KnownMetadataNames.TrackerGetComponentMethod ||
					m.Name == KnownMetadataNames.TrackerGetNearestComponentMethod ||
					m.Name == KnownMetadataNames.TrackerGetComponentsMethod ||
					m.Name == KnownMetadataNames.TrackerGetComponentsCopyMethod
			)
			.Select(static m => m.OriginalDefinition)
			.ToImmutableHashSet<IMethodSymbol>(SymbolEqualityComparer.Default) ?? ImmutableHashSet<IMethodSymbol>.Empty;
		TrackerEnumerateMethods = Tracker
			?.GetMembers()
			.OfType<IMethodSymbol>()
			.Where(
				static m =>
					m.Name == KnownMetadataNames.TrackerEnumerateEntitiesMethod ||
					m.Name == KnownMetadataNames.TrackerEnumerateComponentsMethod
			)
			.Select(static m => m.OriginalDefinition)
			.ToImmutableHashSet<IMethodSymbol>(SymbolEqualityComparer.Default) ?? ImmutableHashSet<IMethodSymbol>.Empty;
		TrackerCountMethods = Tracker
			?.GetMembers()
			.OfType<IMethodSymbol>()
			.Where(
				static m =>
					m.Name == KnownMetadataNames.TrackerCountEntitiesMethod ||
					m.Name == KnownMetadataNames.TrackerCountComponentsMethod
			)
			.Select(static m => m.OriginalDefinition)
			.ToImmutableHashSet<IMethodSymbol>(SymbolEqualityComparer.Default) ?? ImmutableHashSet<IMethodSymbol>.Empty;

		EntityList = comp.GetTypeByMetadataName(KnownMetadataNames.EntityList);
		EntityListFindMethods = EntityList
			?.GetMembers()
			.OfType<IMethodSymbol>()
			.Where(
				static m =>
					m.Name == KnownMetadataNames.EntityListFindFirstMethod ||
					m.Name == KnownMetadataNames.EntityListFindAllMethod
			)
			.Select(static m => m.OriginalDefinition)
			.ToImmutableHashSet<IMethodSymbol>(SymbolEqualityComparer.Default) ?? ImmutableHashSet<IMethodSymbol>.Empty;

		Engine = comp.GetTypeByMetadataName(KnownMetadataNames.Engine);
		NonStaticInitedEngineProperties = Engine
			?.GetMembers()
			.OfType<IPropertySymbol>()
			.Where(
				static p =>
					p.Name == KnownMetadataNames.EngineInstanceProperty ||
					p.Name == KnownMetadataNames.EngineGraphicsProperty ||
					p.Name == KnownMetadataNames.EngineCommandsProperty ||
					p.Name == KnownMetadataNames.EnginePoolerProperty
			)
			.Select(static p => p.OriginalDefinition)
			.ToImmutableHashSet<IPropertySymbol>(SymbolEqualityComparer.Default) ?? ImmutableHashSet<IPropertySymbol>.Empty;

		Draw = comp.GetTypeByMetadataName(KnownMetadataNames.Draw);
		NonStaticInitedDrawFields = Draw
			?.GetMembers()
			.OfType<IFieldSymbol>()
			.Where(static f => f.Name == KnownMetadataNames.DrawParticleField || f.Name == KnownMetadataNames.DrawPixelField)
			.Select(static f => f.OriginalDefinition)
			.ToImmutableHashSet<IFieldSymbol>(SymbolEqualityComparer.Default) ?? ImmutableHashSet<IFieldSymbol>.Empty;
		NonStaticInitedDrawProperties = Draw
			?.GetMembers()
			.OfType<IPropertySymbol>()
			.Where(
				static p =>
					p.Name == KnownMetadataNames.DrawRendererProperty ||
					p.Name == KnownMetadataNames.DrawSpriteBatchProperty ||
					p.Name == KnownMetadataNames.DrawDefaultFontProperty
			)
			.Select(static p => p.OriginalDefinition)
			.ToImmutableHashSet<IPropertySymbol>(SymbolEqualityComparer.Default) ?? ImmutableHashSet<IPropertySymbol>.Empty;

		Gfx = comp.GetTypeByMetadataName(KnownMetadataNames.Gfx);
		NonStaticInitedGfxFields = Gfx
			?.GetMembers()
			.OfType<IFieldSymbol>()
			.Where(
				static f =>
					f.Name != KnownMetadataNames.GfxSubtractField &&
					f.Name != KnownMetadataNames.GfxDestinationTransparencySubtractField
			)
			.Select(static f => f.OriginalDefinition)
			.ToImmutableHashSet<IFieldSymbol>(SymbolEqualityComparer.Default) ?? ImmutableHashSet<IFieldSymbol>.Empty;

		VirtualContent = comp.GetTypeByMetadataName(KnownMetadataNames.VirtualContent);
		NonStaticInitedVirtualContentMethods = VirtualContent
			?.GetMembers()
			.OfType<IMethodSymbol>()
			.Where(
				static m =>
					m.Name == KnownMetadataNames.VirtualContentCreateTextureMethod ||
					m.Name == KnownMetadataNames.VirtualContentCreateRenderTargetMethod
			)
			.Select(static m => m.OriginalDefinition)
			.ToImmutableHashSet<IMethodSymbol>(SymbolEqualityComparer.Default) ?? ImmutableHashSet<IMethodSymbol>.Empty;
	}
}
