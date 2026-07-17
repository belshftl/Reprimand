// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Reprimand.Analyzers.Usage;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IlHookTargetAnalyzer : DiagnosticAnalyzer {
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
		Diagnostics.Usage.PotentiallyWrongIlHookTarget
	);

	public override void Initialize(AnalysisContext context) {
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static ctx => {
				ctx.RegisterOperationAction(analyzeEventAssignment, OperationKind.EventAssignment);
			}
		);
	}

	private static void analyzeEventAssignment(OperationAnalysisContext ctx) {
		var asg = (IEventAssignmentOperation)ctx.Operation;
		if (!asg.Adds)
			return;
		var @ref = (IEventReferenceOperation)asg.EventReference;
		IEventSymbol sym = @ref.Event;
		INamedTypeSymbol hookType = sym.ContainingType;
		if (!tryGetTargetMetadataName(hookType, out string targetMetadataName))
			return;
		if (ctx.Compilation.GetTypeByMetadataName(targetMetadataName) is not INamedTypeSymbol targetType)
			return;
		IMethodSymbol? hookedMethod = targetType.GetMembers(sym.Name).OfType<IMethodSymbol>().FirstOrDefault();
		if (targetType.GetMembers("orig_" + sym.Name).OfType<IMethodSymbol>().FirstOrDefault() is not IMethodSymbol origMethod)
			return;
		ctx.ReportDiagnostic(
			Diagnostic.Create(
				Diagnostics.Usage.PotentiallyWrongIlHookTarget,
				asg.Syntax.GetLocation(),
				$"IL.{targetMetadataName}.{sym.Name}",
				hookedMethod?.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat) ?? "<unknown>",
				origMethod.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)
			)
		);
	}

	private static bool tryGetTargetMetadataName(INamedTypeSymbol hookType, out string metadataName) {
		metadataName = string.Empty;
		INamespaceSymbol ns = hookType.ContainingNamespace;
		if (ns.IsGlobalNamespace)
			return false;

		ImmutableArray<string>.Builder nsParts = ImmutableArray.CreateBuilder<string>();
		for (INamespaceSymbol? curr = ns; curr is { IsGlobalNamespace: false }; curr = curr.ContainingNamespace)
			nsParts.Add(curr.Name);
		nsParts.Reverse();
		if (nsParts.Count == 0 || nsParts[0] != "IL")
			return false;

		ImmutableArray<INamedTypeSymbol>.Builder containingTypes = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
		for (INamedTypeSymbol? curr = hookType; curr is not null; curr = curr.ContainingType)
			containingTypes.Add(curr);
		containingTypes.Reverse();

		StringBuilder sb = new();
		// strip leading `IL.`
		for (int i = 1; i < nsParts.Count; i++) {
			if (sb.Length != 0)
				sb.Append('.');
			sb.Append(nsParts[i]);
		}
		for (int i = 0; i < containingTypes.Count; i++) {
			if (sb.Length != 0)
				sb.Append(i == 0 ? '.' : '+');
			// MetadataName preserves the generic arity notation with the backtick
			sb.Append(containingTypes[i].MetadataName);
		}

		metadataName = sb.ToString();
		return metadataName.Length != 0;
	}
}
