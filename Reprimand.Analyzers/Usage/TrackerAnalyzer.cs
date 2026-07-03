// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Reprimand.Analyzers.Usage;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TrackerAnalyzer : DiagnosticAnalyzer {
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
		Diagnostics.Usage.UseExtTrackerMethods,
		Diagnostics.Usage.DontUseTrackerEnumerateMethods,
		Diagnostics.Usage.DontUseTrackerCountMethods
	);

	public override void Initialize(AnalysisContext context) {
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static ctx => {
				KnownSymbols known = new(ctx.Compilation);
				ctx.RegisterOperationAction(c => analyzeInvocation(c, known), OperationKind.Invocation);
			}
		);
	}

	private static void analyzeInvocation(OperationAnalysisContext ctx, KnownSymbols known) {
		var inv = (IInvocationOperation)ctx.Operation;
		if (known.TrackerExtReplacedMethods.Contains(inv.TargetMethod.OriginalDefinition))
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.UseExtTrackerMethods, inv.Syntax.GetLocation()));
		else if (known.TrackerEnumerateMethods.Contains(inv.TargetMethod.OriginalDefinition))
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.DontUseTrackerEnumerateMethods, inv.Syntax.GetLocation()));
		else if (known.TrackerCountMethods.Contains(inv.TargetMethod.OriginalDefinition))
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.DontUseTrackerCountMethods, inv.Syntax.GetLocation()));
	}
}
