// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System;
using System.Threading;

using Celeste.Mod;

using Reprimand.Lifecycle;

namespace Reprimand;

internal sealed class ReprimandModule : EverestModule {
	private static ReprimandModule? instanceBacking = null;
	public static ReprimandModule Instance => Volatile.Read(ref instanceBacking) ?? throw new InvalidOperationException("the module has not been instantiated yet");
	public volatile bool Active;

	public ReprimandModule() {
		if (Interlocked.CompareExchange(ref instanceBacking, this, null) is not null)
			throw new InvalidOperationException("an instance of the module has already been instantiated");
#if DEBUG
		Logger.SetLogLevel("Reprimand", LogLevel.Verbose);
#else
		Logger.SetLogLevel("Reprimand", LogLevel.Info);
#endif
	}

	public override void Load() {
		LifecycleAttrRunner.OnLoad(this);
		Active = true;
	}

	public override void Unload() {
		Active = false;
		LifecycleAttrRunner.OnUnload(this);
	}

	public static void ThrowIfInactive() {
		if (!Instance.Active)
			throw new InvalidOperationException("the library's hooks are either not active yet or already uninstalled; did you forget to declare an Everest dependency on it?");
	}
}
