// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

using Celeste.Mod;
using Reprimand.Lifecycle;

namespace Reprimand.Runtime;

internal sealed class ReprimandRuntimeModule : EverestModule {
	public const string DetourId = "Reprimand.Runtime";

	private static ReprimandRuntimeModule? instanceBacking = null;
	public static ReprimandRuntimeModule Instance => Volatile.Read(ref instanceBacking) ?? throw new InvalidOperationException("the module has not been instantiated yet");
	public volatile bool Active;

	private LifecycleAttrCallRecord? onLoadCallRecord;

	public ReprimandRuntimeModule() {
		if (Interlocked.CompareExchange(ref instanceBacking, this, null) is not null)
			throw new InvalidOperationException("an instance of the module has already been instantiated");
#if DEBUG
		Logger.SetLogLevel("Reprimand.Runtime", LogLevel.Verbose);
#else
		Logger.SetLogLevel("Reprimand.Runtime", LogLevel.Info);
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
			throw new InvalidOperationException("the runtime mod's hooks are either not active yet or already uninstalled; did you forget to declare an Everest dependency on Reprimand.Runtime?");
	}
}
