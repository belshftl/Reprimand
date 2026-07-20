// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using Microsoft.Xna.Framework;
using Monocle;

namespace Reprimand.Celeste;

internal readonly struct EntityUsageErrorInfo {
	public required Entity Entity { get; init; }
	public required int Id { get; init; }
	public required Vector2 Position { get; init; }
	public required string Message { get; init; }
	public required string ThrownFrom { get; init; }
}

/// <summary>
/// Exception to be thrown on bad entity usage from a mapper. Not directly publicly constructible;
/// use <see cref="Extensions.EntityExtensions.UsageError(Entity, string)"/>.
/// </summary>
/// <remarks>
/// Rather than being thrown up to the Everest exception handler like a normal exception would, this
/// exception type and deriving types are caught and turned into an in-game error popup when thrown
/// out of a custom entity's constructor, <c>Added</c>, or <c>Awake</c> methods.
/// </remarks>
public /* open */ class EntityUsageException : Exception {
	/// <summary>
	/// The entity that threw the exception.
	/// </summary>
	public Entity Entity { get; }

	/// <summary>
	/// Constructs a new instance of <see cref="EntityUsageException"/>; only directly accessible
	/// to deriving classes.
	/// </summary>
	protected internal EntityUsageException(Entity entity, string message) : base(message) {
		Entity = entity;
	}
}
