// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

namespace Reprimand.Analyzers.Usage;

internal static class KnownMetadataNames {
	public const string DontUseInStaticCtorAttribute = "Reprimand.CodeAnalysis.DontUseInStaticCtorAttribute";
	public const string IOnLoadLifecycleAttribute = "Reprimand.Lifecycle.IOnLoadLifecycleAttribute";
	public const string HookMethodAttribute = "Reprimand.MonoMod.HookMethodAttribute";

	public const string Hook = "MonoMod.RuntimeDetour.Hook";
	public const string ILHook = "MonoMod.RuntimeDetour.ILHook";
	public const string NativeHook = "MonoMod.RuntimeDetour.NativeHook";
	public const string DetourConfig = "MonoMod.RuntimeDetour.DetourConfig";
	public const string DetourContext = "MonoMod.RuntimeDetour.DetourContext";
	public const string DetourConfigContext = "MonoMod.RuntimeDetour.DetourConfigContext";
	public const string ILCursor = "MonoMod.Cil.ILCursor";
	public const string ILContext = "MonoMod.Cil.ILContext";
	public const string Instruction = "Mono.Cecil.Cil.Instruction";

	public const string DetourConfigContextUseMethod = "Use";

	public const string Entity = "Monocle.Entity";
	public const string Component = "Monocle.Component";
	public const string SceneAsMethod = "SceneAs";

	public const string TrackedAttribute = "Monocle.Tracked"; // not a typo, the type doesn't have Attribute in its name for some reason
	public const string TrackedAsAttribute = "Monocle.TrackedAsAttribute";

	public const string Tracker = "Monocle.Tracker";
	public const string TrackerGetEntityMethod = "GetEntity";
	public const string TrackerGetNearestEntityMethod = "GetNearestEntity";
	public const string TrackerGetEntitiesMethod = "GetEntities";
	public const string TrackerGetEntitiesCopyMethod = "GetEntitiesCopy";
	public const string TrackerGetComponentMethod = "GetComponent";
	public const string TrackerGetNearestComponentMethod = "GetNearestComponent";
	public const string TrackerGetComponentsMethod = "GetComponents";
	public const string TrackerGetComponentsCopyMethod = "GetComponentsCopy";

	public const string TrackerEnumerateEntitiesMethod = "EnumerateEntities";
	public const string TrackerEnumerateComponentsMethod = "EnumerateComponents";

	public const string TrackerCountEntitiesMethod = "CountEntities";
	public const string TrackerCountComponentsMethod = "CountComponents";

	public const string EntityList = "Monocle.EntityList";
	public const string EntityListFindFirstMethod = "FindFirst";
	public const string EntityListFindAllMethod = "FindAll";

	public const string Engine = "Monocle.Engine";
	public const string EngineInstanceProperty = "Instance";
	public const string EngineGraphicsProperty = "Graphics";
	public const string EngineCommandsProperty = "Commands";
	public const string EnginePoolerProperty = "Pooler";

	public const string Draw = "Monocle.Draw";
	public const string DrawParticleField = "Particle";
	public const string DrawPixelField = "Pixel";
	public const string DrawRendererProperty = "Renderer";
	public const string DrawSpriteBatchProperty = "SpriteBatch";
	public const string DrawDefaultFontProperty = "DefaultFont";

	public const string Gfx = "Celeste.GFX";
	public const string GfxSubtractField = "Subtract";
	public const string GfxDestinationTransparencySubtractField = "DestinationTransparencySubtract";

	public const string VirtualContent = "Monocle.VirtualContent";
	public const string VirtualContentCreateTextureMethod = "CreateTexture";
	public const string VirtualContentCreateRenderTargetMethod = "CreateRenderTarget";

	public const string VirtualRenderTarget = "Monocle.VirtualRenderTarget";
	public const string VirtualTexture = "Monocle.VirtualTexture";
}
