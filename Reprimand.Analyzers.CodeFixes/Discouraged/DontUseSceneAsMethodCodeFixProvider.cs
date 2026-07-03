// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Reprimand.Analyzers.CodeFixes.Discouraged;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DontUseSceneAsMethodCodeFixProvider))]
[Shared]
public sealed class DontUseSceneAsMethodCodeFixProvider : CodeFixProvider {
	private const string explicitCastKey = "SceneAs.ExplicitCast";
	private const string asCastKey = "SceneAs.AsCast";

	public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
		Diagnostics.Usage.DontUseSceneAsMethod.Id
	);

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext ctx) {
		SyntaxNode? root = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
		if (root is null)
			return;

		Diagnostic diagnostic = ctx.Diagnostics[0];
		SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

		InvocationExpressionSyntax? inv = node as InvocationExpressionSyntax ?? node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
		if (inv is null || !tryGetReplacementParts(inv, out TypeSyntax? type, out ExpressionSyntax? scene))
			return;

		// register first so it's normally presented as the primary fix
		ctx.RegisterCodeFix(
			CodeAction.Create(
				title: "Replace SceneAs<T> with explicit cast",
				createChangedDocument: ct => replaceAsync(
					ctx.Document,
					inv,
					type,
					scene,
					useAs: false,
					ct
				),
				equivalenceKey: explicitCastKey
			),
			diagnostic
		);

		// don't offer `Scene as T` when the `T` is definitely invalid for the cast
		SemanticModel? semanticModel = await ctx.Document.GetSemanticModelAsync(ctx.CancellationToken).ConfigureAwait(false);

		ITypeSymbol? targetType = semanticModel?.GetTypeInfo(type, ctx.CancellationToken).Type;
		if (targetType is not null && canUseAs(targetType)) {
			ctx.RegisterCodeFix(
				CodeAction.Create(
					title: "Replace SceneAs<T> with 'as' cast",
					createChangedDocument: ct => replaceAsync(
						ctx.Document,
						inv,
						type,
						scene,
						useAs: true,
						ct
					),
					equivalenceKey: asCastKey
				),
				diagnostic
			);
		}
	}

	private static bool tryGetReplacementParts(InvocationExpressionSyntax inv, out TypeSyntax type, out ExpressionSyntax scene) {
		GenericNameSyntax genericName;
		switch (inv.Expression) {
		// SceneAs<T>()
		case GenericNameSyntax generic:
			genericName = generic;
			scene = SyntaxFactory.IdentifierName("Scene");
			break;

		// obj.SceneAs<T>() -> obj.Scene
		case MemberAccessExpressionSyntax { Name: GenericNameSyntax generic } memberAccess:
			genericName = generic;
			scene = SyntaxFactory.MemberAccessExpression(
				SyntaxKind.SimpleMemberAccessExpression,
				memberAccess.Expression.WithoutLeadingTrivia().WithoutTrailingTrivia(),
				SyntaxFactory.IdentifierName("Scene")
			);
			break;

		default:
			type = null!;
			scene = null!;
			return false;
		}

		if (genericName.TypeArgumentList.Arguments.Count != 1) {
			type = null!;
			scene = null!;
			return false;
		}

		type = genericName.TypeArgumentList.Arguments[0];
		return true;
	}

	private static bool canUseAs(ITypeSymbol type) {
		if (type.IsReferenceType)
			return true;
		if (type is ITypeParameterSymbol typeParameter)
			return typeParameter.HasReferenceTypeConstraint;
		return type is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
	}

	private static async Task<Document> replaceAsync(
		Document doc,
		InvocationExpressionSyntax inv,
		TypeSyntax type,
		ExpressionSyntax scene,
		bool useAs,
		CancellationToken ct
	) {
		SyntaxNode? root = await doc.GetSyntaxRootAsync(ct).ConfigureAwait(false);
		if (root is null)
			return doc;

		type = type.WithoutLeadingTrivia().WithoutTrailingTrivia();

		ExpressionSyntax replacement;
		if (useAs) {
			// (Scene as T)
			replacement = SyntaxFactory.ParenthesizedExpression(
				SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, scene, type)
			);
		} else {
			// ((T)Scene)
			replacement = SyntaxFactory.ParenthesizedExpression(
				SyntaxFactory.CastExpression(type, scene)
			);
		}
		replacement = replacement.WithTriviaFrom(inv).WithAdditionalAnnotations(Formatter.Annotation);

		return doc.WithSyntaxRoot(root.ReplaceNode(inv, replacement));
	}
}
