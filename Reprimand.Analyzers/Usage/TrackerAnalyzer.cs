// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

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
		Diagnostics.Usage.DontUseTrackerCountMethods,
		Diagnostics.Usage.TrackerLookupOfNonTrackedEntityType,
		Diagnostics.Usage.NonTrackerLookupOfTrackedEntityType
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
		Location loc = inv.Syntax.GetLocation();
		IMethodSymbol method = inv.TargetMethod;
		if (known.TrackerOnlyLookupMethods.Contains(method)) {
			if (!method.IsGenericMethod || method.TypeArguments.Length != 1)
				goto next;
			ITypeSymbol typeParam = method.TypeArguments[0];
			if (!typeParam.IsReferenceType)
				goto next;
			if (!typeParam.GetAttributes().Any(a => a.AttributeClass.IsOrDerivesFrom(known.TrackedAttribute)))
				ctx.ReportDiagnostic(
					Diagnostic.Create(
						Diagnostics.Usage.TrackerLookupOfNonTrackedEntityType,
						loc,
						typeParam.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)
					)
				);
		}
	next:
		if (known.TrackerExtReplacedMethods.Contains(method)) {
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.UseExtTrackerMethods, loc));
		} else if (known.TrackerEnumerateMethods.Contains(method)) {
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.DontUseTrackerEnumerateMethods, loc));
		} else if (known.TrackerCountMethods.Contains(method)) {
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.DontUseTrackerCountMethods, loc));
		} else if (known.EntityListFindMethods.Contains(method)) {
			if (!method.IsGenericMethod || method.TypeArguments.Length != 1)
				return;
			ITypeSymbol typeParam = method.TypeArguments[0];
			if (!typeParam.IsReferenceType)
				return;
			if (typeParam.GetAttributes().Any(a => a.AttributeClass.IsOrDerivesFrom(known.TrackedAttribute)))
				ctx.ReportDiagnostic(
					Diagnostic.Create(
						Diagnostics.Usage.NonTrackerLookupOfTrackedEntityType,
						loc,
						typeParam.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
						method.Name == "FindFirst" ? "GetEntityExt" : "GetEntitiesExt",
						method.Name
					)
				);
		}
	}
}
