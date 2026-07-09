// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Collections;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Monocle;

namespace Reprimand.Extensions;

/// <summary>
/// Exception thrown when a tracker operation is attempted on an untracked entity/component type.
/// </summary>
public sealed class TypeNotTrackedException(Type type) : Exception($"type '{type}' is not tracked and cannot be used with the tracker");

/// <summary>
/// Exception thrown when a throwing operation to get a tracked entity of a given type such as
/// <see cref="TrackerExtensions.GetEntityExt{T}(Tracker)"/> fails to find any entities of said type.
/// </summary>
public sealed class TrackedEntityNotFoundException(Type entityType) : Exception($"no tracked entities of type '{entityType}' are present in the current scene");

/// <summary>
/// Exception thrown when a throwing operation to get a tracked component of a given type such as
/// <see cref="TrackerExtensions.GetComponentExt{T}(Tracker)"/> fails to find any components of said type.
/// </summary>
public sealed class TrackedComponentNotFoundException(Type componentType) : Exception($"no tracked components of type '{componentType}' are present in the current scene");

/// <summary>
/// Provides utility/convenience extensions for <see cref="Tracker"/>.
/// </summary>
public static class TrackerExtensions {
	private sealed class CastView<TFrom, TTo>(List<TFrom> source) : IReadOnlyList<TTo> where TFrom : class where TTo : TFrom {
		private readonly List<TFrom> source = source;
		public TTo this[int idx] => (TTo)source[idx];
		public int Count => source.Count;
		public IEnumerator<TTo> GetEnumerator() => new Enumerator(source.GetEnumerator());
		IEnumerator IEnumerable.GetEnumerator() => new Enumerator(source.GetEnumerator());

		public sealed class Enumerator(IEnumerator<TFrom> source) : IEnumerator<TTo> {
			public TTo Current => (TTo)source.Current;
			object? IEnumerator.Current => (TTo)source.Current;
			public bool MoveNext() => source.MoveNext();
			public void Reset() => source.Reset();
			public void Dispose() => source.Dispose();
		}
	}

	extension(Tracker tr) {
		/// <summary>
		/// Gets the first found tracked entity of type <typeparamref name="T"/> known by the tracker.
		/// </summary>
		/// <exception cref="TypeNotTrackedException">
		/// Thrown if <typeparamref name="T"/> is not a tracked type.
		/// </exception>
		/// <exception cref="TrackedEntityNotFoundException">
		/// Thrown if no tracked entities of type <typeparamref name="T"/> are present in the scene.
		/// </exception>
		/// <remarks>
		/// <para>
		/// If multiple are present, the one that got added to the scene first is returned; this is an
		/// unreliable metric, so if you expect multiple to be normally present, use
		/// <see cref="GetEntitiesExt{T}(Tracker)"/> and filter out the desired one manually.
		/// </para>
		/// <para>
		/// Near-identical in behavior to <see cref="Tracker.GetEntity{T}"/>; the primary difference is that
		/// the original method silently returns <see langword="null"/> if no entity was found or if the
		/// entity wasn't actually of type <typeparamref name="T"/> (which is very rare, but possible with
		/// <see cref="TrackedAsAttribute"/> edge cases). This method throws on both such cases.
		/// </para>
		/// </remarks>
		public T GetEntityExt<T>() where T : Entity {
			if (!tr.Entities.TryGetValue(typeof(T), out List<Entity>? l))
				throw new TypeNotTrackedException(typeof(T));
			return l.Count == 0 ? throw new TrackedEntityNotFoundException(typeof(T)) : (T)l[0];
		}

