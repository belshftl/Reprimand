// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Reprimand.Analyzers.Usage;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StaticCtorAnalyzer : DiagnosticAnalyzer {
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
		Diagnostics.Usage.IllegalStaticCtorFieldOrPropertyAccess,
		Diagnostics.Usage.IllegalStaticCtorMethodCall
	);

	public override void Initialize(AnalysisContext context) {
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static ctx => {
				KnownSymbols known = new(ctx.Compilation);
				ctx.RegisterOperationAction(c => analyzeFieldReference(c, known), OperationKind.FieldReference);
				ctx.RegisterOperationAction(c => analyzePropertyReference(c, known), OperationKind.PropertyReference);
				ctx.RegisterOperationAction(c => analyzeInvocation(c, known), OperationKind.Invocation);
			}
		);
	}

	private static void analyzeFieldReference(OperationAnalysisContext ctx, KnownSymbols known) {
		var fr = (IFieldReferenceOperation)ctx.Operation;
		if (fr.IsDeclaration ||
			!(
				fr.Field.OriginalDefinition.GetAttributes().Any(a => a.AttributeClass.IsOrDerivesFrom(known.DontUseInStaticCtorAttribute)) ||
				known.NonStaticInitedDrawFields.Contains(fr.Field.OriginalDefinition) ||
				known.NonStaticInitedGfxFields.Contains(fr.Field.OriginalDefinition)
			)
		)
			return;

		bool report = ctx.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.StaticConstructor };
		if (!report) {
			for (IOperation? ancestor = fr.Parent; ancestor is not null; ancestor = ancestor.Parent) {
				if (ancestor is IAnonymousFunctionOperation or ILocalFunctionOperation or INameOfOperation)
					return;
				if (ancestor is IFieldInitializerOperation fieldInitializer) {
					report = fieldInitializer.InitializedFields.Any(static f => f.IsStatic);
					break;
				}
				if (ancestor is IPropertyInitializerOperation propertyInitializer) {
					report = propertyInitializer.InitializedProperties.Any(static p => p.IsStatic);
					break;
				}
			}
		}
		if (!report)
			return;

		Location? loc = fr.Syntax switch {
			MemberAccessExpressionSyntax access => access.Name.GetLocation(),
			MemberBindingExpressionSyntax binding => binding.Name.GetLocation(),
			IdentifierNameSyntax ident => ident.GetLocation(),
			_ => fr.Syntax.GetLocation(),
		};
		ctx.ReportDiagnostic(
			Diagnostic.Create(
				Diagnostics.Usage.IllegalStaticCtorFieldOrPropertyAccess,
				loc,
				fr.Field.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)
			)
		);
	}

	private static void analyzePropertyReference(OperationAnalysisContext ctx, KnownSymbols known) {
		var pr = (IPropertyReferenceOperation)ctx.Operation;
		if (
			!(
				pr.Property.OriginalDefinition.GetAttributes().Any(a => a.AttributeClass.IsOrDerivesFrom(known.DontUseInStaticCtorAttribute)) ||
				known.NonStaticInitedEngineProperties.Contains(pr.Property.OriginalDefinition) ||
				known.NonStaticInitedDrawProperties.Contains(pr.Property.OriginalDefinition)
			)
		)
			return;

		bool report = ctx.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.StaticConstructor };
		if (!report) {
			for (IOperation? ancestor = pr.Parent; ancestor is not null; ancestor = ancestor.Parent) {
				if (ancestor is IAnonymousFunctionOperation or ILocalFunctionOperation or INameOfOperation)
					return;
				if (ancestor is IFieldInitializerOperation fieldInitializer) {
					report = fieldInitializer.InitializedFields.Any(static f => f.IsStatic);
					break;
				}
				if (ancestor is IPropertyInitializerOperation propertyInitializer) {
					report = propertyInitializer.InitializedProperties.Any(static p => p.IsStatic);
					break;
				}
			}
		}
		if (!report)
			return;

		Location? loc = pr.Syntax switch {
			MemberAccessExpressionSyntax access => access.Name.GetLocation(),
			MemberBindingExpressionSyntax binding => binding.Name.GetLocation(),
			IdentifierNameSyntax ident => ident.GetLocation(),
			_ => pr.Syntax.GetLocation(),
		};
		ctx.ReportDiagnostic(
			Diagnostic.Create(
				Diagnostics.Usage.IllegalStaticCtorFieldOrPropertyAccess,
				loc,
				pr.Property.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)
			)
		);
	}

	private static void analyzeInvocation(OperationAnalysisContext ctx, KnownSymbols known) {
		var inv = (IInvocationOperation)ctx.Operation;
		IMethodSymbol method = inv.TargetMethod.ReducedFrom ?? inv.TargetMethod;
		if (
			!(
				method.OriginalDefinition.GetAttributes().Any(a => a.AttributeClass.IsOrDerivesFrom(known.DontUseInStaticCtorAttribute)) ||
				known.NonStaticInitedVirtualContentMethods.Contains(method.OriginalDefinition)
			)
		)
			return;

		bool report = ctx.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.StaticConstructor };
		if (!report) {
			for (IOperation? ancestor = inv.Parent; ancestor is not null; ancestor = ancestor.Parent) {
				if (ancestor is IAnonymousFunctionOperation or ILocalFunctionOperation or INameOfOperation)
					return;
				if (ancestor is IFieldInitializerOperation fieldInitializer) {
					report = fieldInitializer.InitializedFields.Any(static f => f.IsStatic);
					break;
				}
				if (ancestor is IPropertyInitializerOperation propertyInitializer) {
					report = propertyInitializer.InitializedProperties.Any(static p => p.IsStatic);
					break;
				}
			}
		}
		if (!report)
			return;

		Location? loc = inv.Syntax switch {
			InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } => access.Name.GetLocation(),
			InvocationExpressionSyntax { Expression: MemberBindingExpressionSyntax binding } => binding.Name.GetLocation(),
			InvocationExpressionSyntax { Expression: IdentifierNameSyntax ident } => ident.GetLocation(),
			_ => inv.Syntax.GetLocation(),
		};
		ctx.ReportDiagnostic(
			Diagnostic.Create(
				Diagnostics.Usage.IllegalStaticCtorMethodCall,
				loc,
				method.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)
			)
		);
	}
}
