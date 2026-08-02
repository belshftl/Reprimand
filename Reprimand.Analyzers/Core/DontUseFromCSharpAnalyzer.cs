// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Reprimand.Analyzers.Core;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DontUseFromCSharpAnalyzer : DiagnosticAnalyzer {
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
		Diagnostics.Core.DontUseFromCSharp
	);

	public override void Initialize(AnalysisContext context) {
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static ctx => {
				KnownSymbols known = new(ctx.Compilation);
				if (known.DontUseFromCSharpAttribute is null)
					return;
				ctx.RegisterSyntaxNodeAction(c => analyzeExplicitName(c, known), SyntaxKind.IdentifierName, SyntaxKind.GenericName);
				ctx.RegisterOperationAction(
					c => analyzeUnnamedUse(c, known),
					OperationKind.PropertyReference,
					OperationKind.Conversion,
					OperationKind.Unary,
					OperationKind.Binary,
					OperationKind.CompoundAssignment,
					OperationKind.Increment,
					OperationKind.Decrement,
					OperationKind.ObjectCreation
				);
			}
		);
	}

	private static void analyzeExplicitName(SyntaxNodeAnalysisContext ctx, KnownSymbols known) {
		if (known.DontUseFromCSharpAttribute is null)
			return;
		var name = (SimpleNameSyntax)ctx.Node;
		if (name.IsPartOfStructuredTrivia())
			return;
		if (name.IsVar && SyntaxFacts.IsInTypeOnlyContext(name))
			return;
		ISymbol? symbol = ctx.SemanticModel.GetSymbolInfo(name, ctx.CancellationToken).Symbol;
		ISymbol? marked = getMarkedDefinition(symbol, known.DontUseFromCSharpAttribute);
		if (marked is null)
			return;
		ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Core.DontUseFromCSharp, name.GetLocation(), marked.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
	}

	private static void analyzeUnnamedUse(OperationAnalysisContext ctx, KnownSymbols known) {
		if (known.DontUseFromCSharpAttribute is null)
			return;
		ISymbol? symbol = ctx.Operation switch {
			IPropertyReferenceOperation prop when prop.Property.IsIndexer => prop.Property,
			IConversionOperation conv => conv.OperatorMethod,
			IUnaryOperation unary => unary.OperatorMethod,
			IBinaryOperation binary => binary.OperatorMethod,
			ICompoundAssignmentOperation asg => asg.OperatorMethod,
			IIncrementOrDecrementOperation incr => incr.OperatorMethod,
			IObjectCreationOperation creat when creat.Syntax is ImplicitObjectCreationExpressionSyntax => creat.Constructor?.ContainingType ?? creat.Type,
			_ => null
		};
		ISymbol? marked = getMarkedDefinition(symbol, known.DontUseFromCSharpAttribute);
		if (marked is null)
			return;
		ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Core.DontUseFromCSharp, ctx.Operation.Syntax.GetLocation(), marked.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
	}

	private static ISymbol? getMarkedDefinition(ISymbol? symbol, INamedTypeSymbol attr) {
		if (symbol is null)
			return null;
		symbol = normalize(symbol);
		foreach (AttributeData a in symbol.GetAttributes())
			if (SymbolEqualityComparer.Default.Equals(a.AttributeClass, attr))
				return symbol;
		return null;
	}

	private static ISymbol normalize(ISymbol symbol) {
		if (symbol is IAliasSymbol alias)
			symbol = alias.Target;
		if (symbol is IMethodSymbol method) {
			if (method.MethodKind == MethodKind.Constructor || method.MethodKind == MethodKind.StaticConstructor)
				symbol = method.ContainingType;
			else if (method.ReducedFrom is not null)
				symbol = method.ReducedFrom;
		}
		return symbol.OriginalDefinition;
	}
}