		/// <summary>
		/// Gets the first found tracked entity of type <typeparamref name="T"/> known by the tracker,
		/// or <see langword="null"/> if no tracked entities of that type are present in the scene.
		/// </summary>
		/// <exception cref="TypeNotTrackedException">
		/// Thrown if <typeparamref name="T"/> is not a tracked type.
		/// </exception>
		/// <remarks>
		/// <para>
		/// If multiple are present, the one that got added to the scene first is returned; this is an
		/// unreliable metric, so if you expect multiple to be normally present, use
		/// <see cref="GetEntitiesExt{T}(Tracker)"/> and filter out the desired one manually.
		/// </para>
		/// <para>
		/// Near-identical in behavior to <see cref="Tracker.GetEntity{T}"/>; the primary difference is that
		/// the original method silently returns <see langword="null"/> if no entity was found or if the
		/// entity wasn't actually of type <typeparamref name="T"/> (which is very rare, but possible with
		/// <see cref="TrackedAsAttribute"/> edge cases). This method throws on such a type mismatch; it
		/// still has the same null-on-not-found behavior, but is nullable-aware unlike the original method.
		/// </para>
		/// </remarks>
		public T? GetEntityOrDefaultExt<T>() where T : Entity {
			if (!tr.Entities.TryGetValue(typeof(T), out List<Entity>? l))
				throw new TypeNotTrackedException(typeof(T));
			return l.Count == 0 ? null : (T)l[0];
		}

		/// <summary>
		/// Gets the closest entity of type <typeparamref name="T"/> to the point <paramref name="nearestTo"/>.
		/// </summary>
		/// <param name="nearestTo">
		/// The distance of every candidate entity's <see cref="Entity.Position"/> to this point will be measured,
		/// and the one with the smallest distance will be returned.
		/// </param>
		/// <exception cref="TypeNotTrackedException">
		/// Thrown if <typeparamref name="T"/> is not a tracked type.
		/// </exception>
		/// <exception cref="TrackedEntityNotFoundException">
		/// Thrown if no tracked entities of type <typeparamref name="T"/> are present in the scene.
		/// </exception>
		/// <remarks>
		/// Near-identical in behavior to <see cref="Tracker.GetNearestEntity{T}(Vector2)"/>; the primary
		/// difference is that the original method silently returns <see langword="null"/> if no entity
		/// of that type was found, while this method throws.
		/// </remarks>
		public T GetNearestEntityExt<T>(Vector2 nearestTo) where T : Entity => tr.GetNearestEntityOrDefaultExt<T>(nearestTo) ??
			throw new TrackedEntityNotFoundException(typeof(T));

		/// <summary>
		/// Gets the closest entity of type <typeparamref name="T"/> to the point <paramref name="nearestTo"/>,
		/// or <see langword="null"/> if no tracked entities of that type are present in the scene.
		/// </summary>
		/// <param name="nearestTo">
		/// The distance of every candidate entity's <see cref="Entity.Position"/> to this point will be measured,
		/// and the one with the smallest distance will be returned.
		/// </param>
		/// <exception cref="TypeNotTrackedException">
		/// Thrown if <typeparamref name="T"/> is not a tracked type.
		/// </exception>
		/// <remarks>
		/// Near-identical in behavior to <see cref="Tracker.GetNearestEntity{T}(Vector2)"/>; the primary
		/// differences are that of all <c>*Ext</c> tracker methods, those being better internal type safety,
		/// <see cref="TypeNotTrackedException"/>, and nullable awareness.
		/// </remarks>
		public T? GetNearestEntityOrDefaultExt<T>(Vector2 nearestTo) where T : Entity {
			IReadOnlyList<T> ents = tr.GetEntitiesExt<T>();
			float match = float.PositiveInfinity;
			T? matched = null;
			foreach (T ent in ents) {
				float dsq = Vector2.DistanceSquared(nearestTo, ent.Position);
				if (dsq < match) {
					match = dsq;
					matched = ent;
				}
			}
			return matched;
		}

