// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Diagnostics;

internal static class Core {
#pragma warning disable RS2008 // enable analyzer release tracking
	public static readonly DiagnosticDescriptor InvalidLifecycleAttrMethodCandidate = new(
		id: "RM0001",
		title: "Invalid lifecycle attribute method candidate",
		messageFormat: "Lifecycle attribute method '{0}' must be static, non-generic, and return void",
		category: "Core",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor InvalidUnconditionalLifecycleAttrMethodParams = new(
		id: "RM0002",
		title: "Invalid unconditional lifecycle attribute method parameters",
		messageFormat: "Unconditionally triggering lifecycle attribute method '{0}' must take in 0 parameters",
		category: "Core",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor InvalidDepConditionalLifecycleAttrMethodParams = new(
		id: "RM0003",
		title: "Invalid optional-dependency lifecycle attribute method parameters",
		messageFormat: "Optional-dependency-triggering lifecycle attribute method '{0}' must take in either 0 parameters or a single non-in/out/ref parameter of type EverestModule",
		category: "Core",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor LifecycleAttrUndoMethodNotFound = new(
		id: "RM0004",
		title: "Undo method of lifecycle attribute not found",
		messageFormat: "Failed to find undo method '{0}' from lifecycle attribute; make sure this is the name of a method in the same class",
		category: "Core",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor AmbiguousLifecycleAttrUndoMethodName = new(
		id: "RM0005",
		title: "Undo method name of lifecycle attribute resolves to multiple methods",
		messageFormat: "Undo method name '{0}' from lifecycle attribute resolves to multiple methods; make sure the undo method is not overloaded",
		category: "Core",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor InvalidLifecycleAttrUndoMethod = new(
		id: "RM0006",
		title: "Invalid lifecycle attribute undo method",
		messageFormat: "Lifecycle attribute undo method '{0}' must be static, non-generic, take in 0 parameters, and return void",
		category: "Core",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor UseNameofForLifecycleAttrUndoMethodName = new(
		id: "RM0007",
		title: "Lifecycle attribute undo method name should use nameof",
		messageFormat: "Lifecycle attribute undo method name should use nameof(...), not a string literal or constant",
		category: "Core",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor SuggestedGlobalUsingNs = new(
		id: "RM0008",
		title: "Consider making this `using` globally implicit",
		messageFormat: "Consider adding '{0}' as a global using so you don't have to add it manually every time; add a `<Using>` in your .csproj or a `global using` in a dedicated file",
		category: "Core",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true
	);
#pragma warning restore RS2008 // enable analyzer release tracking
}
