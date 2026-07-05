// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Diagnostics;

internal static class Language {
#pragma warning disable RS2008 // enable analyzer release tracking
	public static readonly DiagnosticDescriptor MissingOpenClassMarker = new(
		id: "RM0300",
		title: "Every class should be sealed/abstract or explicitly marked open",
		messageFormat: "You probably want `sealed class` instead of plain `class`, especially in a Celeste mod; if you know the difference and actually want a plain `class`, mark it with `/* open */`, for example, `public /* open */ class SomeEntity`",
		category: "Language",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor OpenClassMarkerNotAllowed = new(
		id: "RM0301",
		title: "Open class marker not allowed here",
		messageFormat: "The `/* open */` marker is only allowed on non-{{static/abstract/sealed}} classes or non-{{abstract/sealed}} records",
		category: "Language",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor MalformedOpenClassMarker = new(
		id: "RM0302",
		title: "Open class marker has invalid placement",
		messageFormat: "The `/* open */` marker must occur exactly once before the declaration keyword and be separated by horizontal whitespace",
		category: "Language",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor BaseExceptionTypeThrown = new(
		id: "RM0303",
		title: "Base System.Exception type thrown",
		messageFormat: "Prefer throwing more specific exception types instead of raw Exception, as raw Exception is poorly fit for `catch` or parsing error info; if there's no suitable built-in exception type, make your own",
		category: "Language",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);
#pragma warning restore RS2008 // enable analyzer release tracking
}
