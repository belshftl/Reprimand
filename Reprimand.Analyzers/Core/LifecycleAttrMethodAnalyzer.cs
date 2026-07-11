// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Reprimand.Analyzers.Core;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LifecycleAttrMethodAnalyzer : DiagnosticAnalyzer {
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
		Diagnostics.Core.InvalidLifecycleAttrMethodCandidate,
		Diagnostics.Core.InvalidUnconditionalLifecycleAttrMethodParams,
		Diagnostics.Core.InvalidDepConditionalLifecycleAttrMethodParams,
		Diagnostics.Core.LifecycleAttrUndoMethodNotFound,
		Diagnostics.Core.AmbiguousLifecycleAttrUndoMethodName,
		Diagnostics.Core.InvalidLifecycleAttrUndoMethod,
		Diagnostics.Core.UseNameofForLifecycleAttrUndoMethodName
	);

	public override void Initialize(AnalysisContext context) {
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static ctx => {
				KnownSymbols known = new(ctx.Compilation);
				if (
					known.OnLoadAttribute is null ||
					known.OnLoadOneshotAttribute is null ||
					known.OnLoadIfOptionalDepAttribute is null ||
					known.OnLoadIfOptionalDepOneshotAttribute is null ||
					known.EverestModule is null
				)
					return;
				ctx.RegisterSymbolAction(c => analyzeMethod(c, known), SymbolKind.Method);
			}
		);
	}

	private static void analyzeMethod(SymbolAnalysisContext ctx, KnownSymbols known) {
		var sym = (IMethodSymbol)ctx.Symbol;
		AttributeData? attr = null;
		bool isDepConditional = false;
		foreach (AttributeData a in sym.GetAttributes())
			if (
				SymbolEqualityComparer.Default.Equals(a.AttributeClass, known.OnLoadAttribute) ||
				SymbolEqualityComparer.Default.Equals(a.AttributeClass, known.OnLoadOneshotAttribute)
			) {
				attr = a;
			} else if (
				SymbolEqualityComparer.Default.Equals(a.AttributeClass, known.OnLoadIfOptionalDepAttribute) ||
				SymbolEqualityComparer.Default.Equals(a.AttributeClass, known.OnLoadIfOptionalDepOneshotAttribute)
			) {
				attr = a;
				isDepConditional = true;
			}
		if (attr is null)
			return;
		analyzeAttributeReference(ctx, attr);
		Location? loc = attr.ApplicationSyntaxReference?.GetSyntax(ctx.CancellationToken).GetLocation() ??
			sym.Locations.FirstOrDefault(static l => l.IsInSource);
		if (!sym.IsStatic || sym.IsGenericMethod || !sym.ReturnsVoid)
			ctx.ReportDiagnostic(
				Diagnostic.Create(Diagnostics.Core.InvalidLifecycleAttrMethodCandidate, loc, sym.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat))
			);

		if (!isDepConditional) {
			if (sym.Parameters.Length != 0)
				ctx.ReportDiagnostic(
					Diagnostic.Create(
						Diagnostics.Core.InvalidUnconditionalLifecycleAttrMethodParams,
						loc,
						sym.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)
					)
				);
		} else {
			if (sym.Parameters.Length != 0 &&
				!(
					sym.Parameters.Length == 1 && sym.Parameters[0].RefKind == RefKind.None &&
					SymbolEqualityComparer.Default.Equals(sym.Parameters[0].Type, known.EverestModule)
				)
			)
				ctx.ReportDiagnostic(
					Diagnostic.Create(
						Diagnostics.Core.InvalidDepConditionalLifecycleAttrMethodParams,
						loc,
						sym.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)
					)
				);
		}
	}

	private static void analyzeAttributeReference(SymbolAnalysisContext ctx, AttributeData attr) {
		SyntaxReference? sr = attr.ApplicationSyntaxReference;
		if (sr is null)
			return;
		var sx = sr.GetSyntax(ctx.CancellationToken) as AttributeSyntax;
		if (sx?.ArgumentList is null || sx.ArgumentList.Arguments.Count == 0)
			return;
		Location? loc = sx.ArgumentList.Arguments[0].Expression.GetLocation();

		string? methodName = null;
		foreach (KeyValuePair<string, TypedConstant> named in attr.NamedArguments)
			if (named is { Key: "UndoMethod", Value.Value: string s }) {
				methodName = s;
				ExpressionSyntax? node = sx.ArgumentList.Arguments.FirstOrDefault(a => a.NameEquals?.Name.Identifier.ValueText == named.Key)?.Expression;
				if (node is not InvocationExpressionSyntax)
					ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Core.UseNameofForLifecycleAttrUndoMethodName, loc));
				break;
			}
		if (methodName is null)
			return;

		INamedTypeSymbol containingType = ctx.Symbol as INamedTypeSymbol ?? ctx.Symbol.ContainingType;
		if (containingType is null)
			return;

		var methods = containingType
			.GetMembers(methodName)
			.OfType<IMethodSymbol>()
			.Where(static m => m.MethodKind == MethodKind.Ordinary)
			.ToImmutableArray();

		if (methods.Length == 0) {
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Core.LifecycleAttrUndoMethodNotFound, loc, methodName));
			return;
		}
		if (methods.Length != 1) {
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Core.AmbiguousLifecycleAttrUndoMethodName, loc, methodName));
			return;
		}
		IMethodSymbol m = methods[0];
		if (!m.IsStatic || m.IsGenericMethod || m.Parameters.Length != 0 || !m.ReturnsVoid)
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Core.InvalidLifecycleAttrUndoMethod, loc, m.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
	}
}
