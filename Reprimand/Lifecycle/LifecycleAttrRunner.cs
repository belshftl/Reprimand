// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Reflection;
using Celeste.Mod;
using MonoMod.Utils;

namespace Reprimand.Lifecycle;

/// <summary>
/// A record of calls performed by <see cref="LifecycleAttrRunner"/> on load, used to undo them
/// later on unload.
/// </summary>
public sealed class LifecycleAttrCallRecord {
	private MethodInfo[]? undoMethods;
	internal LifecycleAttrCallRecord(MethodInfo[] undoMethods) {
		this.undoMethods = undoMethods ?? throw new InternalStateException("LifecycleAttrCallRecord ctor passed null");
	}
	internal MethodInfo[] Consume() => Interlocked.Exchange(ref undoMethods, null) ??
		throw new InvalidOperationException("the calls in this LifecycleAttrCallRecord have already been undone");
}

/// <summary>
/// Methods to run lifecycle attribute methods in an assembly.
/// </summary>
public static class LifecycleAttrRunner {
	/// <summary>
	/// Runs the on-load lifecycle attribute methods in a given assembly.
	/// </summary>
	/// <param name="asm">
	/// Assembly to find and run the lifecycle attribute methods in.
	/// </param>
	/// <param name="detourId">
	/// MonoMod <see cref="global::MonoMod.RuntimeDetour.DetourConfigContext"/> detour ID to set for
	/// the invocations.
	/// </param>
	/// <remarks>
	/// Usually, <paramref name="detourId"/> should be some identifier-friendly form of your mod's name,
	/// for example <c>"MyAwesomeHelper"</c>; what matters is that, after a public release, you don't
	/// change it without incrementing the major version of your mod, since other mods may start relying
	/// on it.
	/// </remarks>
	public static LifecycleAttrCallRecord OnLoad(Assembly asm, string detourId) {
		List<MethodInfo> undoList = new();
		foreach ((MethodInfo m, IOnLoadLifecycleAttribute a) in getOnLoadMethods(asm, typeof(OnLoadAttribute), typeof(OnLoadIfOptionalDepAttribute))) {
			if (m.DeclaringType is null) {
				Logger.Log(LogLevel.Warn, "Reprimand/LifecycleAttrRunner", $"on-load lifecycle method '{m.Name}' has no declaring type; not calling it");
				continue;
			}
			if (m.DeclaringType.FullName is null) {
				Logger.Log(LogLevel.Warn, "Reprimand/LifecycleAttrRunner", $"on-load lifecycle method '{m.Name}' has a declaring type without a resolved full name; not calling it");
				continue;
			}
			switch (a) {
			case OnLoadAttribute onLoad:
				addUndoAndInvoke(m.DeclaringType.FullName + '.' + m.Name, m.DeclaringType, onLoad.UndoMethod, undoList, () => invokeParamless(m, detourId));
				break;
			case OnLoadOneshotAttribute:
				invokeParamless(m, detourId);
				break;
			case OnLoadIfOptionalDepAttribute opt:
				addUndoAndInvoke(m.DeclaringType.FullName + '.' + m.Name, m.DeclaringType, opt.UndoMethod, undoList, () => invokeOptDep(m, opt.Wanted, detourId));
				break;
			case OnLoadIfOptionalDepOneshotAttribute optOneshot:
				invokeOptDep(m, optOneshot.Wanted, detourId);
				break;
			default:
				throw new InternalStateException("IOnLoadLifecycleAttribute implementor doesn't match any expected known types");
			}
		}
		undoList.Reverse();
		return new LifecycleAttrCallRecord(undoList.ToArray());
	}

	/// <summary>
	/// Runs the on-load lifecycle attribute methods in the assembly of a given <see cref="EverestModule"/>.
	/// </summary>
	/// <param name="m">
	/// Module whose assembly should be searched to find and run the lifecycle attributes in.
	/// </param>
	/// <param name="detourId">
	/// MonoMod <see cref="global::MonoMod.RuntimeDetour.DetourConfigContext"/> detour ID to set for
	/// the invocations.
	/// </param>
	/// <remarks>
	/// <para>
	/// Usually, <paramref name="detourId"/> should be some identifier-friendly form of your mod's name,
	/// for example <c>"MyAwesomeHelper"</c>; what matters is that, after a public release, you don't
	/// change it without incrementing the major version of your mod, since other mods may start relying
	/// on it.
	/// </para>
	/// <para>
	/// Convenience overload for <see cref="OnLoad(Assembly, string)"/>.
	/// </para>
	/// </remarks>
	public static LifecycleAttrCallRecord OnLoad(EverestModule m, string detourId) => OnLoad(m.GetType().Assembly, detourId);

