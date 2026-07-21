// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using Monocle;
using Reprimand.Celeste;

namespace Reprimand.Extensions;

/// <summary>
/// Provides utility/convenience extensions for <see cref="Entity"/>.
/// </summary>
public static class EntityExtensions {
	extension(Entity e) {
		/// <summary>
		/// Produces a new <see cref="EntityUsageException"/> representing a usage error of this entity
		/// on the mapper's part.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Intended to be used like, for instance, <c>throw this.UsageError($"invalid prefix {pfx}");</c>.
		/// </para>
		/// <para>
		/// The exception, if thrown from the constructor, <c>Added</c>, or <c>Awake</c> of an entity,
		/// is caught and turned into an in-game error popup rather than being thrown out like a normal exception.
		/// See <see cref="EntityUsageException"/> for more info.
		/// </para>
		/// </remarks>
		public EntityUsageException UsageError(string message) => new(e, message);
	}
}