		/// <summary>
		/// Gets every tracked entity of type <typeparamref name="T"/> known by the tracker.
		/// </summary>
		/// <exception cref="TypeNotTrackedException">
		/// Thrown if <typeparamref name="T"/> is not a tracked type.
		/// </exception>
		/// <remarks>
		/// <para>
		/// Near-identical in behavior to <see cref="Tracker.GetEntities{T}"/> but with a fix for the return
		/// type; it's now a list of <typeparamref name="T"/> and not of <see cref="Entity"/>. The original
		/// method is also capable of returning a value that isn't actually of type <typeparamref name="T"/>
		/// in some edge cases with <see cref="TrackedAsAttribute"/>, while this method throws instead.
		/// </para>
		/// <para>
		/// The returned list is a live view of internal state; if you want to mutate it or retain it,
		/// you should copy it out instead, either using <see cref="GetComponentsCopyExt{T}(Tracker)"/> or
		/// plain old <c>.ToArray()</c>. If you're wondering, yeah, the original
		/// <see cref="Tracker.GetEntities{T}"/> has this same behavior, it's just undocumented for some reason.
		/// </para>
		/// </remarks>
		public IReadOnlyList<T> GetEntitiesExt<T>() where T : Entity {
			if (!tr.Entities.TryGetValue(typeof(T), out List<Entity>? l))
				throw new TypeNotTrackedException(typeof(T));
			return new CastView<Entity, T>(l);
		}

		/// <summary>
		/// Gets every tracked entity of type <typeparamref name="T"/> known by the tracker, returning a
		/// copy of the list.
		/// </summary>
		/// <exception cref="TypeNotTrackedException">
		/// Thrown if <typeparamref name="T"/> is not a tracked type.
		/// </exception>
		/// <remarks>
		/// Near-identical in behavior to <see cref="Tracker.GetEntitiesCopy{T}"/> but with a fix for the return
		/// type; it's now a list of <typeparamref name="T"/> and not of <see cref="Entity"/>. The original
		/// method is also capable of returning a value that isn't actually of type <typeparamref name="T"/>
		/// in some edge cases with <see cref="TrackedAsAttribute"/>, while this method throws instead.
		/// </remarks>
		public List<T> GetEntitiesCopyExt<T>() where T : Entity {
			if (!tr.Entities.TryGetValue(typeof(T), out List<Entity>? l))
				throw new TypeNotTrackedException(typeof(T));
			return new List<T>(l.Cast<T>());
		}

		/// <summary>
		/// Gets the first found tracked component of type <typeparamref name="T"/> known by the tracker.
		/// </summary>
		/// <exception cref="TypeNotTrackedException">
		/// Thrown if <typeparamref name="T"/> is not a tracked type.
		/// </exception>
		/// <exception cref="TrackedComponentNotFoundException">
		/// Thrown if no tracked components of type <typeparamref name="T"/> are present in the scene.
		/// </exception>
		/// <remarks>
		/// <para>
		/// If multiple are present, the one that got added to the scene first is returned; this is an
		/// unreliable metric, so if you expect multiple to be normally present, use
		/// <see cref="GetComponentsExt{T}(Tracker)"/> and filter out the desired one manually.
		/// </para>
		/// <para>
		/// Near-identical in behavior to <see cref="Tracker.GetComponent{T}"/>; the primary difference is that
		/// the original method silently returns <see langword="null"/> if no component was found or if the
		/// component wasn't actually of type <typeparamref name="T"/> (which is very rare, but possible with
		/// <see cref="TrackedAsAttribute"/> edge cases). This method throws on both such cases.
		/// </para>
		/// </remarks>
		public T GetComponentExt<T>() where T : Component {
			if (!tr.Components.TryGetValue(typeof(T), out List<Component>? l))
				throw new TypeNotTrackedException(typeof(T));
			return l.Count == 0 ? throw new TrackedComponentNotFoundException(typeof(T)) : (T)l[0];
		}

