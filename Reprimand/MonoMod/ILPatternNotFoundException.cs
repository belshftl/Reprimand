// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System;
using System.Globalization;

namespace Reprimand.MonoMod;

/// <summary>
/// Represents a direction in which a search for an IL pattern or sequence was performed or is to
/// be performed.
/// </summary>
public enum ILPatternSearchDirection {
	/// <summary>
	/// The search was/is performed forwards from the cursor's location.
	/// </summary>
	Forward,

	/// <summary>
	/// The search was/is performed backwards from the cursor's location.
	/// </summary>
	Backward,
}

/// <summary>
/// Exception thrown when an IL hook's manipulator fails to locate or match a specific pattern or
/// sequence in the IL of the method being patched.
/// </summary>
public sealed class ILPatternNotFoundException(
	string target,
	string expected,
	int startIndex,
	ILPatternSearchDirection searchDirection
) : InvalidOperationException(fmt(target, expected, startIndex, searchDirection)) {
	/// <summary>
	/// Name of the target method, or a human-readable fallback string if one isn't available.
	/// </summary>
	public string Target { get; } = target;

	/// <summary>
	/// Human-readable description of the expected IL pattern that was being searched for.
	/// </summary>
	public string Expected { get; } = expected;

	/// <summary>
	/// IL cursor index at which the search begun.
	/// </summary>
	public int StartIndex { get; } = startIndex;

	/// <summary>
	/// Direction from <see cref="StartIndex"/> in which the search was performed.
	/// </summary>
	public ILPatternSearchDirection SearchDirection { get; } = searchDirection;

	private static string fmt(string target, string expected, int startIndex, ILPatternSearchDirection searchDirection) {
		string dir = searchDirection switch {
			ILPatternSearchDirection.Forward => "forward",
			ILPatternSearchDirection.Backward => "backward",
			_ => throw new ArgumentOutOfRangeException(nameof(searchDirection), searchDirection, "out of range ILPatternSearchDirection enum value"),
		};
		return $"expected to match IL pattern '{expected}' in method '{target}' searching {dir} from instruction index {startIndex.ToString(CultureInfo.InvariantCulture)}";
	}
}
