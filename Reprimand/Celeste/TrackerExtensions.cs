// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Monocle;

namespace Reprimand.Celeste;

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
		/// Gets every tracked entity of type <typeparamref name="T"/> known by the tracker.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Identical in behavior to <see cref="Tracker.GetEntities{T}"/> but with a fix for the return
		/// type; it's now a list of <typeparamref name="T"/> and not of <see cref="Entity"/>.
		/// </para>
		/// <para>
		/// The returned list is a live view of internal state; if you want to mutate it or retain it,
		/// you should copy it out instead, either using <see cref="GetComponentsTypedCopy{T}(Tracker)"/> or
		/// plain old <c>.ToArray()</c>. If you're wondering, yeah, the original
		/// <see cref="Tracker.GetEntities{T}"/> has this same behavior, it's just undocumented for some reason.
		/// </para>
		/// </remarks>
		public IReadOnlyList<T> GetEntitiesTyped<T>() where T : Entity => new CastView<Entity, T>(tr.Entities[typeof(T)]);

		/// <summary>
		/// Gets every tracked entity of type <typeparamref name="T"/> known by the tracker, returning a
		/// copy of the list.
		/// </summary>
		/// <remarks>
		/// Identical in behavior to <see cref="Tracker.GetEntitiesCopy{T}"/> but with a fix for the return
		/// type; it's now a list of <typeparamref name="T"/> and not of <see cref="Entity"/>.
		/// </remarks>
		public List<T> GetEntitiesTypedCopy<T>() where T : Entity => new(tr.Entities[typeof(T)].Cast<T>());

		/// <summary>
		/// Gets every tracked component of type <typeparamref name="T"/> known by the tracker.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Identical in behavior to <see cref="Tracker.GetComponents{T}"/> but with a fix for the return
		/// type; it's now a list of <typeparamref name="T"/> and not of <see cref="Component"/>.
		/// </para>
		/// <para>
		/// The returned list is a live view of internal state; if you want to mutate it or retain it,
		/// you should copy it out instead, either using <see cref="GetComponentsTypedCopy{T}(Tracker)"/> or
		/// plain old <c>.ToArray()</c>. If you're wondering, yeah, the original
		/// <see cref="Tracker.GetComponents{T}"/> has this same behavior, it's just undocumented for some reason.
		/// </para>
		/// </remarks>
		public IReadOnlyList<T> GetComponentsTyped<T>() where T : Component => new CastView<Component, T>(tr.Components[typeof(T)]);

		/// <summary>
		/// Gets every tracked component of type <typeparamref name="T"/> known by the tracker, returning a
		/// copy of the list.
		/// </summary>
		/// <remarks>
		/// Identical in behavior to <see cref="Tracker.GetComponentsCopy{T}"/> but with a fix for the return
		/// type; it's now a list of <typeparamref name="T"/> and not of <see cref="Component"/>.
		/// </remarks>
		public List<T> GetComponentsTypedCopy<T>() where T : Component => new(tr.Components[typeof(T)].Cast<T>());

		/// <summary>
		/// Gets every entity of type <typeparamref name="T"/> known by the tracker, adding it to the
		/// tracker beforehand if needed.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Identical in behavior to <see cref="Tracker.GetEntitiesTrackIfNeeded{T}"/> but with a fix for
		/// the return type; it's now a list of <typeparamref name="T"/> and not of <see cref="Entity"/>.
		/// </para>
		/// <para>
		/// The returned list is a live view of internal state; if you want to mutate it or retain it,
		/// you should copy it out instead, either using <see cref="GetEntitiesTrackIfNeededTypedCopy{T}(Tracker)"/> or
		/// plain old <c>.ToArray()</c>. If you're wondering, yeah, the original
		/// <see cref="Tracker.GetEntitiesTrackIfNeeded{T}()"/> has this same behavior, it's just
		/// undocumented for some reason.
		/// </para>
		/// </remarks>
		public IReadOnlyList<T> GetEntitiesTrackIfNeededTyped<T>() where T : Entity => new CastView<Entity, T>(tr.GetEntitiesTrackIfNeeded(typeof(T)));

		/// <summary>
		/// Gets every entity of type <typeparamref name="T"/> known by the tracker, adding it to the
		/// tracker beforehand if needed, and returns a copy of the list.
		/// </summary>
		/// <remarks>
		/// Identical in behavior to <see cref="Tracker.GetEntitiesTrackIfNeeded{T}"/> except for the fact
		/// that it makes a copy of the returned list, and that it has a fix for the return type; it's now
		/// a list of <typeparamref name="T"/> and not of <see cref="Entity"/>.
		/// </remarks>
		public List<T> GetEntitiesTrackIfNeededTypedCopy<T>() where T : Entity => new(tr.GetEntitiesTrackIfNeeded(typeof(T)).Cast<T>());

		/// <summary>
		/// Gets every component of type <typeparamref name="T"/> known by the tracker, adding it to the
		/// tracker beforehand if needed.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Identical in behavior to <see cref="Tracker.GetComponentsTrackIfNeeded{T}"/> but with a fix for
		/// the return type; it's now a list of <typeparamref name="T"/> and not of <see cref="Component"/>.
		/// </para>
		/// <para>
		/// The returned list is a live view of internal state; if you want to mutate it or retain it,
		/// you should copy it out instead, either using <see cref="GetComponentsTrackIfNeededTypedCopy{T}(Tracker)"/> or
		/// plain old <c>.ToArray()</c>. If you're wondering, yeah, the original
		/// <see cref="Tracker.GetComponentsTrackIfNeeded{T}()"/> has this same behavior, it's just
		/// undocumented for some reason.
		/// </para>
		/// </remarks>
		public IReadOnlyList<T> GetComponentsTrackIfNeededTyped<T>() where T : Component => new CastView<Component, T>(tr.GetComponentsTrackIfNeeded(typeof(T)));

		/// <summary>
		/// Gets every component of type <typeparamref name="T"/> known by the tracker, adding it to the
		/// tracker beforehand if needed, and returns a copy of the list.
		/// </summary>
		/// <remarks>
		/// Identical in behavior to <see cref="Tracker.GetComponentsTrackIfNeeded{T}"/> except for the fact
		/// that it makes a copy of the returned list, and that it has a fix for the return type; it's now
		/// a list of <typeparamref name="T"/> and not of <see cref="Component"/>.
		/// </remarks>
		public List<T> GetComponentsTrackIfNeededTypedCopy<T>() where T : Component => new(tr.GetComponentsTrackIfNeeded(typeof(T)).Cast<T>());
	}
}
