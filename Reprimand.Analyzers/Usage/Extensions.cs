// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Reprimand.Analyzers.Usage;

internal static class Extensions {
	extension([NotNullWhen(true)] ITypeSymbol? type) {
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
}
