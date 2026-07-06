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
public sealed class HookMethodAnalyzer : DiagnosticAnalyzer {
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
		Diagnostics.Usage.NonNullableHookArgument,
		Diagnostics.Usage.OrigNotCalled,
		Diagnostics.Usage.YieldReturnOrig
	);

	public override void Initialize(AnalysisContext context) {
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static ctx => {
				KnownSymbols known = new(ctx.Compilation);
				if (known.HookMethodAttribute is null)
					return;
				ctx.RegisterSyntaxNodeAction(c => analyzeMethodDeclaration(c, known), SyntaxKind.MethodDeclaration);
				ctx.RegisterOperationBlockAction(c => analyzeOperationBlock(c, known));
				ctx.RegisterOperationAction(c => analyzeYieldReturn(c, known), OperationKind.YieldReturn);
			}
		);
	}

	private static void analyzeMethodDeclaration(SyntaxNodeAnalysisContext ctx, KnownSymbols known) {
		var sx = (MethodDeclarationSyntax)ctx.Node;
		IMethodSymbol? method = ctx.SemanticModel.GetDeclaredSymbol(sx, ctx.CancellationToken);
		if (method is null || !method.OriginalDefinition.GetAttributes().Any(a => a.AttributeClass.IsOrDerivesFrom(known.HookMethodAttribute)))
			return;
		if (method.Parameters.Length == 1 && method.Parameters[0].Type.IsOrDerivesFrom(known.ILContext))
			return;
		foreach (ParameterSyntax paramSx in sx.ParameterList.Parameters) {
			if (paramSx.Type is null)
				continue;
			IParameterSymbol? param = ctx.SemanticModel.GetDeclaredSymbol(paramSx, ctx.CancellationToken);
			if (
				param is null ||
				(param.Type.TypeKind == TypeKind.Delegate && param.Ordinal == 0) ||
				(param.Name == "self" && param.Ordinal == 1)
			)
				continue;
			NullableContext nullableCtx = ctx.SemanticModel.GetNullableContext(paramSx.Type.SpanStart);
			if ((nullableCtx & NullableContext.AnnotationsEnabled) == 0)
				continue;
			if (!param.Type.IsReferenceType || param.NullableAnnotation != NullableAnnotation.NotAnnotated)
				continue;
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.NonNullableHookArgument, paramSx.Identifier.GetLocation(), param.Name));
		}
	}

	private static void analyzeOperationBlock(OperationBlockAnalysisContext ctx, KnownSymbols known) {
		if (ctx.OwningSymbol is not IMethodSymbol method || method.Parameters.Length == 0)
			return;
		if (!method.GetAttributes().Any(a => a.AttributeClass is not null && a.AttributeClass.OriginalDefinition.IsOrDerivesFrom(known.HookMethodAttribute)))
			return;
		IParameterSymbol orig = method.Parameters[0];
		if (orig.Type.TypeKind != TypeKind.Delegate)
			return;

		Stack<IOperation> pending = new();
		foreach (IOperation operationBlock in ctx.OperationBlocks)
			pending.Push(operationBlock);

		while (pending.Count > 0) {
			ctx.CancellationToken.ThrowIfCancellationRequested();
			IOperation op = pending.Pop();
			if (op is IAnonymousFunctionOperation or ILocalFunctionOperation)
				continue;
			if (isOrig(op, orig))
				return;
			foreach (IOperation child in op.ChildOperations)
				pending.Push(child);
		}

		Location? loc = orig.Locations.FirstOrDefault() ?? method.Locations.FirstOrDefault();
		ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.OrigNotCalled, loc, method.Name));
	}

	private static void analyzeYieldReturn(OperationAnalysisContext ctx, KnownSymbols known) {
		// IsIterator was introduced in Microsoft.CodeAnalysis v5, we're targeting v4
		if (ctx.ContainingSymbol is not IMethodSymbol method /* || !method.IsIterator */)
			return;
		if (!method.GetAttributes().Any(a => a.AttributeClass is not null && a.AttributeClass.OriginalDefinition.IsOrDerivesFrom(known.HookMethodAttribute)))
			return;
		IParameterSymbol orig = method.Parameters[0];
		if (orig.Type.TypeKind != TypeKind.Delegate)
			return;

		var yieldReturn = (IReturnOperation)ctx.Operation;
		IOperation? yielded = yieldReturn.ReturnedValue;
		while (yielded is IConversionOperation conv)
			yielded = conv.Operand;
		if (yielded is null)
			return;
		if (yielded is IAnonymousFunctionOperation or ILocalFunctionOperation || !isOrig(yielded, orig))
			return;
		ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.YieldReturnOrig, yieldReturn.Syntax.GetLocation()));
	}

	private static bool isOrig(IOperation op, IParameterSymbol orig) {
		if (op is IInvocationOperation inv && inv.TargetMethod.MethodKind == MethodKind.DelegateInvoke) {
			IOperation? inst = inv.Instance;
			if (inst is IConditionalAccessInstanceOperation) {
				for (IOperation? parent = inv.Parent; parent is not null; parent = parent.Parent) {
					if (parent is IConditionalAccessOperation cond) {
						inst = cond.Operation;
						break;
					}
				}
			}
			for (;;) {
				switch (inst) {
				case IConversionOperation conv:
					inst = conv.Operand;
					continue;
				case IParenthesizedOperation paren:
					inst = paren.Operand;
					continue;
				}
				break;
			}
			if (inst is IParameterReferenceOperation paramRef && SymbolEqualityComparer.Default.Equals(paramRef.Parameter, orig))
				return true;
		}
		return false;
	}
}
