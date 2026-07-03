// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Reprimand.Analyzers.Usage;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TrackedAsAnalyzer : DiagnosticAnalyzer {
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
		Diagnostics.Usage.TrackedAsIsClassOnly,
		Diagnostics.Usage.InvalidTrackedAsType,
		Diagnostics.Usage.UnrelatedTrackedAsType,
		Diagnostics.Usage.TrackedAsDerivedFrom,
		Diagnostics.Usage.TrackedAsFieldWrite
	);

	public override void Initialize(AnalysisContext context) {
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static ctx => {
				KnownSymbols known = new(ctx.Compilation);
				if (known.TrackedAsAttribute is null)
					return;
				ctx.RegisterSyntaxNodeAction(c => analyzeAttribute(c, known), SyntaxKind.Attribute);
				ctx.RegisterSymbolAction(c => analyzeNamedType(c, known), SymbolKind.NamedType);
				if (known.TrackedAsAttributeFields.Count > 0)
					ctx.RegisterOperationAction(c => analyzeFieldReference(c, known), OperationKind.FieldReference);
			}
		);
	}

	private static void analyzeAttribute(SyntaxNodeAnalysisContext ctx, KnownSymbols known) {
		var sx = (AttributeSyntax)ctx.Node;

		if (ctx.SemanticModel.GetOperation(sx, ctx.CancellationToken) is not IAttributeOperation op)
			return;
		if (op.Operation is not IObjectCreationOperation creat)
			return;
		if (creat.Constructor is null || !SymbolEqualityComparer.Default.Equals(creat.Constructor.ContainingType, known.TrackedAsAttribute))
			return;

		if (!tryGetAttributedClass(sx, ctx.SemanticModel, ctx.CancellationToken, out INamedTypeSymbol? attributedType)) {
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.TrackedAsIsClassOnly, sx.GetLocation()));
			return;
		}

		IArgumentOperation? typeArgument = creat.Arguments.FirstOrDefault(static arg => arg.Parameter?.Ordinal == 0);
		// missing/unbound ctor args already have diagnostics from roslyn
		if (typeArgument is null)
			return;

		ITypeSymbol? trackedType = getTypeofOperand(typeArgument.Value);
		if (
			trackedType is not INamedTypeSymbol trackedNamedType ||
			trackedNamedType.TypeKind != TypeKind.Class ||
			!trackedNamedType.IsReferenceType ||
			trackedNamedType.IsStatic
		) {
			ctx.ReportDiagnostic(
				Diagnostic.Create(
					Diagnostics.Usage.InvalidTrackedAsType,
					typeArgument.Syntax.GetLocation(),
					trackedType?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) ?? "<unknown>"
				)
			);
			return;
		}

		if (!attributedType.IsOrDerivesFrom(trackedNamedType))
			ctx.ReportDiagnostic(
				Diagnostic.Create(
					Diagnostics.Usage.UnrelatedTrackedAsType,
					typeArgument.Syntax.GetLocation(),
					attributedType?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) ?? "<unknown>",
					trackedNamedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
				)
			);
	}

	private static void analyzeNamedType(SymbolAnalysisContext ctx, KnownSymbols known) {
		var type = (INamedTypeSymbol)ctx.Symbol;
		if (SymbolEqualityComparer.Default.Equals(type, known.TrackedAsAttribute))
			return;
		if (type.IsOrDerivesFrom(known.TrackedAsAttribute)) {
			Location? loc = type.Locations.FirstOrDefault(static l => l.IsInSource);
			ctx.ReportDiagnostic(
				Diagnostic.Create(
					Diagnostics.Usage.TrackedAsDerivedFrom,
					loc,
					type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
				)
			);
		}
	}

	private static void analyzeFieldReference(OperationAnalysisContext ctx, KnownSymbols known) {
		var fr = (IFieldReferenceOperation)ctx.Operation;
		if (!known.TrackedAsAttributeFields.Contains(fr.Field.OriginalDefinition))
			return;

		// don't report on assignments on the original ctor(s)
		if (
			ctx.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor } method &&
			SymbolEqualityComparer.Default.Equals(method.ContainingType, known.TrackedAsAttribute)
		)
			return;

		if (!isWrite(fr))
			return;

		ctx.ReportDiagnostic(
			Diagnostic.Create(
				Diagnostics.Usage.TrackedAsFieldWrite,
				fr.Syntax.GetLocation(),
				fr.Field.Name
			)
		);
	}

	private static bool tryGetAttributedClass(
		AttributeSyntax attr,
		SemanticModel model,
		System.Threading.CancellationToken ct,
		out INamedTypeSymbol? attributedType)
	{
		attributedType = null;
		if (attr.Parent is not AttributeListSyntax { Target: null } attributeList)
			return false;
		SyntaxNode? decl = attributeList.Parent;
		if (decl is ClassDeclarationSyntax) {
			attributedType = model.GetDeclaredSymbol(decl, ct) as INamedTypeSymbol;
			return attributedType is not null;
		}
		if (decl is RecordDeclarationSyntax record && !record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)) {
			attributedType = model.GetDeclaredSymbol(record, ct);
			return attributedType is not null;
		}
		return false;
	}

	private static ITypeSymbol? getTypeofOperand(IOperation op) {
		while (op is IConversionOperation conv)
			op = conv.Operand;
		return op is ITypeOfOperation @typeof ? @typeof.TypeOperand : null;
	}

	private static bool isWrite(IFieldReferenceOperation fr) {
		IOperation curr = fr;

		// go only through wrappers which can surround an lvalue
		while (curr.Parent is IParenthesizedOperation or IConversionOperation or ITupleOperation)
			curr = curr.Parent;

		switch (curr.Parent) {
		case IAssignmentOperation asg when ReferenceEquals(asg.Target, curr):
			return true;
		case IIncrementOrDecrementOperation incr when ReferenceEquals(incr.Target, curr):
			return true;
		case IArgumentOperation arg when ReferenceEquals(arg.Value, curr) && arg.Parameter?.RefKind is RefKind.Ref or RefKind.Out:
			return true;
		}

		// reject ref aliases and pointer aliases since you can write through them later
		SyntaxNode sx = fr.Syntax;
		while (sx.Parent is ParenthesizedExpressionSyntax)
			sx = sx.Parent;
		return sx.Parent is RefExpressionSyntax || sx.Parent is PrefixUnaryExpressionSyntax prefix && prefix.IsKind(SyntaxKind.AddressOfExpression);
	}
}
