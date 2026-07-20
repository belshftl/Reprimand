// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using Microsoft.Xna.Framework;
using Monocle;
using Reprimand.Celeste;

namespace Reprimand.Extensions;

public static class EntityExtensions {
	extension(Entity e) {
		public EntityUsageException UsageError(string message) => new(e, message);
	}
}
