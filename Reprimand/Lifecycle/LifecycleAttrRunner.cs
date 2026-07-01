// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Celeste.Mod;

namespace Reprimand.Lifecycle;

/// <summary>
/// Methods to run lifecycle attribute methods in an assembly.
/// </summary>
public static class LifecycleAttrRunner {
	/// <summary>
	/// Runs the on-load lifecycle attribute methods in a given assembly.
	/// </summary>
	public static void OnLoad(Assembly asm) {
		foreach ((MethodInfo m, IOnLoadLifecycleAttribute a) in getOnLoadMethods(asm, typeof(OnLoadAttribute), typeof(OnLoadWithOptionalDepAttribute))) {
			switch (a) {
			case OnLoadAttribute:
				if (m.GetParameters().Length != 0) {
					Logger.Log(LogLevel.Warn, "Reprimand/LifecycleAttrRunner", $"method '{m.DeclaringType?.FullName ?? "<unknown class>"}.{m.Name}' is marked with a lifecycle attribute but isn't a valid lifecycle attribute method because it takes in more than 0 parameters; not calling it");
				} else {
					Logger.Log(LogLevel.Debug, "Reprimand/LifecycleAttrRunner", $"invoking on-load lifecycle method '{m.DeclaringType?.FullName ?? "<unknown class>"}.{m.Name}'");
					m.Invoke(null, null);
				}
				break;
			case OnLoadWithOptionalDepAttribute opt:
				ParameterInfo[] @params = m.GetParameters();
				if (@params.Length != 0)
					if (@params.Length != 1 || @params[0].ParameterType.IsByRef || @params[0].ParameterType.IsAssignableFrom(typeof(EverestModule)))
						Logger.Log(LogLevel.Warn, "Reprimand/LifecycleAttrRunner", $"method '{m.DeclaringType?.FullName ?? "<unknown class>"}.{m.Name}' is marked with a optional-dep lifecycle attribute but isn't a valid optional-dep lifecycle attribute method because it takes in something that isn't either no parameters or a single parameter of type EverestModule; not calling it");
				if (Everest.Loader.TryGetDependency(opt.Wanted, out EverestModule mod)) {
					Logger.Log(LogLevel.Debug, "Reprimand/LifecycleAttrRunner", $"invoking on-load lifecycle method '{m.DeclaringType?.FullName ?? "<unknown class>"}.{m.Name}'");
					if (@params.Length == 0)
						m.Invoke(null, null);
					else
						m.Invoke(null, [mod]);
				}
				break;
			default:
				throw new InternalStateException("expected IOnLoadLifecycleAttribute implementor to be OnLoadAttribute or OnLoadWithOptionalDepAttribute");
			}
		}
	}

	/// <summary>
	/// Runs the on-load lifecycle attribute methods in the assembly of a given <see cref="EverestModule"/>.
	/// </summary>
	/// <remarks>
	/// Convenience overload for <see cref="OnLoad(Assembly)"/>.
	/// </remarks>
	public static void OnLoad(EverestModule m) => OnLoad(m.GetType().Assembly);

	/// <summary>
	/// Runs the on-unload lifecycle attribute methods in a given assembly.
	/// </summary>
	public static void OnUnload(Assembly asm) {
		foreach ((MethodInfo m, IOnUnloadLifecycleAttribute a) in getOnUnloadMethods(asm, typeof(OnUnloadAttribute), typeof(OnUnloadWithOptionalDepAttribute))) {
			switch (a) {
			case OnUnloadAttribute:
				if (m.GetParameters().Length != 0) {
					Logger.Log(LogLevel.Warn, "Reprimand/LifecycleAttrRunner", $"method '{m.DeclaringType?.FullName ?? "<unknown class>"}.{m.Name}' is marked with a lifecycle attribute but isn't a valid lifecycle attribute method because it takes in more than 0 parameters; not calling it");
				} else {
					Logger.Log(LogLevel.Debug, "Reprimand/LifecycleAttrRunner", $"invoking on-unload lifecycle method '{m.DeclaringType?.FullName ?? "<unknown class>"}.{m.Name}'");
					m.Invoke(null, null);
				}
				break;
			case OnUnloadWithOptionalDepAttribute opt:
				ParameterInfo[] @params = m.GetParameters();
				if (@params.Length != 0)
					if (@params.Length != 1 || @params[0].ParameterType.IsByRef || @params[0].ParameterType.IsAssignableFrom(typeof(EverestModule)))
						Logger.Log(LogLevel.Warn, "Reprimand/LifecycleAttrRunner", $"method '{m.DeclaringType?.FullName ?? "<unknown class>"}.{m.Name}' is marked with a optional-dep lifecycle attribute but isn't a valid optional-dep lifecycle attribute method because it takes in something that isn't either no parameters or a single parameter of type EverestModule; not calling it");
				if (Everest.Loader.TryGetDependency(opt.Wanted, out EverestModule mod)) {
					Logger.Log(LogLevel.Debug, "Reprimand/LifecycleAttrRunner", $"invoking on-unload lifecycle method '{m.DeclaringType?.FullName ?? "<unknown class>"}.{m.Name}'");
					if (@params.Length == 0)
						m.Invoke(null, null);
					else
						m.Invoke(null, [mod]);
				}
				break;
			default:
				throw new InternalStateException("expected IOnUnloadLifecycleAttribute implementor to be OnUnloadAttribute or OnUnloadWithOptionalDepAttribute");
			}
		}
	}

