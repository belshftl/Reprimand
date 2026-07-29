// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

namespace Reprimand.Analyzers.Core;

internal static class KnownMetadataNames {
	public const string ReprimandExtensionsNs = "Reprimand.Extensions";
	public const string ReprimandMonoModNs = "Reprimand.MonoMod";

	public const string ReprimandRuntimeExtensionsNs = "Reprimand.Runtime.Extensions";
	public const string ReprimandRuntimeLifecycleNs = "Reprimand.Runtime.Lifecycle";

	public const string OnLoadAttribute = "Reprimand.Runtime.Lifecycle.OnLoadAttribute";
	public const string OnLoadOneshotAttribute = "Reprimand.Runtime.Lifecycle.OnLoadOneshotAttribute";
	public const string OnLoadIfOptionalDepAttribute = "Reprimand.Runtime.Lifecycle.OnLoadIfOptionalDepAttribute";
	public const string OnLoadIfOptionalDepOneshotAttribute = "Reprimand.Runtime.Lifecycle.OnLoadIfOptionalDepOneshotAttribute";
	public const string EverestModule = "Celeste.Mod.EverestModule";
}