		/// <summary>
		/// Gets the first found tracked component of type <typeparamref name="T"/> known by the tracker,
		/// or <see langword="null"/> if no tracked components of that type are present in the scene.
		/// </summary>
		/// <exception cref="TypeNotTrackedException">
		/// Thrown if <typeparamref name="T"/> is not a tracked type.
		/// </exception>
		/// <remarks>
		/// <para>
		/// If multiple are present, the one that got added to the scene first is returned; this is an
		/// unreliable metric, so if you expect multiple to be normally present, use
		/// <see cref="GetComponentsExt{T}(Tracker)"/> and filter out the desired one manually.
		/// </para>
		/// <para>
		/// Near-identical in behavior to <see cref="Tracker.GetComponent{T}"/>; the primary difference is that
		/// the original method silently returns <see langword="null"/> if no component was found or if the
		/// component wasn't actually of type <typeparamref name="T"/> (which is very rare, but possible with
		/// <see cref="TrackedAsAttribute"/> edge cases). This method throws on such a type mismatch; it
		/// still has the same null-on-not-found behavior, but is nullable-aware unlike the original method.
		/// </para>
		/// </remarks>
		public T? GetComponentOrDefaultExt<T>() where T : Component {
			if (!tr.Components.TryGetValue(typeof(T), out List<Component>? l))
				throw new TypeNotTrackedException(typeof(T));
			return l.Count == 0 ? null : (T)l[0];
		}

		/// <summary>
		/// Gets the component of type <typeparamref name="T"/> whose parent entity's distance to the point
		/// <paramref name="nearestTo"/> is shorter than that of any other parent entities of other tracked
		/// components of type <typeparamref name="T"/>.
		/// </summary>
		/// <param name="nearestTo">
		/// The distance of every candidate component's parent entity <see cref="Entity.Position"/> to this
		/// point will be measured, and the one with the smallest distance will be returned.
		/// </param>
		/// <exception cref="TypeNotTrackedException">
		/// Thrown if <typeparamref name="T"/> is not a tracked type.
		/// </exception>
		/// <exception cref="TrackedComponentNotFoundException">
		/// Thrown if no tracked components of type <typeparamref name="T"/> are present in the scene.
		/// </exception>
		/// <remarks>
		/// Near-identical in behavior to <see cref="Tracker.GetNearestComponent{T}(Vector2)"/>; the primary
		/// difference is that the original method silently returns <see langword="null"/> if no component
		/// of that type was found, while this method throws.
		/// </remarks>
		public T GetNearestComponentExt<T>(Vector2 nearestTo) where T : Component => tr.GetNearestComponentOrDefaultExt<T>(nearestTo) ??
			throw new TrackedComponentNotFoundException(typeof(T));

		/// <summary>
		/// Gets the component of type <typeparamref name="T"/> whose parent entity's distance to the point
		/// <paramref name="nearestTo"/> is shorter than that of any other parent entities of other tracked
		/// components of type <typeparamref name="T"/>, or <see langword="null"/> if no tracked components of
		/// that type are present in the scene.
		/// </summary>
		/// <param name="nearestTo">
		/// The distance of every candidate component's parent entity <see cref="Entity.Position"/> to this
		/// point will be measured, and the one with the smallest distance will be returned.
		/// </param>
		/// <exception cref="TypeNotTrackedException">
		/// Thrown if <typeparamref name="T"/> is not a tracked type.
		/// </exception>
		/// <remarks>
		/// Near-identical in behavior to <see cref="Tracker.GetNearestComponent{T}(Vector2)"/>; the primary
		/// differences are that of all <c>*Ext</c> tracker methods, those being better internal type safety,
		/// <see cref="TypeNotTrackedException"/>, and nullable awareness.
		/// </remarks>
		public T? GetNearestComponentOrDefaultExt<T>(Vector2 nearestTo) where T : Component {
			IReadOnlyList<T> comps = tr.GetComponentsExt<T>();
			float match = float.PositiveInfinity;
			T? matched = null;
			foreach (T comp in comps) {
				float dsq = Vector2.DistanceSquared(nearestTo, comp.Entity.Position);
				if (dsq < match) {
					match = dsq;
					matched = comp;
				}
			}
			return matched;
		}

