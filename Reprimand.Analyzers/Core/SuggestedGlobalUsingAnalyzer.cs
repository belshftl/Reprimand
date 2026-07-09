// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Reprimand.Analyzers.Core;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SuggestedGlobalUsingAnalyzer : DiagnosticAnalyzer {
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
		Diagnostics.Core.SuggestedGlobalUsingNs
	);

	public override void Initialize(AnalysisContext context) {
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static ctx => {
				KnownSymbols known = new(ctx.Compilation);
				ctx.RegisterSyntaxNodeAction(c => analyzeUsingDirective(c, known), SyntaxKind.UsingDirective);
			}
		);
	}

	private static void analyzeUsingDirective(SyntaxNodeAnalysisContext ctx, KnownSymbols known) {
		var sx = (UsingDirectiveSyntax)ctx.Node;
		if (sx.GlobalKeyword.RawKind > 0) // global using ...;
			return;
		if (sx.Alias is not null)
			return; // TODO
		ISymbol? sym = ctx.SemanticModel.GetSymbolInfo(sx.Name!, ctx.CancellationToken).Symbol;
		if (sym is not INamespaceSymbol ns)
			return;
		if (
			SymbolEqualityComparer.Default.Equals(ns, known.ReprimandExtensionsNs) ||
			SymbolEqualityComparer.Default.Equals(ns, known.ReprimandLifecycleNs) ||
			SymbolEqualityComparer.Default.Equals(ns, known.ReprimandMonoModNs)
		)
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Core.SuggestedGlobalUsingNs, sx.GetLocation(), ns.ToDisplayString()));
	}
}