	/// <summary>
	/// Runs the undo methods for the on-load lifecycle attribute method calls recorded in a
	/// <see cref="LifecycleAttrCallRecord"/>.
	/// </summary>
	/// <remarks>
	/// <paramref name="record"/> is consumed by this call and becomes unusable.
	/// </remarks>
	public static void OnUnload(LifecycleAttrCallRecord record) {
		MethodInfo[] methods = record.Consume();
		foreach (MethodInfo m in methods) {
			Logger.Log(LogLevel.Debug, "Reprimand/LifecycleAttrRunner", $"invoking on-load lifecycle undo method '{m.DeclaringType!.FullName}.{m.Name}'");
			m.Invoke(null, null);
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
						if (m.IsStatic && !m.IsGenericMethodDefinition && m.ReturnType == typeof(void)) {
							result.Add((m, cast));
						} else {
							string why = !m.IsStatic ? "is non-static" : (m.IsGenericMethodDefinition ? "is generic" : "doesn't return void");
							Logger.Log(LogLevel.Warn, "Reprimand/LifecycleAttrRunner", $"method '{m.DeclaringType?.FullName ?? "<unknown class>"}.{m.Name}' is marked with a lifecycle attribute but isn't a valid lifecycle attribute method because it {why}; not calling it");
						}
					}
				}
			}
		}
		result.Sort((a, b) => {
			int c = a.attr.Priority.CompareTo(b.attr.Priority);
			if (c != 0)
				return c;
			return orderMethods(a.m, b.m);
		});
		return result;
	}

	private static int orderMethods(MethodInfo a, MethodInfo b) {
		int cmp = StringComparer.Ordinal.Compare(a.DeclaringType?.AssemblyQualifiedName, b.DeclaringType?.AssemblyQualifiedName);
		if (cmp != 0)
			return cmp;

		cmp = StringComparer.Ordinal.Compare(a.Name, b.Name);
		if (cmp != 0)
			return cmp;

		ParameterInfo[] ap = a.GetParameters();
		ParameterInfo[] bp = b.GetParameters();
		cmp = ap.Length.CompareTo(bp.Length);
		if (cmp != 0)
			return cmp;
		for (int i = 0; i < ap.Length; i++) {
			cmp = StringComparer.Ordinal.Compare(ap[i].ParameterType.AssemblyQualifiedName, bp[i].ParameterType.AssemblyQualifiedName);
			if (cmp != 0)
				return cmp;
		}

		cmp = StringComparer.Ordinal.Compare(a.ReturnType.AssemblyQualifiedName, b.ReturnType.AssemblyQualifiedName);
		if (cmp != 0)
			return cmp;

		return a.MetadataToken.CompareTo(b.MetadataToken);
	}

	private static void addUndoAndInvoke(string methodName, Type declaringType, string undoMethod, List<MethodInfo> undoList, Action callback) {
		MethodInfo undo = declaringType.GetMethod(undoMethod, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) ??
			throw new MissingMethodException(declaringType.FullName, undoMethod);
		if (undo.IsStatic && !undo.IsGenericMethodDefinition && undo.ReturnType == typeof(void) && undo.GetParameters().Length == 0) {
			undoList.Add(undo);
			callback();
		} else {
			string why = !undo.IsStatic ? "is non-static" : (undo.IsGenericMethodDefinition ? "is generic" : (undo.ReturnType != typeof(void) ? "doesn't return void" : "takes in more than 0 parameters"));
			Logger.Log(LogLevel.Warn, "Reprimand/LifecycleAttrRunner", $"on-load lifecycle method '{methodName}' has an invalid undo counterpart as said counterpart {why}; not calling it");
		}
	}

	private static void invokeParamless(MethodInfo m, string detourId) {
		if (m.GetParameters().Length != 0) {
			Logger.Log(LogLevel.Warn, "Reprimand/LifecycleAttrRunner", $"on-load lifecycle method '{m.DeclaringType!.FullName}.{m.Name}' isn't valid because it takes in more than 0 parameters; not calling it");
		} else {
			Logger.Log(LogLevel.Debug, "Reprimand/LifecycleAttrRunner", $"invoking on-load lifecycle method '{m.DeclaringType!.FullName}.{m.Name}'");
			using (new global::MonoMod.RuntimeDetour.DetourConfigContext(new global::MonoMod.RuntimeDetour.DetourConfig(detourId)).Use())
				m.Invoke(null, null);
		}
	}

	private static void invokeOptDep(MethodInfo m, EverestModuleMetadata wanted, string detourId) {
		ParameterInfo[] @params = m.GetParameters();
		if (@params.Length != 0)
			if (@params.Length != 1 || @params[0].ParameterType.IsByRef || @params[0].ParameterType.IsAssignableFrom(typeof(EverestModule)))
				Logger.Log(LogLevel.Warn, "Reprimand/LifecycleAttrRunner", $"on-load lifecycle method '{m.DeclaringType!.FullName}.{m.Name}' isn't valid because it takes in something either than 0 parameters or 1 parameter of type EverestModule; not calling it");
		if (Everest.Loader.TryGetDependency(wanted, out EverestModule mod)) {
			Logger.Log(LogLevel.Debug, "Reprimand/LifecycleAttrRunner", $"invoking on-load lifecycle method '{m.DeclaringType!.FullName}.{m.Name}'");
			using (new global::MonoMod.RuntimeDetour.DetourConfigContext(new global::MonoMod.RuntimeDetour.DetourConfig(detourId)).Use()) {
				if (@params.Length == 0)
					m.Invoke(null, null);
				else
					m.Invoke(null, [mod]);
			}
		}
	}
}
