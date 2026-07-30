// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Reprimand.Analyzers.Usage;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FrozenUpdateTagAnalyzer : DiagnosticAnalyzer {
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
		Diagnostics.Usage.DontUseFrozenUpdateTag
	);

	public override void Initialize(AnalysisContext context) {
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static ctx => {
				KnownSymbols known = new(ctx.Compilation);
				if (known.RmTags is null) // don't report the diagnostic if the suggested fix isn't even available
					return;
				ctx.RegisterOperationAction(c => analyzeFieldReference(c, known), OperationKind.FieldReference);
			}
		);
	}

	private static void analyzeFieldReference(OperationAnalysisContext ctx, KnownSymbols known) {
		var fr = (IFieldReferenceOperation)ctx.Operation;
		if (!SymbolEqualityComparer.Default.Equals(fr.Field.OriginalDefinition, known.TagsFrozenUpdateField))
			return;
		ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.DontUseFrozenUpdateTag, fr.Syntax.GetLocation()));
	}
}
