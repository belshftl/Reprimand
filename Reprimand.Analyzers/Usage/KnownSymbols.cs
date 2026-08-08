// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Usage;

internal sealed class KnownSymbols {
	public INamedTypeSymbol? SystemDelegate { get; }
	public INamedTypeSymbol? LinqExpression { get; }
	public INamedTypeSymbol? IEnumerator { get; }

	public INamedTypeSymbol? DontUseInStaticCtorAttribute { get; }
	public INamedTypeSymbol? RunsUnderDetourIdAttribute { get; }
	public INamedTypeSymbol? IOnLoadLifecycleAttribute { get; }

	public INamedTypeSymbol? Tags { get; }
	public IFieldSymbol? TagsFrozenUpdateField { get; }
	public INamedTypeSymbol? RmTags { get; }

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
	public INamedTypeSymbol? TrackerExtensions { get; }
	public ImmutableHashSet<IMethodSymbol> TrackerOnlyLookupMethods { get; }
	public ImmutableHashSet<IMethodSymbol> TrackerExtReplacedMethods { get; }
	public ImmutableHashSet<IMethodSymbol> TrackerEnumerateMethods { get; }
	public ImmutableHashSet<IMethodSymbol> TrackerCountMethods { get; }

	public INamedTypeSymbol? EntityList { get; }
	public ImmutableHashSet<IMethodSymbol> EntityListFindMethods { get; }

	public INamedTypeSymbol? Engine { get; }
	public IFieldSymbol? EngineEffectiveTimeRateField { get; }
	public ImmutableHashSet<IPropertySymbol> NonStaticInitedEngineProperties { get; }

	public INamedTypeSymbol? Draw { get; }
	public ImmutableHashSet<IFieldSymbol> NonStaticInitedDrawFields { get; }
	public ImmutableHashSet<IPropertySymbol> NonStaticInitedDrawProperties { get; }

	public INamedTypeSymbol? Gfx { get; }
	public ImmutableHashSet<IFieldSymbol> NonStaticInitedGfxFields { get; }

	public INamedTypeSymbol? VirtualContent { get; }
	public ImmutableHashSet<IMethodSymbol> NonStaticInitedVirtualContentMethods { get; }

	public INamedTypeSymbol? VirtualRenderTarget { get; }
	public INamedTypeSymbol? VirtualTexture { get; }