		/// <summary>
		/// Gets every tracked component of type <typeparamref name="T"/> known by the tracker.
		/// </summary>
		/// <exception cref="TypeNotTrackedException">
		/// Thrown if <typeparamref name="T"/> is not a tracked type.
		/// </exception>
		/// <remarks>
		/// <para>
		/// Near-identical in behavior to <see cref="Tracker.GetComponents{T}"/> but with a fix for the return
		/// type; it's now a list of <typeparamref name="T"/> and not of <see cref="Component"/>. The original
		/// method is also capable of returning a value that isn't actually of type <typeparamref name="T"/>
		/// in some edge cases with <see cref="TrackedAsAttribute"/>, while this method throws instead.
		/// </para>
		/// <para>
		/// The returned list is a live view of internal state; if you want to mutate it or retain it,
		/// you should copy it out instead, either using <see cref="GetComponentsCopyExt{T}(Tracker)"/> or
		/// plain old <c>.ToArray()</c>. If you're wondering, yeah, the original
		/// <see cref="Tracker.GetComponents{T}"/> has this same behavior, it's just undocumented for some reason.
		/// </para>
		/// </remarks>
		public IReadOnlyList<T> GetComponentsExt<T>() where T : Component {
			if (!tr.Components.TryGetValue(typeof(T), out List<Component>? l))
				throw new TypeNotTrackedException(typeof(T));
			return new CastView<Component, T>(l);
		}

		/// <summary>
		/// Gets every tracked component of type <typeparamref name="T"/> known by the tracker, returning a
		/// copy of the list.
		/// </summary>
		/// <exception cref="TypeNotTrackedException">
		/// Thrown if <typeparamref name="T"/> is not a tracked type.
		/// </exception>
		/// <remarks>
		/// Near-identical in behavior to <see cref="Tracker.GetComponentsCopy{T}"/> but with a fix for the return
		/// type; it's now a list of <typeparamref name="T"/> and not of <see cref="Component"/>. The original
		/// method is also capable of returning a value that isn't actually of type <typeparamref name="T"/>
		/// in some edge cases with <see cref="TrackedAsAttribute"/>, while this method throws instead.
		/// </remarks>
		public List<T> GetComponentsCopyExt<T>() where T : Component {
			if (!tr.Components.TryGetValue(typeof(T), out List<Component>? l))
				throw new TypeNotTrackedException(typeof(T));
			return new List<T>(l.Cast<T>());
		}

		/// <summary>
		/// Gets every entity of type <typeparamref name="T"/> known by the tracker, adding it to the
		/// tracker beforehand if needed.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Near-identical in behavior to <see cref="Tracker.GetEntitiesTrackIfNeeded{T}"/> but with a fix for
		/// the return type; it's now a list of <typeparamref name="T"/> and not of <see cref="Entity"/>. The original
		/// method is also capable of returning a value that isn't actually of type <typeparamref name="T"/>
		/// in some edge cases with <see cref="TrackedAsAttribute"/>, while this method throws instead.
		/// </para>
		/// <para>
		/// The returned list is a live view of internal state; if you want to mutate it or retain it,
		/// you should copy it out instead, either using <see cref="GetEntitiesTrackIfNeededCopyExt{T}(Tracker)"/> or
		/// plain old <c>.ToArray()</c>. If you're wondering, yeah, the original
		/// <see cref="Tracker.GetEntitiesTrackIfNeeded{T}()"/> has this same behavior, it's just
		/// undocumented for some reason.
		/// </para>
		/// </remarks>
		public IReadOnlyList<T> GetEntitiesTrackIfNeededExt<T>() where T : Entity => new CastView<Entity, T>(tr.GetEntitiesTrackIfNeeded(typeof(T)));

