// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

namespace Reprimand.MonoMod;

/// <summary>
/// Marks a method as being a hook body, either that of a plain detour (<c>On.</c>) or of an
/// IL manipulator (<c>IL.</c>). Currently used for code analysis to determine what is and isn't
/// "inside a hook" or "a hook method"; may get more uses in the future.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class HookMethodAttribute : Attribute;
