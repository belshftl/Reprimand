// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Reprimand.Analyzers.Usage;

internal static class Extensions {
	extension([NotNullWhen(true)] ITypeSymbol? type) {
		public bool IsOrDerivesFrom([NotNullWhen(true)] INamedTypeSymbol? candidateBase) {
			if (type is null || candidateBase is null)
				return false;
			for (ITypeSymbol? sym = type; sym is not null; sym = sym.BaseType)
				if (SymbolEqualityComparer.Default.Equals(sym.OriginalDefinition, candidateBase.OriginalDefinition))
					return true;
			return false;
		}

		public bool Implements([NotNullWhen(true)] INamedTypeSymbol? iface) {
			if (type is null || iface is null)
				return false;
			return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iface.OriginalDefinition));
		}

		public bool IsHook(KnownSymbols known) {
			if (type is null)
				return false;
			return
				known.Hook is not null && type.IsOrDerivesFrom(known.Hook) ||
				known.ILHook is not null && type.IsOrDerivesFrom(known.ILHook) ||
				known.NativeHook is not null && type.IsOrDerivesFrom(known.NativeHook)
				;
		}
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
