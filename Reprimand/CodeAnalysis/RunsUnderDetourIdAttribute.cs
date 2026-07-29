// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

namespace Reprimand.CodeAnalysis;

/// <summary>
/// Signifies that a method runs under a <see cref="global::MonoMod.RuntimeDetour.DetourConfigContext"/>
/// or other similar mechanism that sets a detour ID, and as such, hooks declared within it can be assumed
/// to be declared under a detour ID.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RunsUnderDetourIdAttribute : Attribute;
