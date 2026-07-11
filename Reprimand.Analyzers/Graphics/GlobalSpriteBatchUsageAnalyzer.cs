// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Reprimand.Analyzers.Graphics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GlobalSpriteBatchUsageAnalyzer : DiagnosticAnalyzer {
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
		Diagnostics.Graphics.SpriteBatchOwnedExternally,
		Diagnostics.Graphics.NonGlobalSpriteBatchUsage,
		Diagnostics.Graphics.DrawSpriteBatchUsed
	);

	public override void Initialize(AnalysisContext context) {
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static ctx => {
				KnownSymbols known = new(ctx.Compilation);
				if (known.SpriteBatch is null || known.GlobalSpriteBatch is null)
					return;

				ctx.RegisterSyntaxNodeAction(c => analyzeVariableDeclaration(c, known), SyntaxKind.VariableDeclaration);
				ctx.RegisterSyntaxNodeAction(c => analyzeParameter(c, known), SyntaxKind.Parameter);
				ctx.RegisterSyntaxNodeAction(
					c => analyzeTypedDeclaration(c, known),
					SyntaxKind.PropertyDeclaration,
					SyntaxKind.IndexerDeclaration,
					SyntaxKind.EventDeclaration,
					SyntaxKind.MethodDeclaration,
					SyntaxKind.LocalFunctionStatement,
					SyntaxKind.DelegateDeclaration,
					SyntaxKind.OperatorDeclaration,
					SyntaxKind.ConversionOperatorDeclaration,
					SyntaxKind.SimpleBaseType,
					SyntaxKind.TypeConstraint,
					SyntaxKind.DeclarationPattern,
					SyntaxKind.RecursivePattern
				);
				ctx.RegisterSyntaxNodeAction(c => analyzeForEachStatement(c, known), SyntaxKind.ForEachStatement);
				ctx.RegisterSyntaxNodeAction(c => analyzeDeclarationExpression(c, known), SyntaxKind.DeclarationExpression);
				ctx.RegisterOperationAction(
					c => analyzeSpriteBatchValue(c, known),
					OperationKind.LocalReference,
					OperationKind.ParameterReference,
					OperationKind.FieldReference,
					OperationKind.PropertyReference,
					OperationKind.ArrayElementReference,
					OperationKind.Invocation,
					OperationKind.Conversion,
					OperationKind.Await,
					OperationKind.InstanceReference
				);
			}
		);
	}

	private static void analyzeVariableDeclaration(SyntaxNodeAnalysisContext ctx, KnownSymbols known) {
		if (isWithinGlobalSpriteBatch(ctx, known))
			return;
		var decl = (VariableDeclarationSyntax)ctx.Node;
		ITypeSymbol? type = ctx.SemanticModel.GetTypeInfo(decl.Type, ctx.CancellationToken).Type;
		if (type is null && decl.Variables.Count != 0) {
			ISymbol? sym = ctx.SemanticModel.GetDeclaredSymbol(decl.Variables[0], ctx.CancellationToken);
			type = getDeclaredValueType(sym);
		}
		reportDeclaration(ctx, known, type, decl.Type.GetLocation());
	}

	private static void analyzeParameter(SyntaxNodeAnalysisContext ctx, KnownSymbols known) {
		if (isWithinGlobalSpriteBatch(ctx, known))
			return;
		var param = (ParameterSyntax)ctx.Node;
		IParameterSymbol? sym = ctx.SemanticModel.GetDeclaredSymbol(param, ctx.CancellationToken);
		reportDeclaration(ctx, known, sym?.Type, param.Type?.GetLocation() ?? param.Identifier.GetLocation());
	}

	private static void analyzeTypedDeclaration(SyntaxNodeAnalysisContext ctx, KnownSymbols known) {
		if (isWithinGlobalSpriteBatch(ctx, known))
			return;
		TypeSyntax? typeSyntax = ctx.Node switch {
			PropertyDeclarationSyntax decl => decl.Type,
			IndexerDeclarationSyntax decl => decl.Type,
			EventDeclarationSyntax decl => decl.Type,
			MethodDeclarationSyntax decl => decl.ReturnType,
			LocalFunctionStatementSyntax decl => decl.ReturnType,
			DelegateDeclarationSyntax decl => decl.ReturnType,
			OperatorDeclarationSyntax decl => decl.ReturnType,
			ConversionOperatorDeclarationSyntax decl => decl.Type,
			SimpleBaseTypeSyntax decl => decl.Type,
			TypeConstraintSyntax decl => decl.Type,
			DeclarationPatternSyntax decl => decl.Type,
			RecursivePatternSyntax decl => decl.Type,
			_ => null,
		};
		if (typeSyntax is null)
			return;
		ITypeSymbol? type = ctx.SemanticModel.GetTypeInfo(typeSyntax, ctx.CancellationToken).Type;
		reportDeclaration(ctx, known, type, typeSyntax.GetLocation());
	}

	private static void analyzeForEachStatement(SyntaxNodeAnalysisContext ctx, KnownSymbols known) {
		if (isWithinGlobalSpriteBatch(ctx, known))
			return;
		var stmt = (ForEachStatementSyntax)ctx.Node;
		ILocalSymbol? sym = ctx.SemanticModel.GetDeclaredSymbol(stmt, ctx.CancellationToken);
		ITypeSymbol? type = sym?.Type ?? ctx.SemanticModel.GetTypeInfo(stmt.Type, ctx.CancellationToken).Type;
		reportDeclaration(ctx, known, type, stmt.Type.GetLocation());
	}

	private static void analyzeDeclarationExpression(SyntaxNodeAnalysisContext ctx, KnownSymbols known) {
		if (isWithinGlobalSpriteBatch(ctx, known) || known.SpriteBatch is null)
			return;
		var decl = (DeclarationExpressionSyntax)ctx.Node;
		ITypeSymbol? type = ctx.SemanticModel.GetTypeInfo(decl.Type, ctx.CancellationToken).Type;
		if (type is null)
			foreach (SingleVariableDesignationSyntax dsg in decl.Designation.DescendantNodesAndSelf().OfType<SingleVariableDesignationSyntax>()) {
				var sym = ctx.SemanticModel.GetDeclaredSymbol(dsg, ctx.CancellationToken) as ILocalSymbol;
				if (sym is not null && containsSpriteBatch(sym.Type, known.SpriteBatch)) {
					type = sym.Type;
					break;
				}
			}
		reportDeclaration(ctx, known, type, decl.Type.GetLocation());
	}

	private static void analyzeSpriteBatchValue(OperationAnalysisContext ctx, KnownSymbols known) {
		if (known.GlobalSpriteBatch is null || isWithinGlobalSpriteBatch(ctx.ContainingSymbol, known.GlobalSpriteBatch))
			return;
		IOperation op = ctx.Operation;
		if (!op.Type.IsOrDerivesFrom(known.SpriteBatch))
			return;
		if (isWithinNameOf(op))
			return;
		if (known.GlobalSpriteBatchProperty is null || matchesProperty(op, known.GlobalSpriteBatchProperty))
			return;
		if (known.DrawSpriteBatchProperty is not null && matchesProperty(op, known.DrawSpriteBatchProperty))
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Graphics.DrawSpriteBatchUsed, op.Syntax.GetLocation()));
		else
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Graphics.NonGlobalSpriteBatchUsage, op.Syntax.GetLocation()));
	}

	private static void reportDeclaration(SyntaxNodeAnalysisContext ctx, KnownSymbols known, ITypeSymbol? type, Location loc) {
		if (known.SpriteBatch is null || !containsSpriteBatch(type, known.SpriteBatch))
			return;
		ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Graphics.SpriteBatchOwnedExternally, loc));
	}

	private static bool matchesProperty(IOperation op, IPropertySymbol prop) {
		while (op is IConversionOperation conv)
			op = conv.Operand;
		return op is IPropertyReferenceOperation propRef && SymbolEqualityComparer.Default.Equals(propRef.Property, prop);
	}

	private static bool isWithinNameOf(IOperation op) {
		for (IOperation? curr = op.Parent; curr is not null; curr = curr.Parent)
			if (curr.Kind == OperationKind.NameOf)
				return true;
		return false;
	}

	private static bool isWithinGlobalSpriteBatch(SyntaxNodeAnalysisContext ctx, KnownSymbols known) {
		if (known.GlobalSpriteBatch is null)
			return false;
		ISymbol? enclosing = ctx.SemanticModel.GetEnclosingSymbol(ctx.Node.SpanStart, ctx.CancellationToken);
		return isWithinGlobalSpriteBatch(enclosing, known.GlobalSpriteBatch);
	}

	private static bool isWithinGlobalSpriteBatch(ISymbol? sym, INamedTypeSymbol globalSpriteBatchType) {
		for (ISymbol? curr = sym; curr is not null; curr = curr.ContainingSymbol)
			if (curr is INamedTypeSymbol type && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, globalSpriteBatchType))
				return true;
		return false;
	}

	private static bool containsSpriteBatch([NotNullWhen(true)] ITypeSymbol? type, INamedTypeSymbol spriteBatchType) {
		if (type is null)
			return false;
		if (type.IsOrDerivesFrom(spriteBatchType))
			return true;
		switch (type) {
		case IArrayTypeSymbol arr:
			return containsSpriteBatch(arr.ElementType, spriteBatchType);
		case IPointerTypeSymbol ptr:
			return containsSpriteBatch(ptr.PointedAtType, spriteBatchType);
		case IFunctionPointerTypeSymbol funcptr:
			if (containsSpriteBatch(funcptr.Signature.ReturnType, spriteBatchType))
				return true;
			foreach (IParameterSymbol param in funcptr.Signature.Parameters)
				if (containsSpriteBatch(param.Type, spriteBatchType))
					return true;
			return false;
		case INamedTypeSymbol named:
			foreach (ITypeSymbol typeArgument in named.TypeArguments)
				if (containsSpriteBatch(typeArgument, spriteBatchType))
					return true;
			return false;
		default:
			return false;
		}
	}

	private static ITypeSymbol? getDeclaredValueType(ISymbol? sym) => sym switch {
		IFieldSymbol field => field.Type,
		ILocalSymbol local => local.Type,
		IEventSymbol @event => @event.Type,
		_ => null,
	};
}
