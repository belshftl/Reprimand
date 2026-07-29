// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Diagnostics;

internal static class Generators {
#pragma warning disable RS2008 // enable analyzer release tracking
	public static readonly DiagnosticDescriptor InvalidLogTag = new(
		id: "RM0400",
		title: "Invalid log tag",
		messageFormat: "Log tag must not be null",
		category: "Generators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor InvalidLogAlias = new(
		id: "RM0401",
		title: "Invalid log alias",
		messageFormat: "Log alias '{0}' is not a valid C# identifier",
		category: "Generators",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);
#pragma warning restore RS2008 // enable analyzer release tracking
}
