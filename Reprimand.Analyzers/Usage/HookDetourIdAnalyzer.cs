// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Reprimand.Analyzers.Usage;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HookDetourIdAnalyzer : DiagnosticAnalyzer {
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
		Diagnostics.Usage.HookWithoutDetourId
	);

	public override void Initialize(AnalysisContext context) {
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static ctx => {
				KnownSymbols known = new(ctx.Compilation);
				ctx.RegisterOperationAction(c => analyzeObjectCreation(c, known), OperationKind.ObjectCreation);
				ctx.RegisterOperationAction(c => analyzeEventAssignment(c, known), OperationKind.EventAssignment);
			}
		);
	}

	private static void analyzeObjectCreation(OperationAnalysisContext ctx, KnownSymbols known) {
		var creat = (IObjectCreationOperation)ctx.Operation;
		if (!isExactType(creat.Type, known.Hook) && !isExactType(creat.Type, known.ILHook))
			return;
		if (
			hasExplicitNonNullConfigArg(creat, known) ||
			isOnLoadLifecycleMethod(ctx.ContainingSymbol, known) ||
			isInsideDetourConfigScope(creat, known)
		)
			return;
		ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.HookWithoutDetourId, creat.Syntax.GetLocation()));
	}

	private static void analyzeEventAssignment(OperationAnalysisContext ctx, KnownSymbols known) {
		var asg = (IEventAssignmentOperation)ctx.Operation;
		if (!asg.Adds)
			return;
		var @ref = (IEventReferenceOperation)asg.EventReference;
		IEventSymbol sym = @ref.Event;
		INamespaceSymbol ns = sym.ContainingNamespace;
		for (; ns.ContainingNamespace is { IsGlobalNamespace: false } parent; ns = parent) ;
		if (ns.Name is not "On" and not "IL")
			return;
		if (isOnLoadLifecycleMethod(ctx.ContainingSymbol, known) || isInsideDetourConfigScope(asg, known))
			return;
		ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.HookWithoutDetourId, asg.Syntax.GetLocation()));
	}

	private static bool isOnLoadLifecycleMethod(ISymbol containingSymbol, KnownSymbols known) {
		if (containingSymbol is not IMethodSymbol method)
			return false;
		foreach (AttributeData attr in method.GetAttributes())
			if (attr.AttributeClass.Implements(known.IOnLoadLifecycleAttribute))
				return true;
		return false;
	}

	private static bool hasExplicitNonNullConfigArg(IObjectCreationOperation creat, KnownSymbols known) {
		if (known.DetourConfig is null)
			return false;
		foreach (IArgumentOperation arg in creat.Arguments) {
			if (arg.IsImplicit || arg.Parameter is null || !SymbolEqualityComparer.Default.Equals(arg.Parameter.Type, known.DetourConfig))
				continue;
			if (arg.Value.ConstantValue.HasValue && arg.Value.ConstantValue.Value is null)
				continue;
			return true;
		}
		return false;
	}

	private static bool isInsideDetourConfigScope(IOperation op, KnownSymbols known) {
		IOperation executableRoot = getExecutableRoot(op);
		IOperation curr = op;
		while (curr.Parent is IOperation parent) {
			// lambdas or local functions can be deferred, don't count them
			if (parent is IAnonymousFunctionOperation or ILocalFunctionOperation)
				break;

			if (
				parent is IUsingOperation @using && ReferenceEquals(@using.Body, curr) &&
				usingActivatesDetourConfig(@using.Resources, executableRoot, known)
			)
				return true;

			if (parent is IBlockOperation block)
				foreach (IOperation stmt in block.Operations) {
					if (ReferenceEquals(stmt, curr))
						break;
					if (
						stmt is IUsingDeclarationOperation usingDecl &&
						usingActivatesDetourConfig(usingDecl.DeclarationGroup, executableRoot, known)
					)
						return true;
				}

			curr = parent;
		}
		return false;
	}

	private static bool usingActivatesDetourConfig(IOperation op, IOperation executableRoot, KnownSymbols known) {
		HashSet<ISymbol> resolvingLocals = new(SymbolEqualityComparer.Default);
		return containsConfiguredContextUse(op, executableRoot, known, resolvingLocals);
	}

	private static bool containsConfiguredContextUse(IOperation op, IOperation executableRoot, KnownSymbols known, HashSet<ISymbol> resolvingLocals) {
		if (op is IAnonymousFunctionOperation or ILocalFunctionOperation)
			return false;

		if (
			op is IInvocationOperation inv && isDetourContextUse(inv, known) && inv.Instance is not null &&
			resolvesToConfiguredContext(inv.Instance, executableRoot, known, resolvingLocals)
		)
			return true;

		if (op is IVariableInitializerOperation initer && resolvesToConfiguredContext(initer.Value, executableRoot, known, resolvingLocals))
			return true;

		if (op is ILocalReferenceOperation localRef && resolvingLocals.Add(localRef.Local)) {
			IOperation? assigned = findLatestAssignedValue(localRef.Local, executableRoot, localRef.Syntax.SpanStart);
			bool result = assigned is not null && containsConfiguredContextUse(assigned, executableRoot, known, resolvingLocals);
			resolvingLocals.Remove(localRef.Local);
			if (result)
				return true;
		}

		foreach (IOperation child in op.ChildOperations)
			if (containsConfiguredContextUse(child, executableRoot, known, resolvingLocals))
				return true;

		return false;
	}

	private static bool resolvesToConfiguredContext(IOperation op, IOperation executableRoot, KnownSymbols known, HashSet<ISymbol> resolvingLocals) {
		switch (op) {
		case IObjectCreationOperation creat:
			return isExactType(creat.Type, known.DetourConfigContext) && hasExplicitNonNullConfigArg(creat, known);
		case IConversionOperation conv:
			return resolvesToConfiguredContext(conv.Operand, executableRoot, known, resolvingLocals);
		case IParenthesizedOperation paren:
			return resolvesToConfiguredContext(paren.Operand, executableRoot, known, resolvingLocals);
		case ILocalReferenceOperation localRef:
			if (!resolvingLocals.Add(localRef.Local))
				return false;
			IOperation? assigned = findLatestAssignedValue(localRef.Local, executableRoot, localRef.Syntax.SpanStart);
			bool result = assigned is not null && resolvesToConfiguredContext(assigned, executableRoot, known, resolvingLocals);
			resolvingLocals.Remove(localRef.Local);
			return result;
		default:
			// fallback, don't follow fields, properties, parameters, methods, etc.
			return false;
		}
	}

	private static bool isDetourContextUse(IInvocationOperation inv, KnownSymbols known) =>
		SymbolEqualityComparer.Default.Equals(inv.TargetMethod.OriginalDefinition, known.DetourContextParamlessUseMethod);

	private static IOperation? findLatestAssignedValue(ILocalSymbol local, IOperation executableRoot, int beforePos) {
		IOperation? bestVal = null;
		int bestPos = -1;
		Stack<IOperation> pending = new();
		pending.Push(executableRoot);
		while (pending.Count != 0) {
			IOperation candidate = pending.Pop();
			if (!ReferenceEquals(candidate, executableRoot) && candidate is IAnonymousFunctionOperation or ILocalFunctionOperation)
				continue;
			if (!ReferenceEquals(candidate, executableRoot) && candidate.Syntax.SpanStart >= beforePos)
				continue;
			if (
				candidate is IVariableDeclaratorOperation declarator &&
				SymbolEqualityComparer.Default.Equals(declarator.Symbol, local) &&
				declarator.Initializer is not null
			) {
				int pos = declarator.Syntax.SpanStart;
				if (pos > bestPos) {
					bestPos = pos;
					bestVal = declarator.Initializer.Value;
				}
			} else if (
				candidate is ISimpleAssignmentOperation asg &&
				asg.Target is ILocalReferenceOperation target &&
				SymbolEqualityComparer.Default.Equals(target.Local, local)
			) {
				int pos = asg.Syntax.SpanStart;
				if (pos > bestPos) {
					bestPos = pos;
					bestVal = asg.Value;
				}
			}
			foreach (IOperation child in candidate.ChildOperations)
				pending.Push(child);
		}

		return bestVal;
	}

	private static IOperation getExecutableRoot(IOperation op) {
		IOperation curr = op;
		while (curr.Parent is IOperation parent && parent is not IAnonymousFunctionOperation and not ILocalFunctionOperation)
			curr = parent;
		return curr;
	}

	private static bool isExactType([NotNullWhen(true)] ITypeSymbol? actual, [NotNullWhen(true)] INamedTypeSymbol? expected) =>
		expected is not null && actual is INamedTypeSymbol named && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, expected.OriginalDefinition);
}
