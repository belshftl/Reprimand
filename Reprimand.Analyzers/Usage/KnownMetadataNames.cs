// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

namespace Reprimand.Analyzers.Usage;

internal static class KnownMetadataNames {
	public const string Hook = "MonoMod.RuntimeDetour.Hook";
	public const string ILHook = "MonoMod.RuntimeDetour.ILHook";
	public const string NativeHook = "MonoMod.RuntimeDetour.NativeHook";
	public const string DetourConfig = "MonoMod.RuntimeDetour.DetourConfig";
	public const string ILCursor = "MonoMod.Cil.ILCursor";
	public const string ILContext = "MonoMod.Cil.ILContext";
	public const string Instruction = "Mono.Cecil.Cil.Instruction";

	public const string Entity = "Monocle.Entity";
	public const string Component = "Monocle.Component";
	public const string SceneAsMethod = "SceneAs";

	public const string TrackedAsAttribute = "Monocle.TrackedAsAttribute";
	public const string TrackedAsAttributeTypeField = "TrackedAsType";
	public const string TrackedAsAttributeInheritedField = "Inherited";

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
}
