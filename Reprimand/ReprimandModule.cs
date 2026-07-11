// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using Celeste.Mod;
using Reprimand.Lifecycle;

namespace Reprimand;

internal sealed class ReprimandModule : EverestModule {
	public const string DetourId = "Reprimand";

	private static ReprimandModule? instanceBacking = null;
	public static ReprimandModule Instance => Volatile.Read(ref instanceBacking) ?? throw new InvalidOperationException("the module has not been instantiated yet");
	public volatile bool Active;

	private LifecycleAttrCallRecord? onLoadCallRecord;

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
		onLoadCallRecord = LifecycleAttrRunner.OnLoad(this, DetourId);
		Active = true;
	}

	public override void Unload() {
		Active = false;
		LifecycleAttrRunner.OnUnload(onLoadCallRecord ?? throw new InvalidOperationException("Unload() called before Load()"));
	}

	public static void ThrowIfInactive() {
		if (!Instance.Active)
			throw new InvalidOperationException("the library's hooks are either not active yet or already uninstalled; did you forget to declare an Everest dependency on Reprimand.Runtime?");
	}
}
