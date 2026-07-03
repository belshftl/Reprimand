// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using Celeste.Mod;

namespace Reprimand.Lifecycle;

internal interface IOnLoadLifecycleAttribute {
	int Priority { get; }
}

/// <summary>
/// <para>
/// Marks a method to be called during mod load, alongside a counterpart to undo hooks/etc from it.
/// The method in question must be static, non-generic, parameterless, and return <c>void</c>.
/// </para>
/// <para>
/// This is an on-load lifecycle attribute.
/// </para>
/// </summary>
/// <remarks>
/// See <see cref="OnLoadOneshotAttribute"/> if you don't have an undo counterpart.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class OnLoadAttribute : Attribute, IOnLoadLifecycleAttribute {
	/// <summary>
	/// Name of the method that cleans up state declared by this method (undoing hooks, etc.)
	/// Must be declared in the same class as this method, as well as be static, non-generic,
	/// parameterless, and return <c>void</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The undo methods of every on-load lifecycle attribute method are called in the reverse of
	/// the order of the original calls.
	/// </para>
	/// <para>
	/// Required; setting this to <see langword="null"/> or an empty string will fail. If you genuinely
	/// don't have an undo counterpart, use <see cref="OnLoadOneshotAttribute"/>.
	/// </para>
	/// </remarks>
	public required string UndoMethod { get; init; }

	/// <summary>
	/// Ordering relative to every other on-load lifecycle attribute. Calls are performed in ascending
	/// priority, so -1 would be called earlier than 0 and 1 would be called later than 0.
	/// </summary>
	public int Priority { get; init; } = 0;
}

/// <summary>
/// <para>
/// Marks a method to be called during mod load. The method in question must be static, non-generic,
/// parameterless, and return <c>void</c>.
/// </para>
/// <para>
/// Unlike the plain <see cref="OnLoadAttribute"/>, this is for methods that have no "undo" counterpart,
/// for example oneshot initialization that doesn't need cleanup.
/// </para>
/// <para>
/// This is an on-load lifecycle attribute.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class OnLoadOneshotAttribute : Attribute, IOnLoadLifecycleAttribute {
	/// <summary>
	/// Ordering relative to every other on-load lifecycle attribute. Calls are performed in ascending
	/// priority, so -1 would be called earlier than 0 and 1 would be called later than 0.
	/// </summary>
	public int Priority { get; init; } = 0;
}

/// <summary>
/// <para>
/// Marks a method to be called during mod load if an optional dependency is present, alongside a
/// counterpart to undo hooks/etc from it.
/// The method in question must be static, non-generic, return <c>void</c>, and take in either no
/// parameters or a single parameter of type <see cref="EverestModule"/>.
/// </para>
/// <para>
/// This is an on-load lifecycle attribute.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// While this technically works for required dependencies too, a required dependency being missing
/// already prevents the mod from being loaded, so using this for a required dependency would be redundant.
/// </para>
/// <para>
/// See <see cref="OnLoadIfOptionalDepOneshotAttribute"/> if you don't have an undo counterpart.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class OnLoadIfOptionalDepAttribute : Attribute, IOnLoadLifecycleAttribute {
	/// <summary>
	/// The optional dependency in question.
	/// </summary>
	public EverestModuleMetadata Wanted { get; }

	/// <summary>
	/// Name of the method that cleans up state declared by this method (undoing hooks, etc.)
	/// Must be declared in the same class as this method, as well as be static, non-generic,
	/// parameterless, and return <c>void</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The undo methods of every on-load lifecycle attribute method are called in the reverse of
	/// the order of the original calls. The undo method only gets called if the original method got
	/// called, so if the optional dependency wasn't present, neither get called.
	/// </para>
	/// <para>
	/// Required; setting this to <see langword="null"/> or an empty string will fail. If you genuinely
	/// don't have an undo counterpart, use <see cref="OnLoadIfOptionalDepOneshotAttribute"/>.
	/// </para>
	/// </remarks>
	public required string UndoMethod { get; init; }

	/// <inheritdoc cref="OnLoadAttribute.Priority"/>
	public int Priority { get; init; } = 0;

	/// <summary>
	/// Constructs a new <see cref="OnLoadIfOptionalDepAttribute"/> instance by using the
	/// <paramref name="name"/> and <paramref name="version"/> parameters to construct an
	/// <see cref="EverestModuleMetadata"/>.
	/// </summary>
	/// <param name="name">The name of the mod, as specified in its <c>everest.yaml</c>.</param>
	/// <param name="version">
	/// Semver version string requesting a compatible mod version, internally parsed into a <see cref="Version"/>.
	/// </param>
	public OnLoadIfOptionalDepAttribute(string name, string version) {
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
/// <para>
/// Marks a method to be called during mod load if an optional dependency is present.
/// The method in question must be static, non-generic, return <c>void</c>, and take in either no
/// parameters or a single parameter of type <see cref="EverestModule"/>.
/// </para>
/// <para>
/// Unlike the plain <see cref="OnLoadIfOptionalDepAttribute"/>, this is for methods that have no
/// "undo" counterpart, for example oneshot initialization that doesn't need cleanup.
/// </para>
/// <para>
/// This is an on-load lifecycle attribute.
/// </para>
/// </summary>
/// <remarks>
/// While this technically works for required dependencies too, a required dependency being missing
/// already prevents the mod from being loaded, so using this for a required dependency would be redundant.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class OnLoadIfOptionalDepOneshotAttribute : Attribute, IOnLoadLifecycleAttribute {
	/// <summary>
	/// The optional dependency in question.
	/// </summary>
	public EverestModuleMetadata Wanted { get; }

	/// <inheritdoc cref="OnLoadAttribute.Priority"/>
	public int Priority { get; init; } = 0;

	/// <summary>
	/// Constructs a new <see cref="OnLoadIfOptionalDepOneshotAttribute"/> instance by using the
	/// <paramref name="name"/> and <paramref name="version"/> parameters to construct an
	/// <see cref="EverestModuleMetadata"/>.
	/// </summary>
	/// <param name="name">The name of the mod, as specified in its <c>everest.yaml</c>.</param>
	/// <param name="version">
	/// Semver version string requesting a compatible mod version, internally parsed into a <see cref="Version"/>.
	/// </param>
	public OnLoadIfOptionalDepOneshotAttribute(string name, string version) {
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
