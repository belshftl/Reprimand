// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Reprimand.Analyzers.Usage;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GfxAnalyzer : DiagnosticAnalyzer {
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
		Diagnostics.Usage.IllegalFieldWrite
	);

	public override void Initialize(AnalysisContext context) {
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static ctx => {
				KnownSymbols known = new(ctx.Compilation);
				if (known.Gfx is null)
					return;
				ctx.RegisterOperationAction(c => analyzeFieldReference(c, known), OperationKind.FieldReference);
			}
		);
	}

	private static void analyzeFieldReference(OperationAnalysisContext ctx, KnownSymbols known) {
		var fr = (IFieldReferenceOperation)ctx.Operation;
		if (
			!(
				SymbolEqualityComparer.Default.Equals(fr.Field.OriginalDefinition.ContainingType, known.Draw) ||
				SymbolEqualityComparer.Default.Equals(fr.Field.OriginalDefinition.ContainingType, known.Gfx) ||
				SymbolEqualityComparer.Default.Equals(fr.Field.OriginalDefinition.ContainingType, known.TrackedAsAttribute)
			)
		)
			return;
		if (fr.IsWrite(out Location? loc))
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.IllegalFieldWrite, loc, fr.Field.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
	}
}
