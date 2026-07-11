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
public sealed class HookAnalyzer : DiagnosticAnalyzer {
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
		Diagnostics.Usage.NonStaticHookMethod,
		Diagnostics.Usage.GenericHookMethod,
		Diagnostics.Usage.NonNullableHookParameter,
		Diagnostics.Usage.OrigNotCalled,
		Diagnostics.Usage.YieldReturnOrig
	);

	[Flags]
	private enum HookKind {
		None = 0,
		Plain = 1,
		Il = 2,
	}

	private readonly struct Callable(IMethodSymbol method, SyntaxNode? declaration) {
		public IMethodSymbol Method { get; } = method;
		public SyntaxNode? Declaration { get; } = declaration;
	}

	private sealed class RegisteredHook(IMethodSymbol method, SyntaxNode declaration, HookKind kind) {
		public IMethodSymbol Method { get; } = method;
		public SyntaxNode Declaration { get; } = declaration;
		public HookKind Kind { get; set; } = kind;
	}

	public override void Initialize(AnalysisContext context) {
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static ctx => {
				KnownSymbols known = new(ctx.Compilation);
				ctx.RegisterSemanticModelAction(c => analyzeSemanticModel(c, known));
			}
		);
	}

	private static void analyzeSemanticModel(SemanticModelAnalysisContext ctx, KnownSymbols known) {
		SemanticModel model = ctx.SemanticModel;
		CancellationToken ct = ctx.CancellationToken;
		SyntaxNode root = model.SyntaxTree.GetRoot(ct);
		Dictionary<IMethodSymbol, RegisteredHook> hooks = new(SymbolEqualityComparer.Default);
		foreach (AssignmentExpressionSyntax? asg in root.DescendantNodes().OfType<AssignmentExpressionSyntax>()) {
			ct.ThrowIfCancellationRequested();
			if (!asg.IsKind(SyntaxKind.AddAssignmentExpression))
				continue;
			if (model.GetSymbolInfo(asg.Left, ct).Symbol is not IEventSymbol ev || !tryClassifyHookGenEvent(ev, out HookKind kind))
				continue;
			if (tryGetCallable(model.GetOperation(asg.Right, ct)) is Callable c)
				addHook(hooks, model.SyntaxTree, c, kind, ct);
		}

		foreach (BaseObjectCreationExpressionSyntax? creatSx in root.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>()) {
			ct.ThrowIfCancellationRequested();
			if (
				model.GetOperation(creatSx, ct) is not IObjectCreationOperation creat ||
				creat.Constructor is null ||
				!tryClassifyDetourType(creat.Constructor.ContainingType, known, out HookKind kind)
			)
				continue;

			IArgumentOperation? hookArg = creat.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == 1);
			if (hookArg is null)
				continue;

			if (tryGetCallable(hookArg.Value) is Callable c)
				addHook(hooks, model.SyntaxTree, c, kind, ct);
		}
		foreach (RegisteredHook? h in hooks.Values) {
			ct.ThrowIfCancellationRequested();
			analyzeHook(ctx, h, known);
		}
	}

	private static void analyzeHook(
		SemanticModelAnalysisContext ctx,
		RegisteredHook hook,
		KnownSymbols known
	) {
		SemanticModel model = ctx.SemanticModel;
		CancellationToken ct = ctx.CancellationToken;
		IMethodSymbol method = hook.Method;
		SyntaxNode decl = hook.Declaration;
		string displayName = decl is AnonymousFunctionExpressionSyntax ? "<lambda expression>" : method.Name;
		Location declLoc = getDeclLocation(method, decl, model.SyntaxTree);

		if (!(decl is AnonymousFunctionExpressionSyntax anon ? anon.Modifiers.Any(SyntaxKind.StaticKeyword) : method.IsStatic))
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.NonStaticHookMethod, declLoc, displayName));
		if (method.IsGenericMethod)
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.GenericHookMethod, declLoc, displayName));

		if (hook.Kind != HookKind.Plain)
			return;

		IParameterSymbol? orig = method.Parameters.FirstOrDefault();
		IOperation? body = getBodyOperation(model, decl, ct);
		if (orig is null || body is null || !hasInvocationOf(body, orig, ct))
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.OrigNotCalled, declLoc, displayName));

		if (orig is not null && known.IEnumerator is not null && SymbolEqualityComparer.Default.Equals(method.ReturnType.OriginalDefinition, known.IEnumerator.OriginalDefinition))
			foreach (YieldStatementSyntax? yield in getDirectYieldStatements(decl)) {
				ct.ThrowIfCancellationRequested();
				if (yield.Expression is null)
					continue;
				IOperation? expr = model.GetOperation(yield.Expression, ct);
				if (!isDirectInvocationOf(expr, orig))
					continue;
				ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.YieldReturnOrig, yield.GetLocation()));
			}

		foreach (IParameterSymbol? param in method.Parameters) {
			ct.ThrowIfCancellationRequested();

			if (
				param.Ordinal == 0 || // orig
				param.Ordinal == 1 && param.Name == "self" ||
				!param.Type.IsReferenceType ||
				param.NullableAnnotation != NullableAnnotation.NotAnnotated
			)
				continue;

			Location? loc = getParamLocation(param, model.SyntaxTree, ct);
			if (loc is null)
				continue;

			NullableContext nullable = model.GetNullableContext(loc.SourceSpan.Start);
			if ((nullable & NullableContext.AnnotationsEnabled) == 0)
				continue;
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Usage.NonNullableHookParameter, loc, param.Name));
		}
	}

	private static bool tryClassifyHookGenEvent(IEventSymbol ev, out HookKind kind) {
		INamespaceSymbol ns = ev.ContainingNamespace;
		for (; ns.ContainingNamespace is { IsGlobalNamespace: false } parent; ns = parent) ;
		switch (ns.Name) {
		case "On":
			kind = HookKind.Plain;
			return true;
		case "IL":
			kind = HookKind.Il;
			return true;
		default:
			kind = HookKind.None;
			return false;
		}
	}

	private static bool tryClassifyDetourType(INamedTypeSymbol type, KnownSymbols known, out HookKind kind) {
		if (known.Hook is not null && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, known.Hook.OriginalDefinition)) {
			kind = HookKind.Plain;
			return true;
		}
		if (known.ILHook is not null && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, known.ILHook.OriginalDefinition)) {
			kind = HookKind.Il;
			return true;
		}
		kind = HookKind.None;
		return false;
	}

	private static Callable? tryGetCallable(IOperation? op) {
		while (op is not null) {
			switch (op) {
			case IConversionOperation conv:
				op = conv.Operand;
				continue;
			case IParenthesizedOperation paren:
				op = paren.Operand;
				continue;
			case IDelegateCreationOperation delegateCreat:
				op = delegateCreat.Target;
				continue;
			case IMethodReferenceOperation methodRef:
				return new Callable(methodRef.Method, declaration: null);
			case IAnonymousFunctionOperation anon:
				return new Callable(anon.Symbol, anon.Syntax);
			default:
				return null;
			}
		}
		return null;
	}

	private static void addHook(
		Dictionary<IMethodSymbol, RegisteredHook> hooks,
		SyntaxTree tree,
		Callable callable,
		HookKind kind,
		CancellationToken ct
	) {
		IMethodSymbol method = (callable.Method.PartialImplementationPart ?? callable.Method).OriginalDefinition;
		SyntaxNode? decl = callable.Declaration;
		if (decl is null || decl.SyntaxTree != tree)
			decl = findDecl(method, tree, ct);
		if (decl is null)
			return;
		if (hooks.TryGetValue(method, out RegisteredHook? existing)) {
			existing.Kind |= kind;
			return;
		}
		hooks.Add(method, new RegisteredHook(method, decl, kind));
	}

	private static SyntaxNode? findDecl(IMethodSymbol method, SyntaxTree tree, CancellationToken ct) {
		SyntaxNode? first = null;
		foreach (SyntaxReference? sxRef in method.DeclaringSyntaxReferences) {
			if (sxRef.SyntaxTree != tree)
				continue;
			SyntaxNode sx = sxRef.GetSyntax(ct);
			first ??= sx;
			if (sx switch {
				MethodDeclarationSyntax m => m.Body is not null || m.ExpressionBody is not null,
				LocalFunctionStatementSyntax l => l.Body is not null || l.ExpressionBody is not null,
				AnonymousFunctionExpressionSyntax => true,
				_ => false,
			})
				return sx;
		}
		return first;
	}

	private static IOperation? getBodyOperation(SemanticModel model, SyntaxNode decl, CancellationToken ct) {
		return decl switch {
			MethodDeclarationSyntax { Body: not null } method => model.GetOperation(method.Body, ct),
			MethodDeclarationSyntax { ExpressionBody: not null } method => model.GetOperation(method.ExpressionBody.Expression, ct),
			LocalFunctionStatementSyntax { Body: not null } localFn => model.GetOperation(localFn.Body, ct),
			LocalFunctionStatementSyntax { ExpressionBody: not null } localFn => model.GetOperation(localFn.ExpressionBody.Expression, ct),
			AnonymousFunctionExpressionSyntax anon => (model.GetOperation(anon, ct) as IAnonymousFunctionOperation)?.Body,
			_ => null,
		};
	}

	private static bool hasInvocationOf(IOperation root, IParameterSymbol param, CancellationToken ct) {
		Stack<IOperation> pending = new();
		pending.Push(root);
		while (pending.Count != 0) {
			ct.ThrowIfCancellationRequested();
			IOperation? op = pending.Pop();
			if (op is IInvocationOperation inv && isInvocationOf(inv, param))
				return true;
			foreach (IOperation child in op.ChildOperations)
				pending.Push(child);
		}
		return false;
	}

	private static bool isDirectInvocationOf(IOperation? op, IParameterSymbol param) {
		op = unwrap(op);
		if (op is IInvocationOperation inv)
			return isInvocationOf(inv, param);
		if (op is IConditionalAccessOperation condAccess) {
			IOperation? whenNotNull = unwrap(condAccess.WhenNotNull);
			return whenNotNull is IInvocationOperation conditionalInvocation &&
				isInvocationOf(conditionalInvocation, param);
		}
		return false;
	}

	private static bool isInvocationOf(IInvocationOperation inv, IParameterSymbol param) {
		if (inv.TargetMethod.MethodKind != MethodKind.DelegateInvoke)
			return false;

		IOperation? inst = unwrap(inv.Instance);
		if (inst is IParameterReferenceOperation paramRef)
			return SymbolEqualityComparer.Default.Equals(paramRef.Parameter, param);
		if (inst is not IConditionalAccessInstanceOperation)
			return false;

		for (IOperation? parent = inv.Parent; parent is not null; parent = parent.Parent) {
			if (parent is not IConditionalAccessOperation condAccess)
				continue;
			IOperation? receiver = unwrap(condAccess.Operation);
			return receiver is IParameterReferenceOperation recvRef && SymbolEqualityComparer.Default.Equals(recvRef.Parameter, param);
		}

		return false;
	}

	private static IOperation? unwrap(IOperation? op) {
		while (op is not null) {
			switch (op) {
			case IConversionOperation conv:
				op = conv.Operand;
				continue;
			case IParenthesizedOperation paren:
				op = paren.Operand;
				continue;
			default:
				return op;
			}
		}
		return null;
	}

	private static IEnumerable<YieldStatementSyntax> getDirectYieldStatements(SyntaxNode decl) => decl
		.DescendantNodes(node => node is not AnonymousFunctionExpressionSyntax && node is not LocalFunctionStatementSyntax)
		.OfType<YieldStatementSyntax>()
		.Where(statement => statement.IsKind(SyntaxKind.YieldReturnStatement));

	private static Location getDeclLocation(IMethodSymbol method, SyntaxNode decl, SyntaxTree tree) {
		return decl switch {
			MethodDeclarationSyntax methodDecl => methodDecl.Identifier.GetLocation(),
			LocalFunctionStatementSyntax localFn => localFn.Identifier.GetLocation(),
			ParenthesizedLambdaExpressionSyntax lambda => lambda.ArrowToken.GetLocation(),
			SimpleLambdaExpressionSyntax lambda => lambda.ArrowToken.GetLocation(),
			AnonymousMethodExpressionSyntax anon => anon.DelegateKeyword.GetLocation(),
			_ => method.Locations.FirstOrDefault(l => l.IsInSource && l.SourceTree == tree) ?? decl.GetLocation(),
		};
	}

	private static Location? getParamLocation(IParameterSymbol param, SyntaxTree tree, CancellationToken ct) {
		foreach (SyntaxReference? sxRef in param.DeclaringSyntaxReferences) {
			if (sxRef.SyntaxTree != tree)
				continue;
			if (sxRef.GetSyntax(ct) is ParameterSyntax paramSx)
				return paramSx.Identifier.GetLocation();
		}
		return param.Locations.FirstOrDefault(l => l.IsInSource && l.SourceTree == tree);
	}
}