		/// <summary>
		/// Gets every entity of type <typeparamref name="T"/> known by the tracker, adding it to the
		/// tracker beforehand if needed, and returns a copy of the list.
		/// </summary>
		/// <remarks>
		/// Near-identical in behavior to <see cref="Tracker.GetEntitiesTrackIfNeeded{T}"/> except for the fact
		/// that it makes a copy of the returned list, and that it has a fix for the return type; it's now
		/// a list of <typeparamref name="T"/> and not of <see cref="Entity"/>. The original method is also capable
		/// of returning a value that isn't actually of type <typeparamref name="T"/> in some edge cases with
		/// <see cref="TrackedAsAttribute"/>, while this method throws instead.
		/// </remarks>
		public List<T> GetEntitiesTrackIfNeededCopyExt<T>() where T : Entity => new(tr.GetEntitiesTrackIfNeeded(typeof(T)).Cast<T>());

		/// <summary>
		/// Gets every component of type <typeparamref name="T"/> known by the tracker, adding it to the
		/// tracker beforehand if needed.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Near-identical in behavior to <see cref="Tracker.GetComponentsTrackIfNeeded{T}"/> but with a fix for
		/// the return type; it's now a list of <typeparamref name="T"/> and not of <see cref="Component"/>. The original
		/// method is also capable of returning a value that isn't actually of type <typeparamref name="T"/>
		/// in some edge cases with <see cref="TrackedAsAttribute"/>, while this method throws instead.
		/// </para>
		/// <para>
		/// The returned list is a live view of internal state; if you want to mutate it or retain it,
		/// you should copy it out instead, either using <see cref="GetComponentsTrackIfNeededCopyExt{T}(Tracker)"/> or
		/// plain old <c>.ToArray()</c>. If you're wondering, yeah, the original
		/// <see cref="Tracker.GetComponentsTrackIfNeeded{T}()"/> has this same behavior, it's just
		/// undocumented for some reason.
		/// </para>
		/// </remarks>
		public IReadOnlyList<T> GetComponentsTrackIfNeededExt<T>() where T : Component {
			try {
				return new CastView<Component, T>(tr.GetComponentsTrackIfNeeded(typeof(T)));
			} catch (UnreachableException ex) when (ex.Message.Contains("failed for an unknown reason!")) {
				// everest misuses UnreachableException here to mean "unexpected internal condition" and not
				// "provably unreachable control flow state"
				// it would be nicer to somehow change just the exception type while retaining the throw
				// origin, but eh
				throw new InvalidOperationException($"Everest failed to track type '{typeof(T)}' for an unknown reason");
			}
		}

		/// <summary>
		/// Gets every component of type <typeparamref name="T"/> known by the tracker, adding it to the
		/// tracker beforehand if needed, and returns a copy of the list.
		/// </summary>
		/// <remarks>
		/// Identical in behavior to <see cref="Tracker.GetComponentsTrackIfNeeded{T}"/> except for the fact
		/// that it makes a copy of the returned list, and that it has a fix for the return type; it's now
		/// a list of <typeparamref name="T"/> and not of <see cref="Component"/>. The original method is also capable
		/// of returning a value that isn't actually of type <typeparamref name="T"/> in some edge cases with
		/// <see cref="TrackedAsAttribute"/>, while this method throws instead.
		/// </remarks>
		public List<T> GetComponentsTrackIfNeededCopyExt<T>() where T : Component {
			try {
				return new List<T>(tr.GetComponentsTrackIfNeeded(typeof(T)).Cast<T>());
			} catch (UnreachableException ex) when (ex.Message.Contains("failed for an unknown reason!")) {
				// everest misuses UnreachableException here to mean "unexpected internal condition" and not
				// "provably unreachable control flow state"
				// it would be nicer to somehow change just the exception type while retaining the throw
				// origin, but eh
				throw new InvalidOperationException($"Everest failed to track type '{typeof(T)}' for an unknown reason");
			}
		}
	}
}