	public KnownSymbols(Compilation comp) {
		SystemDelegate = comp.GetTypeByMetadataName(KnownMetadataNames.SystemDelegate);
		LinqExpression = comp.GetTypeByMetadataName(KnownMetadataNames.LinqExpression);
		IEnumerator = comp.GetTypeByMetadataName(KnownMetadataNames.IEnumerator);

		DontUseInStaticCtorAttribute = comp.GetTypeByMetadataName(KnownMetadataNames.DontUseInStaticCtorAttribute);
		RunsUnderDetourIdAttribute = comp.GetTypeByMetadataName(KnownMetadataNames.RunsUnderDetourIdAttribute);
		IOnLoadLifecycleAttribute = comp.GetTypeByMetadataName(KnownMetadataNames.IOnLoadLifecycleAttribute);

		Tags = comp.GetTypeByMetadataName(KnownMetadataNames.Tags);
		TagsFrozenUpdateField = Tags
			?.GetMembers()
			.OfType<IFieldSymbol>()
			.FirstOrDefault(static f => f.Name == KnownMetadataNames.TagsFrozenUpdateField)
			?.OriginalDefinition;
		RmTags = comp.GetTypeByMetadataName(KnownMetadataNames.RmTags);

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
			?.GetAllMethods()
			.Where(static m => m.Name == "EmitDelegate")
			.ToImmutableHashSet(MethodDefinitionComparer.Instance) ?? ImmutableHashSet<IMethodSymbol>.Empty;
		RemoveInstructionMethods = ILCursor
			?.GetAllMethods()
			.Where(static m => m.Name is "Remove" or "RemoveRange")
			.ToImmutableHashSet(MethodDefinitionComparer.Instance) ?? ImmutableHashSet<IMethodSymbol>.Empty;
		GotoMethods = ILCursor
			?.GetAllMethods()
			.Where(static m => m.Name is "GotoNext" or "GotoPrev")
			.ToImmutableHashSet(MethodDefinitionComparer.Instance) ?? ImmutableHashSet<IMethodSymbol>.Empty;
		InstructionMembers = Instruction
			?.GetMembers()
			.Where(static m => m.Name is "OpCode" or "Operand")
			.Select(static m => m.OriginalDefinition)
			.ToImmutableHashSet(SymbolEqualityComparer.Default) ?? ImmutableHashSet<ISymbol>.Empty;

		DetourContextParamlessUseMethod = MethodDefinitionComparer.Canonicalize(
			DetourContext
			?.GetAllMethods()
			.FirstOrDefault(static m => m.Name == KnownMetadataNames.DetourConfigContextUseMethod && m.Parameters.Length == 0)
		);

		Entity = comp.GetTypeByMetadataName(KnownMetadataNames.Entity);
		Component = comp.GetTypeByMetadataName(KnownMetadataNames.Component);
		SceneAsMethods = Entity
			?.GetAllMethods()
			.Where(static m => m.Name == KnownMetadataNames.SceneAsMethod)
			.Concat(
				Component
					?.GetAllMethods()
					.Where(static m => m.Name == KnownMetadataNames.SceneAsMethod)
					?? ImmutableArray<IMethodSymbol>.Empty
			)
			.ToImmutableHashSet(MethodDefinitionComparer.Instance) ?? ImmutableHashSet<IMethodSymbol>.Empty;

		TrackedAttribute = comp.GetTypeByMetadataName(KnownMetadataNames.TrackedAttribute);
		TrackedAsAttribute = comp.GetTypeByMetadataName(KnownMetadataNames.TrackedAsAttribute);

		Tracker = comp.GetTypeByMetadataName(KnownMetadataNames.Tracker);
		TrackerExtensions = comp.GetTypeByMetadataName(KnownMetadataNames.TrackerExtensions);
		TrackerOnlyLookupMethods = Tracker
			?.GetAllMethods()
			.Where(static m => m.Arity == 1 && !m.Name.Contains("TrackIfNeeded", StringComparison.Ordinal))
			.Concat(
				TrackerExtensions
					?.GetAllMethods()
					.Where(static m => m.Arity == 1 && !m.Name.Contains("TrackIfNeeded", StringComparison.Ordinal))
					?? ImmutableArray<IMethodSymbol>.Empty
			)
			.ToImmutableHashSet(MethodDefinitionComparer.Instance) ?? ImmutableHashSet<IMethodSymbol>.Empty;
		TrackerExtReplacedMethods = Tracker
			?.GetAllMethods()
			.Where(static m =>
				m.Name == KnownMetadataNames.TrackerGetEntityMethod ||
				m.Name == KnownMetadataNames.TrackerGetNearestEntityMethod ||
				m.Name == KnownMetadataNames.TrackerGetEntitiesMethod ||
				m.Name == KnownMetadataNames.TrackerGetEntitiesCopyMethod ||
				m.Name == KnownMetadataNames.TrackerGetComponentMethod ||
				m.Name == KnownMetadataNames.TrackerGetNearestComponentMethod ||
				m.Name == KnownMetadataNames.TrackerGetComponentsMethod ||
				m.Name == KnownMetadataNames.TrackerGetComponentsCopyMethod
			)
			.ToImmutableHashSet(MethodDefinitionComparer.Instance) ?? ImmutableHashSet<IMethodSymbol>.Empty;
		TrackerEnumerateMethods = Tracker
			?.GetAllMethods()
			.Where(static m =>
				m.Name == KnownMetadataNames.TrackerEnumerateEntitiesMethod ||
				m.Name == KnownMetadataNames.TrackerEnumerateComponentsMethod
			)
			.ToImmutableHashSet(MethodDefinitionComparer.Instance) ?? ImmutableHashSet<IMethodSymbol>.Empty;
		TrackerCountMethods = Tracker
			?.GetAllMethods()
			.Where(static m =>
				m.Name == KnownMetadataNames.TrackerCountEntitiesMethod ||
				m.Name == KnownMetadataNames.TrackerCountComponentsMethod
			)
			.ToImmutableHashSet(MethodDefinitionComparer.Instance) ?? ImmutableHashSet<IMethodSymbol>.Empty;

		EntityList = comp.GetTypeByMetadataName(KnownMetadataNames.EntityList);
		EntityListFindMethods = EntityList
			?.GetAllMethods()
			.Where(static m =>
				m.Name == KnownMetadataNames.EntityListFindFirstMethod ||
				m.Name == KnownMetadataNames.EntityListFindAllMethod
			)
			.ToImmutableHashSet(MethodDefinitionComparer.Instance) ?? ImmutableHashSet<IMethodSymbol>.Empty;

		Engine = comp.GetTypeByMetadataName(KnownMetadataNames.Engine);
		EngineEffectiveTimeRateField = Engine
			?.GetMembers()
			.OfType<IFieldSymbol>()
			.FirstOrDefault(static f => f.Name == KnownMetadataNames.EngineEffectiveTimeRateField)
			?.OriginalDefinition;
		NonStaticInitedEngineProperties = Engine
			?.GetMembers()
			.OfType<IPropertySymbol>()
			.Where(static p =>
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
			.Where(static p =>
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
			.Where(static f =>
				f.Name != KnownMetadataNames.GfxSubtractField &&
				f.Name != KnownMetadataNames.GfxDestinationTransparencySubtractField
			)
			.Select(static f => f.OriginalDefinition)
			.ToImmutableHashSet<IFieldSymbol>(SymbolEqualityComparer.Default) ?? ImmutableHashSet<IFieldSymbol>.Empty;

		VirtualContent = comp.GetTypeByMetadataName(KnownMetadataNames.VirtualContent);
		NonStaticInitedVirtualContentMethods = VirtualContent
			?.GetAllMethods()
			.Where(static m =>
				m.Name == KnownMetadataNames.VirtualContentCreateTextureMethod ||
				m.Name == KnownMetadataNames.VirtualContentCreateRenderTargetMethod
			)
			.ToImmutableHashSet(MethodDefinitionComparer.Instance) ?? ImmutableHashSet<IMethodSymbol>.Empty;

		VirtualRenderTarget = comp.GetTypeByMetadataName(KnownMetadataNames.VirtualRenderTarget);
		VirtualTexture = comp.GetTypeByMetadataName(KnownMetadataNames.VirtualTexture);
	}
}
