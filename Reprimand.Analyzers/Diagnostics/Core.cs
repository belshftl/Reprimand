// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Diagnostics;

internal static class Core {
#pragma warning disable RS2008 // enable analyzer release tracking
	public static readonly DiagnosticDescriptor InvalidLifecycleAttrMethodCandidate = new(
		id: "RM0001",
		title: "Invalid lifecycle attribute method candidate",
		messageFormat: "Lifecycle attribute method '{0}' must be static and non-generic",
		category: "Core",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor InvalidUnconditionalLifecycleAttrMethodParams = new(
		id: "RM0002",
		title: "Invalid unconditional lifecycle attribute method parameters",
		messageFormat: "Unconditionally triggering lifecycle attribute method '{0}' must take in no parameters",
		category: "Core",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor InvalidDepConditionalLifecycleAttrMethodParams = new(
		id: "RM0003",
		title: "Invalid optional-dependency lifecycle attribute method parameters",
		messageFormat: "Optional-dependency-triggering lifecycle attribute method '{0}' must take in either no parameters or a single non-in/out/ref parameter of type EverestModule",
		category: "Core",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);
#pragma warning restore RS2008 // enable analyzer release tracking
}
