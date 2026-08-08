// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Reprimand.Analyzers;

internal static class Extensions {
	extension([NotNullWhen(true)] ITypeSymbol? t) {
		public bool IsOrDerivesFrom([NotNullWhen(true)] INamedTypeSymbol? candidateBase) {
			if (t is null || candidateBase is null)
				return false;
			for (ITypeSymbol? sym = t; sym is not null; sym = sym.BaseType)
				if (SymbolEqualityComparer.Default.Equals(sym.OriginalDefinition, candidateBase.OriginalDefinition))
					return true;
			return false;
		}

		public bool Implements([NotNullWhen(true)] INamedTypeSymbol? iface) {
			if (t is null || iface is null)
				return false;
			return t.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iface.OriginalDefinition));
		}
	}

	extension(INamedTypeSymbol t) {
		public IEnumerable<IMethodSymbol> GetAllMethods() =>
			t.GetMembers()
				.Concat(
					t
						.GetTypeMembers()
						.Where(static t => t.IsExtension)
						.SelectMany(static t => t.GetMembers())
				)
				.OfType<IMethodSymbol>();
	}

	extension(IFieldReferenceOperation fr) {
		public bool IsWrite([NotNullWhen(true)] out Location? loc) {
			IOperation curr = fr;

			// go only through wrappers which can surround an lvalue
			while (curr.Parent is IParenthesizedOperation or IConversionOperation or ITupleOperation)
				curr = curr.Parent;

			switch (curr.Parent) {
			case IAssignmentOperation asg when ReferenceEquals(asg.Target, curr):
				loc = asg.Syntax.GetLocation();
				return true;
			case IIncrementOrDecrementOperation incr when ReferenceEquals(incr.Target, curr):
				loc = incr.Syntax.GetLocation();
				return true;
			case IArgumentOperation arg when ReferenceEquals(arg.Value, curr) && arg.Parameter?.RefKind is RefKind.Ref or RefKind.Out:
				loc = arg.Syntax.GetLocation();
				return true;
			}

			// reject ref aliases and pointer aliases since you can write through them later
			SyntaxNode sx = fr.Syntax;
			while (sx.Parent is ParenthesizedExpressionSyntax)
				sx = sx.Parent;
			if (sx.Parent is RefExpressionSyntax || sx.Parent is PrefixUnaryExpressionSyntax prefix && prefix.IsKind(SyntaxKind.AddressOfExpression)) {
				loc = sx.Parent.GetLocation();
				return true;
			} else {
				loc = null;
				return false;
			}
		}
	}
}
