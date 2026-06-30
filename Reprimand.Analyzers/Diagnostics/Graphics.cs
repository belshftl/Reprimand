// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Diagnostics;

internal static class Graphics {
#pragma warning disable RS2008 // enable analyzer release tracking
	public static readonly DiagnosticDescriptor SpriteBatchOwnedExternally = new(
		id: "RM0100",
		title: "SpriteBatch ownership is restricted",
		messageFormat: "Celeste renders through a singular global SpriteBatch, which you can use through Reprimand.Graphics.GlobalSpriteBatch; SpriteBatch instances must not be stored, passed, returned, or owned elsewhere",
		category: "Graphics",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor NonGlobalSpriteBatchUsage = new(
		id: "RM0101",
		title: "Use the global SpriteBatch",
		messageFormat: "Celeste renders through a singular global SpriteBatch, which you can use through Reprimand.Graphics.GlobalSpriteBatch; use that instead of this value",
		category: "Graphics",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	public static readonly DiagnosticDescriptor DrawSpriteBatchUsed = new(
		id: "RM0102",
		title: "Use the global SpriteBatch through GlobalSpriteBatch",
		messageFormat: "Use the global SpriteBatch through GlobalSpriteBatch.Batch instead of Draw.SpriteBatch for stylistic consistency",
		category: "Graphics",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true
	);
#pragma warning restore RS2008 // enable analyzer release tracking
}
