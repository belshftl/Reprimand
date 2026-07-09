// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Diagnostics;

internal static class Usage {
#pragma warning disable RS2008 // enable analyzer release tracking
	public static readonly DiagnosticDescriptor IllegalFieldWrite = new(
		id: "RM0200",
		title: "Field must not be written to",
		messageFormat: "Do not write to '{0}'; it is not a readonly field or get-only property due to a Celeste/Everest oversight, and should not be written to",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor IllegalTypeDerive = new(
		id: "RM0201",
		title: "Type must not be derived from",
		messageFormat: "Type '{0}' must not be derived from",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor IllegalStaticCtorFieldOrPropertyAccess = new(
		id: "RM0202",
		title: "Field/property should not be accessed in a static constructor",
		messageFormat: "Field/property '{0}' should not be accessed in a static constructor or static field/property initializer as it may still be null/uninitialized at this point; for an alternative, consider GameDependent<T> from Reprimand.Celeste or custom lazy init with `??=`",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor IllegalStaticCtorStaticMethodCall = new(
		id: "RM0203",
		title: "Static method should not be called in a static constructor",
		messageFormat: "Static method '{0}' should not be called in a static constructor or static field/property initializer, as due to Celeste/Everest shortsightedness, state it relies on may still be null/uninitialized at this point; for an alternative, consider GameDependent<T> from Reprimand.Celeste or custom lazy init with `??=`",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor IllegalStaticCtorObjectInstantiation = new(
		id: "RM0204",
		title: "Object should not be instantiated in a static constructor",
		messageFormat: "Objects of type '{0}' should not be instantiated in a static constructor or static field/property initializer, as due to Celeste/Everest shortsightedness, state its constructor relies on may still be null/uninitialized at this point; for an alternative, consider GameDependent<T> from Reprimand.Celeste or custom lazy init with `??=`",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor UnmarkedHookMethod = new(
		id: "RM0205",
		title: "Hook methods should be marked with [HookMethod]",
		messageFormat: "Hook methods should be marked with the [HookMethod] attribute from Reprimand.MonoMod for better analyzer diagnostics",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor NonStaticHookMethod = new(
		id: "RM0206",
		title: "Hook methods should be plain static methods",
		messageFormat:
		"Hook methods should be plain static methods; instance methods / capturing lambdas can cause all sorts of chaos by capturing state, and Action/Func/etc objects make it hard to pinpoint what actually gets used as the hook",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor HookStaticLambda = new(
		id: "RM0207",
		title: "Prefer plain static methods over static lambdas for hooks",
		messageFormat:
		"Prefer a plain static method over a static lambda for hooks; static lambdas make it harder to pinpoint the hook body or give it an identity for debugging/diagnostics/etc",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor NonStaticEmitDelegateMethod = new(
		id: "RM0208",
		title: "EmitDelegate argument should be a plain static method",
		messageFormat:
		"EmitDelegate should be passed a plain static method; instance methods / capturing lambdas can cause all sorts of chaos by capturing state, and Action/Func/etc objects make it hard to pinpoint what actually gets called",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor EmitDelegateStaticLambda = new(
		id: "RM0209",
		title: "Prefer plain static methods over static lambdas for EmitDelegate",
		messageFormat:
		"Prefer a plain static method over a static lambda for EmitDelegate, as static lambdas usually emit worse IL; what could be a plain call instruction becomes a capture of a delegate field on a compiler-generated class, and the callsite has to do weird castclass magic",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor DestructiveILEdit = new(
		id: "RM0210",
		title: "Avoid destructive IL edits",
		messageFormat:
		"Removing or modifying instructions commonly breaks IL hooks of other mods, which causes seemingly random crashes for players; prefer exclusively adding new instructions. For example, to patch out some code segment, skip it with an unconditional branch instead of deleting it.",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor PreferRequireGoto = new(
		id: "RM0211",
		title: "Use RequireGoto{Next,Prev} instead of plain Goto{Next,Prev}",
		messageFormat:
		"Use ILCursor.RequireGoto{{Next,Prev}} from Reprimand.MonoMod instead of plain Goto{{Next,Prev}} as they provide much better exceptions/messages on match failure",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor HookWithoutDetourId = new(
		id: "RM0212",
		title: "Declare hooks under detour IDs",
		messageFormat:
		"Hooks should be declared under a detour ID; ID-less hooks are much harder if not impossible to order against from other mods",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor NonNullableHookArgument = new(
		id: "RM0213",
		title: "Reference type hook parameters should be nullable",
		messageFormat: "'{0}' should be marked nullable; Celeste/Everest aren't nullable-aware, so reference type parameters other than `orig` and `self` should be maybe-null to catch potential null reference bugs",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor OrigNotCalled = new(
		id: "RM0214",
		title: "orig(...) not called in hook",
		messageFormat: "'{0}' should call its orig(...) delegate in at least one code path to give the rest of the detour chain a chance to run; consider making your changes conditional, e.g only if some setting is active or the player is in some map",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor YieldReturnOrig = new(
		id: "RM0215",
		title: "Avoid `yield return orig(...)`",
		messageFormat: "Due to how Celeste's coroutine system works, `yield return orig(...)` introduces a 1-frame delay; consider using `yield return new SwapImmediately(orig(...))` instead",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor DontUseSceneAsMethod = new(
		id: "RM0216",
		title: "Don't use SceneAs<T>",
		messageFormat: "Instead of `SceneAs<T>`, use `((T)Scene)` if a type mismatch should throw or `(Scene as T)` if you expect the cast to possibly fail; it's functionally identical, more idiomatic, and avoids erasing the nullable annotation since Celeste/Monocle aren't nullable-aware",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor UseExtTrackerMethods = new(
		id: "RM0217",
		title: "Use the tracker methods from Reprimand.Extensions",
		messageFormat: "Use the tracker methods that end in `Ext` from Reprimand.Extensions (`Tracker.GetEntityExt<T>`, etc.), as the vanilla ones neglect some type safety, are not nullable-aware, and have less clear error reporting",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor DontUseTrackerEnumerateMethods = new(
		id: "RM0218",
		title: "Don't use Tracker.EnumerateEntities<T> or Tracker.EnumerateComponents<T>",
		messageFormat: "Use Tracker.GetEntitiesExt<T> or Tracker.GetComponentsExt<T> instead of Tracker.EnumerateEntities<T> / Tracker.EnumerateComponents<T>; the overhead is either the same or even lower",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor DontUseTrackerCountMethods = new(
		id: "RM0219",
		title: "Don't use Tracker.CountEntities<T> or Tracker.CountComponents<T>",
		messageFormat: "Do `Tracker.GetEntitiesExt<T>().Count` or `Tracker.GetComponentsExt<T>().Count` instead of Tracker.CountEntities<T> or Tracker.CountComponents<T>; it's functionally identical, more idiomatic, and has clearer exceptions",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor NonTrackerLookupOfTrackedEntityType = new(
		id: "RM0220",
		title: "Use the Tracker for tracked entity types",
		messageFormat: "'{0}' is tracked; use Tracker.GetEntityExt<T> or Tracker.GetEntitiesExt<T> (both of which are from Reprimand.Extensions) instead of EntityList.Find{{First,All}} as it's much faster",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor TrackedAsIsClassOnly = new(
		id: "RM0221",
		title: "[TrackedAs] can only be applied to classes",
		messageFormat: "[TrackedAs] is only valid on class declarations; the attribute doesn't have `AttributeTargets.Class` set due to an Everest oversight",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor InvalidTrackedAsType = new(
		id: "RM0222",
		title: "Invalid [TrackedAs] type",
		messageFormat: "[TrackedAs] target type '{0}' must be a non-static reference-type class",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor UnrelatedTrackedAsType = new(
		id: "RM0223",
		title: "[TrackedAs] type is unrelated to the attributed class",
		messageFormat: "Type '{0}' must be the same as, or derive from, [TrackedAs] type '{1}'",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);
#pragma warning restore RS2008 // enable analyzer release tracking
}
