// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

using Microsoft.Xna.Framework;
using Monocle;

namespace Reprimand.Runtime.Celeste;

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
/// <para>
/// While inside <see cref="global::Celeste.Level"/>, rather than being thrown up to the Everest
/// exception handler like a normal exception would, this exception type is caught and turned into
/// an in-game error popup when thrown out of a custom entity's constructor, <c>Added</c>, or
/// <c>Awake</c> methods.
/// </para>
/// <para>
/// If the offending entity is already in a <see cref="global::Celeste.Level"/>, it is removed.
/// If the exception is thrown out of <c>Awake</c> (as opposed to the constructor or <c>Added</c>),
/// the entity's <c>Removed</c> method is called; this is subject to change before the first stable release.
/// </para>
/// </remarks>
public /* open */ class EntityUsageException : Exception {
	/// <summary>
	/// The entity that threw the exception.
	/// </summary>
	/// <remarks>
	/// <b>May be partially constructed.</b> If the base ctor ran and then the deriving type's
	/// ctor threw this exception, you've now got a partially constructed instance of the deriving type.
	/// It's still fine to use as an <see cref="Entity"/> instance, but be careful.
	/// </remarks>
	internal Entity Entity { get; }

	/// <summary>
	/// Constructs a new instance of <see cref="EntityUsageException"/>; only directly accessible
	/// to deriving classes.
	/// </summary>
	protected internal EntityUsageException(Entity entity, string message) : base(message) {
		Entity = entity;
	}
}
