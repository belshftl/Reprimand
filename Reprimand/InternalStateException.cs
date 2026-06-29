// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System;

namespace Reprimand;

/// <summary>
/// Exception thrown on internal logic bugs, invariant violations, state corruption,
/// seemingly impossible conditions, bad values for purely-internal types, etc.
/// Informally speaking, you should never see this unless either there's a bug in the library
/// or <c>unsafe</c> code / reflection over internals / mod hooks have messed something up.
/// </summary>
/// <remarks>
/// Do not throw this from your own code. Like, seriously.
/// Complete freedom is reserved to add special treatment for this exception type, including
/// poisoning objects, skipping error handling / cleanup, bailing out on tasks/operations, or
/// aborting the process.
/// </remarks>
public sealed class InternalStateException : Exception {
	internal InternalStateException() {}
	internal InternalStateException(string message) : base(message) {}
	internal InternalStateException(string message, Exception ex) : base(message, ex) {}
}
