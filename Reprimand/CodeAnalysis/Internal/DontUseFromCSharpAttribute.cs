// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

namespace Reprimand.CodeAnalysis.Internal;

/// <summary>
/// Signifies that a class, struct, method, property, or field must not be used from C# code.
/// Intended for members meant to be used only from emitted IL.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
internal sealed class DontUseFromCSharpAttribute : Attribute;
