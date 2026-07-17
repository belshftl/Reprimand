// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;

namespace Reprimand.Analyzers.CodeFixes.Usage;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreferRequireGotoCodeFixProvider))]
[Shared]
public sealed class PreferRequireGotoCodeFixProvider : CodeFixProvider {
	public const string PlaceholderExpected = "TODO: string form of the IL pattern";

	private const string equivalenceKey = nameof(PreferRequireGotoCodeFixProvider);

	public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
		Diagnostics.Usage.PreferRequireGoto.Id
	);

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext ctx) {
		SyntaxNode? root = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
		SemanticModel? model = await ctx.Document.GetSemanticModelAsync(ctx.CancellationToken).ConfigureAwait(false);
		if (root is null || model is null)
			return;

		// do the replacement ahead of time to avoid offering the codefix action if speculative binding fails
		// i still feel kinda iffy about this but like Ehhh
		foreach (Diagnostic d in ctx.Diagnostics) {
			InvocationExpressionSyntax? sx = root.FindNode(d.Location.SourceSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<InvocationExpressionSyntax>();
			if (sx is null)
				continue;
			if (model.GetOperation(sx, ctx.CancellationToken) is not IInvocationOperation inv)
				continue;

			IMethodSymbol target = inv.TargetMethod;
			// i'm gonna be honest i don't know how you get a Compilation from here so i can't use KnownSymbols
			// so just do this instead, this is a safety guard anyways
			if (target.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != "global::MonoMod.Cil.ILCursor")
				continue;
			if (target.Name is not "GotoNext" and not "GotoPrev")
				continue;

			SimpleNameSyntax? oldName = sx.Expression switch {
				MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
				MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
				SimpleNameSyntax simpleName => simpleName,
				_ => null,
			};
			if (oldName is null)
				continue;

			IArgumentOperation? paramsArgument = inv.Arguments.FirstOrDefault(static a => a.Parameter?.IsParams == true);
			int predicateCount = -1;
			if (paramsArgument is not null && paramsArgument.ArgumentKind == ArgumentKind.ParamArray && paramsArgument.Value is IArrayCreationOperation arrCreat)
				predicateCount = arrCreat.Initializer?.ElementValues.Length ?? 0;

			string replacementName = "Require" + target.Name;
			SyntaxToken newIdent = SyntaxFactory.Identifier(oldName.Identifier.LeadingTrivia, replacementName, oldName.Identifier.TrailingTrivia);

			SimpleNameSyntax newName = oldName switch {
				IdentifierNameSyntax identName => identName.WithIdentifier(newIdent),
				GenericNameSyntax genericName => genericName.WithIdentifier(newIdent),
				_ => oldName,
			};
			InvocationExpressionSyntax replacement = sx.ReplaceNode(oldName, newName);
			if (predicateCount < 1 || predicateCount > 4) {
				ArgumentSyntax expectedArgument = SyntaxFactory.Argument(
					SyntaxFactory.LiteralExpression(
						SyntaxKind.StringLiteralExpression,
						SyntaxFactory.Literal(PlaceholderExpected)
					)
				);
				replacement = replacement.WithArgumentList(
					replacement.ArgumentList.WithArguments(
						replacement.ArgumentList.Arguments.Insert(
							0,
							expectedArgument
						)
					)
				);
			}
			replacement = replacement.WithAdditionalAnnotations(Formatter.Annotation);

			var replacementSymbol = model.GetSpeculativeSymbolInfo(sx.SpanStart, replacement, SpeculativeBindingOption.BindAsExpression).Symbol as IMethodSymbol;
			if (replacementSymbol?.Name != replacementName)
				continue;

			SyntaxNode newRoot = root.ReplaceNode(sx, replacement);
			ctx.RegisterCodeFix(
				CodeAction.Create(
					title: $"Replace with {replacementName}",
					createChangedDocument: _ => Task.FromResult(ctx.Document.WithSyntaxRoot(newRoot)),
					equivalenceKey: equivalenceKey
				),
				d
			);
		}
	}
}
