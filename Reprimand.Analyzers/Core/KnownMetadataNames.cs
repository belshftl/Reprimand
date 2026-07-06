// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

namespace Reprimand.Analyzers.Core;

internal static class KnownMetadataNames {
	public const string ReprimandCelesteNs = "Reprimand.Celeste";
	public const string ReprimandLifecycleNs = "Reprimand.Lifecycle";
	public const string ReprimandMonoModNs = "Reprimand.MonoMod";

	public const string OnLoadAttribute = "Reprimand.Lifecycle.OnLoadAttribute";
	public const string OnLoadOneshotAttribute = "Reprimand.Lifecycle.OnLoadOneshotAttribute";
	public const string OnLoadIfOptionalDepAttribute = "Reprimand.Lifecycle.OnLoadIfOptionalDepAttribute";
	public const string OnLoadIfOptionalDepOneshotAttribute = "Reprimand.Lifecycle.OnLoadIfOptionalDepOneshotAttribute";
	public const string EverestModule = "Celeste.Mod.EverestModule";
}
