// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

namespace Reprimand.CodeAnalysis;

/// <summary>
/// Signifies that a field, property, or method should not be accessed or called inside a
/// static constructor or static field/property initializer as it carries or relies on state that
/// may not be initialized yet when static constructors run.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class DontUseInStaticCtorAttribute : Attribute;
