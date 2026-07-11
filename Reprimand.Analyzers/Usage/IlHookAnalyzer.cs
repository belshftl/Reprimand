// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Reprimand.Analyzers.Usage;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IlHookAnalyzer : DiagnosticAnalyzer {
	private enum DelegateTargetKind {
		PlainStaticMethodGroup,
		StaticLambda,
		NonStaticOrUnknown,
	}

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
		Diagnostics.Usage.NonStaticEmitDelegateMethod,
		Diagnostics.Usage.EmitDelegateStaticLambda,
		Diagnostics.Usage.DestructiveILEdit,
		Diagnostics.Usage.PreferRequireGoto
	);

	public override void Initialize(AnalysisContext context) {
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static ctx => {
				KnownSymbols known = new(ctx.Compilation);
				ctx.RegisterOperationAction(c => analyzeInvocation(c, known), OperationKind.Invocation);
				ctx.RegisterOperationAction(
					c => analyzeAssignment(c, known),
					OperationKind.SimpleAssignment,
					OperationKind.CompoundAssignment,
					OperationKind.Increment,
					OperationKind.Decrement
				);
			}
		);
	}

	private static void analyzeInvocation(OperationAnalysisContext ctx, KnownSymbols known) {
		var inv = (IInvocationOperation)ctx.Operation;
		if (known.EmitDelegateMethods.Contains(inv.TargetMethod.OriginalDefinition)) {
			IArgumentOperation? delegateArg = findDelegateArgument(inv.Arguments);
			if (delegateArg is not null)
				maybeReportDelegateArg(ctx, delegateArg);
		} else if (known.RemoveInstructionMethods.Contains(inv.TargetMethod.OriginalDefinition)) {
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.DestructiveILEdit, inv.Syntax.GetLocation()));
		} else if (known.GotoMethods.Contains(inv.TargetMethod.OriginalDefinition)) {
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.PreferRequireGoto, inv.Syntax.GetLocation()));
		} else {
			ITypeSymbol? receiverType = inv.Instance?.Type;
			if (receiverType is null || !isInstructionListLike(receiverType, known))
				return;
			// TODO move out these constants
			if (inv.TargetMethod.Name is "Add" or "AddRange" or "Clear" or "Insert" or "InsertRange" or "Remove" or "RemoveAt" or "RemoveRange")
				ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.DestructiveILEdit, inv.Syntax.GetLocation()));
		}
	}

	private static void analyzeAssignment(OperationAnalysisContext ctx, KnownSymbols known) {
		IOperation? left = ctx.Operation switch {
			ISimpleAssignmentOperation op => op.Target,
			ICompoundAssignmentOperation op => op.Target,
			IIncrementOrDecrementOperation op => op.Target,
			_ => null,
		};
		if (left is null)
			return;

		if (left is IPropertyReferenceOperation propRef) {
			IPropertySymbol prop = propRef.Property.OriginalDefinition;
			if (known.InstructionMembers.Contains(prop) ||
				propRef.Property.IsIndexer && propRef.Instance?.Type is {} receiverType && isInstructionListLike(receiverType, known)) {
				ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.DestructiveILEdit, left.Syntax.GetLocation()));
				return;
			}
		}

		if (left is IFieldReferenceOperation fieldRef && known.InstructionMembers.Contains(fieldRef.Field.OriginalDefinition))
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.DestructiveILEdit, left.Syntax.GetLocation()));
	}

	private static bool isInstructionListLike(ITypeSymbol type, KnownSymbols known) {
		if (known.Instruction is null)
			return false;
		if (isGenericInstructionCollection(type, known.Instruction))
			return true;
		foreach (INamedTypeSymbol iface in type.AllInterfaces)
			if (isGenericInstructionCollection(iface, known.Instruction))
				return true;
		return false;
	}

	private static bool isGenericInstructionCollection(ITypeSymbol type, INamedTypeSymbol instructionType) {
		if (type is not INamedTypeSymbol named || named.TypeArguments.Length != 1)
			return false;
		if (!SymbolEqualityComparer.Default.Equals(named.TypeArguments[0], instructionType))
			return false;
		// TODO move out these constants
		return named.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) is
			"global::System.Collections.Generic.ICollection<T>" or
			"global::System.Collections.Generic.IList<T>" or
			"global::System.Collections.Generic.List<T>" or
			"global::Mono.Collections.Generic.Collection<T>";
	}

	private static IArgumentOperation? findDelegateArgument(ImmutableArray<IArgumentOperation> args) {
		foreach (IArgumentOperation arg in args)
			if (arg.Value.Type?.TypeKind == TypeKind.Delegate)
				return arg;
		return null;
	}

	private static void maybeReportDelegateArg(OperationAnalysisContext ctx, IArgumentOperation arg) =>
		maybeReportDelegateValue(ctx, arg.Value);

	private static void maybeReportDelegateValue(OperationAnalysisContext ctx, IOperation op) {
		(_, DelegateTargetKind kind) = classifyDelegateExpression(op);
		switch (kind) {
		case DelegateTargetKind.PlainStaticMethodGroup:
			return;
		case DelegateTargetKind.StaticLambda:
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.EmitDelegateStaticLambda, op.Syntax.GetLocation()));
			return;
		case DelegateTargetKind.NonStaticOrUnknown:
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.NonStaticEmitDelegateMethod, op.Syntax.GetLocation()));
			return;
		}
	}

	private static (IMethodSymbol? m, DelegateTargetKind kind) classifyDelegateExpression(IOperation op) {
		while (op is IConversionOperation conv && conv.IsImplicit)
			op = conv.Operand;
		if (op is IDelegateCreationOperation del)
			return classifyDelegateExpression(del.Target);
		if (op is IMethodReferenceOperation methodRef)
			return (methodRef.Method, methodRef.Method.IsStatic ? DelegateTargetKind.PlainStaticMethodGroup : DelegateTargetKind.NonStaticOrUnknown);
		if (op is IAnonymousFunctionOperation anon)
			return (anon.Symbol, anon.Symbol.IsStatic ? DelegateTargetKind.StaticLambda : DelegateTargetKind.NonStaticOrUnknown);
		return (null, DelegateTargetKind.NonStaticOrUnknown);
	}
}
