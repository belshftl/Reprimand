// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System;

using Celeste.Mod;

namespace Reprimand.Lifecycle;

internal interface IOnLoadLifecycleAttribute {
	int Order { get; }
}

internal interface IOnUnloadLifecycleAttribute {
	int ReverseOrder { get; }
}

/// <summary>
/// Marks a static, non-generic, parameterless method to be called during mod load.
/// This is an on-load lifecycle attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class OnLoadAttribute : Attribute, IOnLoadLifecycleAttribute {
	/// <summary>
	/// Ordering relative to every other on-load lifecycle attribute. Calls are performed in ascending
	/// priority, so -1 would be called earlier than 0 and 1 would be called later than 0.
	/// </summary>
	public int Order { get; init; } = 0;
}

/// <summary>
/// Marks a static, non-generic method with either no parameters or a single <see cref="EverestModule"/>-typed
/// parameter to be called during mod load if an optional dependency is present.
/// This is an on-load lifecycle attribute.
/// </summary>
/// <remarks>
/// While this technically works for required dependencies too, a required dependency being missing
/// already prevents the mod from being loaded, so using this for a required dependency would be redundant.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class OnLoadWithOptionalDepAttribute : Attribute, IOnLoadLifecycleAttribute {
	/// <summary>
	/// The optional dependency in question.
	/// </summary>
	public EverestModuleMetadata Wanted { get; }

	/// <inheritdoc cref="OnLoadAttribute.Order"/>
	public int Order { get; init; } = 0;

	/// <summary>
	/// Constructs a new <see cref="OnLoadWithOptionalDepAttribute"/> instance by using the
	/// <paramref name="name"/> and <paramref name="version"/> parameters to construct an
	/// <see cref="EverestModuleMetadata"/>.
	/// </summary>
	/// <param name="name">The name of the mod, as specified in its <c>everest.yaml</c>.</param>
	/// <param name="version">
	/// Semver version string requesting a compatible mod version, internally parsed into a <see cref="Version"/>.
	/// </param>
	public OnLoadWithOptionalDepAttribute(string name, string version) {
		if (version is not null) {
			Wanted = new EverestModuleMetadata {
				Name = name,
				Version = Version.Parse(version),
			};
		} else {
			Wanted = new EverestModuleMetadata {
				Name = name,
			};
		}
	}
}

/// <summary>
/// Marks a static, non-generic, parameterless method to be called during mod unload.
/// This is an on-unload lifecycle attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class OnUnloadAttribute : Attribute, IOnUnloadLifecycleAttribute {
	/// <summary>
	/// Ordering relative to every other on-unload lifecycle attribute. Calls are performed in <b>descending</b>
	/// priority, so 1 would be called earlier than 0 and -1 would be called later than 0.
	/// </summary>
	/// <remarks>
	/// The seemingly strange design choice to make the order ascending on on-load lifecycle attributes
	/// but descending on on-unload ones is so that you can specify the same number for something that
	/// instantiates hooks/state/etc and something that tears it down, and get reverse registration
	/// order teardown.
	/// </remarks>
	public int ReverseOrder { get; init; } = 0;
}

/// <summary>
/// Marks a static, non-generic method with either no parameters or a single <see cref="EverestModule"/>-typed
/// parameter to be called during mod load if an optional dependency is present.
/// This is an on-unload lifecycle attribute.
/// </summary>
/// <remarks>
/// While this technically works for required dependencies too, a required dependency being missing
/// already prevents the mod from being loaded in the first place, so using this for a required
/// dependency would be redundant.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class OnUnloadWithOptionalDepAttribute : Attribute, IOnUnloadLifecycleAttribute {
	/// <summary>
	/// The optional dependency in question.
	/// </summary>
	public EverestModuleMetadata Wanted { get; }

	/// <inheritdoc cref="OnUnloadAttribute.ReverseOrder"/>
	public int ReverseOrder { get; init; } = 0;

	/// <summary>
	/// Constructs a new <see cref="OnUnloadWithOptionalDepAttribute"/> instance by using the
	/// <paramref name="name"/> and <paramref name="version"/> parameters to construct an
	/// <see cref="EverestModuleMetadata"/>.
	/// </summary>
	/// <param name="name">The name of the mod, as specified in its <c>everest.yaml</c>.</param>
	/// <param name="version">
	/// Semver version string requesting a compatible mod version, internally parsed into a <see cref="Version"/>.
	/// </param>
	public OnUnloadWithOptionalDepAttribute(string name, string version) {
		if (version is not null) {
			Wanted = new EverestModuleMetadata {
				Name = name,
				Version = Version.Parse(version),
			};
		} else {
			Wanted = new EverestModuleMetadata {
				Name = name,
			};
		}
	}
}
