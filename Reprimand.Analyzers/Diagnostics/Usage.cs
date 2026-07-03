// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Diagnostics;

internal static class Usage {
#pragma warning disable RS2008 // enable analyzer release tracking
	public static readonly DiagnosticDescriptor NonStaticHookMethod = new(
		id: "RM0200",
		title: "Hook methods should be plain static methods",
		messageFormat:
		"Hook methods should be plain static methods; instance methods / capturing lambdas can cause all sorts of chaos by capturing state, and Action/Func/etc objects make it hard to pinpoint what actually gets used as the hook",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor HookStaticLambda = new(
		id: "RM0201",
		title: "Prefer plain static methods over static lambdas for hooks",
		messageFormat:
		"Prefer a plain static method over a static lambda for hooks; static lambdas make it harder to pinpoint the hook body or give it an identity for debugging/diagnostics/etc",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor NonStaticEmitDelegateMethod = new(
		id: "RM0202",
		title: "EmitDelegate argument should be a plain static methods",
		messageFormat:
		"EmitDelegate should be passed a plain static method; instance methods / capturing lambdas can cause all sorts of chaos by capturing state, and Action/Func/etc objects make it hard to pinpoint what actually gets called",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor EmitDelegateStaticLambda = new(
		id: "RM0203",
		title: "Prefer plain static methods over static lambdas for EmitDelegate",
		messageFormat:
		"Prefer a plain static method over a static lambda for EmitDelegate, as static lambdas usually emit worse IL; what could be a plain call instruction becomes a capture of a delegate field on a compiler-generated class, and the callsite has to do weird castclass magic",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor DestructiveILEdit = new(
		id: "RM0204",
		title: "Avoid destructive IL edits",
		messageFormat:
		"Removing or modifying instructions commonly breaks IL hooks of other mods, which causes seemingly random crashes for players; prefer exclusively adding new instructions. For example, to patch out some code segment, skip it with an unconditional branch instead of deleting it.",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor PreferRequireGoto = new(
		id: "RM0205",
		title: "Use RequireGoto{Next,Prev} instead of plain Goto{Next,Prev}",
		messageFormat:
		"Use ILCursor.RequireGoto{Next,Prev} from Reprimand.MonoMod instead of plain Goto{Next,Prev} as they provide much better exceptions/messages on match failure",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor HookWithoutDetourID = new(
		id: "RM0206",
		title: "Declare hooks under detour IDs",
		messageFormat:
		"Hooks should be declared under a detour ID; ID-less hooks are much harder if not impossible to order against from other mods, so not using one will create a huge pain for someone else that may want to take priority over your hooks",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor OrigNotCalled = new(
		id: "RM0207",
		title: "orig(...) not called in hook",
		messageFormat: "Call the orig(...) method in at least one code path to allow other hooks to run properly; consider making your changes only run conditionally, e.g if some setting is active or the player is in some map",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor YieldReturnOrig = new(
		id: "RM0208",
		title: "Avoid `yield return orig(...)`",
		messageFormat: "Due to how Celeste's coroutine system works, `yield return orig(...)` introduces a 1-frame delay; consider using `yield return new SwapImmediately(orig(...))` instead",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor DontUseSceneAsMethod = new(
		id: "RM0209",
		title: "Don't use SceneAs<T>",
		messageFormat: "Instead of `SceneAs<T>`, use `((T)Scene)` if a type mismatch should throw or `(Scene as T)` if you expect the cast to possibly fail; it's functionally identical, more idiomatic, and avoids erasing the nullable annotation since Celeste/Monocle aren't nullable-aware",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor UseTypedTrackerMethods = new(
		id: "RM0210",
		title: "Use the typed tracker methods from Reprimand.Celeste",
		messageFormat: "Use the typed tracker methods (`Tracker.GetEntityTyped<T>`, `Tracker.GetEntitiesTyped<T>`, etc.) from Reprimand.Celeste, as the vanilla ones neglect some type safety and aren't nullable-aware",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor NonTrackerLookupOfTrackedEntityType = new(
		id: "RM0211",
		title: "Use the Tracker for tracked entity types",
		messageFormat: "'{0}' is tracked; use Tracker.GetEntityTyped<T> or Tracker.GetEntitiesTyped<T> (both from Reprimand.Celeste) instead of EntityList.Find{First,All} as it's much faster",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor TrackedAsIsClassOnly = new(
		id: "RM0212",
		title: "[TrackedAs] can only be applied to classes",
		messageFormat: "[TrackedAs] is only valid on class declarations",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor InvalidTrackedAsType = new(
		id: "RM0213",
		title: "Invalid [TrackedAs] type",
		messageFormat: "[TrackedAs] target type '{0}' must be a non-static reference-type class",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor UnrelatedTrackedAsType = new(
		id: "RM0214",
		title: "[TrackedAs] type is unrelated to the attributed class",
		messageFormat: "Type '{0}' must be the same as, or derive from, [TrackedAs] type '{1}'",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor TrackedAsDerivedFrom = new(
		id: "RM0215",
		title: "Do not derive from TrackedAsAttribute",
		messageFormat: "Type '{0}' must not derive from TrackedAsAttribute",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor TrackedAsFieldWrite = new(
		id: "RM0216",
		title: "Do not write to fields in TrackedAsAttribute",
		messageFormat: "Do not write to '{0}'; the fields in TrackedAsAttribute are not readonly fields or get-only properties due to an Everest oversight, and should not be written to",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);
#pragma warning restore RS2008 // enable analyzer release tracking
}
