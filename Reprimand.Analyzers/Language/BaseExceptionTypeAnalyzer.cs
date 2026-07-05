// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Reprimand.Analyzers.Language;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BaseExceptionTypeAnalyzer : DiagnosticAnalyzer {
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
		Diagnostics.Language.BaseExceptionTypeThrown
	);

	public override void Initialize(AnalysisContext context) {
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static ctx => {
				KnownTypes known = new(ctx.Compilation);
				if (known.Exception is null)
					return;
				ctx.RegisterOperationAction(c => analyzeThrow(c, known), OperationKind.Throw);
			}
		);
	}

	private static void analyzeThrow(OperationAnalysisContext ctx, KnownTypes known) {
		var @throw = (IThrowOperation)ctx.Operation;
		IOperation? thrown = @throw.Exception;
		if (thrown?.Type is null) // throw;
			return;
		if (thrown.ConstantValue is { HasValue: true, Value: null }) // throw null!;
			return;
		if (thrown is IConversionOperation { IsImplicit: true, Operand: IOperation operand } conv && SymbolEqualityComparer.Default.Equals(conv.Type, known.Exception))
			thrown = operand;
		if (!SymbolEqualityComparer.Default.Equals(thrown.Type, known.Exception))
			return;
		ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Language.BaseExceptionTypeThrown, @throw.Syntax.GetLocation()));
	}
}