	private static List<(MethodInfo m, IOnLoadLifecycleAttribute attr)> getOnLoadMethods(Assembly asm, params Type[] attrTypes) {
		if (attrTypes.Length == 0)
			throw new InternalStateException("getOnLoadMethods() takes in at least one attr type argument");

		// enumerate upfront to hit ReflectionTypeLoadException if there is one
		Type[] types;
		try {
			types = asm.GetTypes().ToArray();
		} catch (ReflectionTypeLoadException e) {
			Logger.Log(LogLevel.Warn, "Reprimand/LifecycleAttrRunner", $"caught ReflectionTypeLoadException getting the types declared in assembly '{asm.FullName}'; that assembly may be missing some dependency or only partially loaded");
			types = e.Types.Where(static t => t is not null).ToArray()!;
		}

		List<(MethodInfo m, IOnLoadLifecycleAttribute attr)> result = new();
		foreach (Type t in types) {
			foreach (MethodInfo m in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
				foreach (Type a in attrTypes) {
					if (m.GetCustomAttribute(a, inherit: false) is {} attr) {
						var cast = (IOnLoadLifecycleAttribute)attr;
						if (m.IsStatic && !m.IsGenericMethodDefinition)
							result.Add((m, cast));
						else
							Logger.Log(LogLevel.Warn, "Reprimand/LifecycleAttrRunner", $"method '{m.DeclaringType?.FullName ?? "<unknown class>"}.{m.Name}' is marked with a lifecycle attribute but isn't a valid lifecycle attribute method because it's {(!m.IsStatic ? "non-static" : "generic")}; not calling it");
					}
				}
			}
		}
		result.Sort((a, b) => {
			int c = a.attr.Order.CompareTo(b.attr.Order);
			if (c != 0)
				return c;
			return orderMethods(a.m, b.m, reverse: false);
		});
		return result;
	}

	private static List<(MethodInfo m, IOnUnloadLifecycleAttribute attr)> getOnUnloadMethods(Assembly asm, params Type[] attrTypes) {
		if (attrTypes.Length == 0)
			throw new InternalStateException("getOnUnloadMethods() takes in at least one attr type argument");

		// enumerate upfront to hit ReflectionTypeUnloadException if there is one
		Type[] types;
		try {
			types = asm.GetTypes().ToArray();
		} catch (ReflectionTypeLoadException e) {
			Logger.Log(LogLevel.Warn, "Reprimand/LifecycleAttrRunner", $"caught ReflectionTypeLoadException getting the types declared in assembly '{asm.FullName}'; that assembly may be missing some dependency or only partially loaded");
			types = e.Types.Where(static t => t is not null).ToArray()!;
		}

		List<(MethodInfo m, IOnUnloadLifecycleAttribute attr)> result = new();
		foreach (Type t in types) {
			foreach (MethodInfo m in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
				foreach (Type a in attrTypes) {
					if (m.GetCustomAttribute(a, inherit: false) is {} attr) {
						var cast = (IOnUnloadLifecycleAttribute)attr;
						if (m.IsStatic && !m.IsGenericMethodDefinition)
							result.Add((m, cast));
						else
							Logger.Log(LogLevel.Warn, "Reprimand/LifecycleAttrRunner", $"method '{m.DeclaringType?.FullName ?? "<unknown class>"}.{m.Name}' is marked with a lifecycle attribute but isn't a valid lifecycle attribute method because it's {(!m.IsStatic ? "non-static" : "generic")}; not calling it");
					}
				}
			}
		}
		result.Sort((a, b) => {
			int c = b.attr.ReverseOrder.CompareTo(a.attr.ReverseOrder);
			if (c != 0)
				return c;
			return orderMethods(a.m, b.m, reverse: true);
		});
		return result;
	}

	private static int orderMethods(MethodInfo a, MethodInfo b, bool reverse) {
		int mul = reverse ? -1 : 1;

		int cmp = StringComparer.Ordinal.Compare(a.DeclaringType?.AssemblyQualifiedName, b.DeclaringType?.AssemblyQualifiedName);
		if (cmp != 0)
			return cmp * mul;

		cmp = StringComparer.Ordinal.Compare(a.Name, b.Name);
		if (cmp != 0)
			return cmp * mul;

		ParameterInfo[] ap = a.GetParameters();
		ParameterInfo[] bp = b.GetParameters();
		cmp = ap.Length.CompareTo(bp.Length);
		if (cmp != 0)
			return cmp * mul;
		for (int i = 0; i < ap.Length; i++) {
			cmp = StringComparer.Ordinal.Compare(ap[i].ParameterType.AssemblyQualifiedName, bp[i].ParameterType.AssemblyQualifiedName);
			if (cmp != 0)
				return cmp * mul;
		}

		cmp = StringComparer.Ordinal.Compare(a.ReturnType.AssemblyQualifiedName, b.ReturnType.AssemblyQualifiedName);
		if (cmp != 0)
			return cmp * mul;

		return a.MetadataToken.CompareTo(b.MetadataToken) * mul;
	}

	/// <summary>
	/// Runs the on-load lifecycle attribute methods in the assembly of a given <see cref="EverestModule"/>.
	/// </summary>
	/// <remarks>
	/// Convenience overload for <see cref="OnUnload(Assembly)"/>.
	/// </remarks>
	public static void OnUnload(EverestModule m) => OnUnload(m.GetType().Assembly);
}
