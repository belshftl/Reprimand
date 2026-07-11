// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Reprimand.MonoMod;

/// <summary>
/// Provides utility/convenience extensions for <see cref="ILCursor"/>.
/// </summary>
public static class ILCursorExtensions {
	extension(ILCursor c) {
		/// <summary>
		/// Searches forward and moves the cursor to the next sequence of instructions ahead
		/// of the cursor that are matched by the corresponding predicates, or throws if the
		/// sequence is not found.
		/// </summary>
		/// <param name="expected">
		/// Short, human-readable string describing the instruction(s) being searched for.
		/// </param>
		/// <param name="moveType">
		/// Where the cursor is to be moved relative to the matched instruction(s).
		/// </param>
		/// <param name="predicates">
		/// Predicates used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <exception cref="ILPatternNotFoundException">
		/// Thrown if the expected sequence of instructions was not found.
		/// </exception>
		public void RequireGotoNext(string expected, MoveType moveType = MoveType.Before, params Func<Instruction, bool>[] predicates) {
			int start = c.Index;
			if (c.TryGotoNext(moveType, predicates))
				return;
			string target = c.Context.Method.FullName ?? c.Context.Method.Name ?? "<name unavailable>";
			throw new ILPatternNotFoundException(target, expected, start, ILPatternSearchDirection.Forward);
		}

		/// <summary>
		/// Searches forward and moves the cursor before the next sequence of instructions ahead
		/// of the cursor that are matched by the corresponding predicates, or throws if the
		/// sequence is not found.
		/// </summary>
		/// <inheritdoc cref="RequireGotoNext(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		public void RequireGotoNext(string expected, params Func<Instruction, bool>[] predicates) =>
			c.RequireGotoNext(expected, moveType: MoveType.Before, predicates);

		/// <summary>
		/// Searches forward and moves the cursor to the next sequence of instructions behind
		/// the cursor that are matched by the corresponding predicates, or throws if the
		/// sequence is not found.
		/// </summary>
		/// <inheritdoc cref="RequireGotoNext(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		public void RequireGotoPrev(string expected, MoveType moveType = MoveType.Before, params Func<Instruction, bool>[] predicates) {
			int start = c.Index;
			if (c.TryGotoPrev(moveType, predicates))
				return;
			string target = c.Context.Method.FullName ?? c.Context.Method.Name ?? "<name unavailable>";
			throw new ILPatternNotFoundException(target, expected, start, ILPatternSearchDirection.Backward);
		}

		/// <summary>
		/// Searches forward and moves the cursor before the next sequence of instructions behind
		/// the cursor that are matched by the corresponding predicates, or throws if the
		/// sequence is not found.
		/// </summary>
		/// <inheritdoc cref="RequireGotoNext(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		public void RequireGotoPrev(string expected, params Func<Instruction, bool>[] predicates) =>
			c.RequireGotoPrev(expected, moveType: MoveType.Before, predicates);

		// ==============================================================================================
		// convenience overload hell

#pragma warning disable IDE0079 // remove unnecessary suppression
#pragma warning disable CS1573 // parameter has no matching <param> tag
#pragma warning disable CS8620 // argument cannot be used due to differences in the nullability of reference types (i genuinely don't know why this sometimes gets reported, i think it's a false positive)
		/// <inheritdoc cref="RequireGotoNext(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		/// <param name="predicate0">
		/// First predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <remarks>
		/// Convenience overload for <see cref="RequireGotoNext(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		/// that auto-generates the description string from the caller argument expression.
		/// </remarks>
		public void RequireGotoNext(
			MoveType moveType,
			Func<Instruction, bool> predicate0,
			[CallerArgumentExpression(nameof(predicate0))]
			string expr0 = "<unknown>"
		) => c.RequireGotoNext(expr0, moveType, predicate0);

		/// <inheritdoc cref="RequireGotoNext(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		/// <param name="predicate0">
		/// First predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate1">
		/// Second predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <remarks>
		/// Convenience overload for <see cref="RequireGotoNext(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		/// that auto-generates the description string from the caller argument expressions.
		/// </remarks>
		public void RequireGotoNext(
			MoveType moveType,
			Func<Instruction, bool> predicate0,
			Func<Instruction, bool> predicate1,
			[CallerArgumentExpression(nameof(predicate0))]
			string expr0 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate1))]
			string expr1 = "<unknown>"
		) => c.RequireGotoNext($"{expr0}, {expr1}", moveType, predicate0, predicate1);

		/// <inheritdoc cref="RequireGotoNext(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		/// <param name="predicate0">
		/// First predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate1">
		/// Second predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate2">
		/// Third predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <remarks>
		/// Convenience overload for <see cref="RequireGotoNext(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		/// that auto-generates the description string from the caller argument expressions.
		/// </remarks>
		public void RequireGotoNext(
			MoveType moveType,
			Func<Instruction, bool> predicate0,
			Func<Instruction, bool> predicate1,
			Func<Instruction, bool> predicate2,
			[CallerArgumentExpression(nameof(predicate0))]
			string expr0 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate1))]
			string expr1 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate2))]
			string expr2 = "<unknown>"
		) => c.RequireGotoNext($"{expr0}, {expr1}, {expr2}", moveType, predicate0, predicate1, predicate2);

		/// <inheritdoc cref="RequireGotoNext(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		/// <param name="predicate0">
		/// First predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate1">
		/// Second predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate2">
		/// Third predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate3">
		/// Fourth predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <remarks>
		/// Convenience overload for <see cref="RequireGotoNext(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		/// that auto-generates the description string from the caller argument expressions.
		/// </remarks>
		public void RequireGotoNext(
			MoveType moveType,
			Func<Instruction, bool> predicate0,
			Func<Instruction, bool> predicate1,
			Func<Instruction, bool> predicate2,
			Func<Instruction, bool> predicate3,
			[CallerArgumentExpression(nameof(predicate0))]
			string expr0 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate1))]
			string expr1 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate2))]
			string expr2 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate2))]
			string expr3 = "<unknown>"
		) => c.RequireGotoNext($"{expr0}, {expr1}, {expr2}, {expr3}", moveType, predicate0, predicate1, predicate2, predicate3);

		/// <inheritdoc cref="RequireGotoNext(ILCursor, string, Func{Instruction, bool}[])"/>
		/// <param name="predicate0">
		/// First predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <remarks>
		/// Convenience overload for <see cref="RequireGotoNext(ILCursor, string, Func{Instruction, bool}[])"/>
		/// that auto-generates the description string from the caller argument expression.
		/// </remarks>
		public void RequireGotoNext(
			Func<Instruction, bool> predicate0,
			[CallerArgumentExpression(nameof(predicate0))]
			string expr0 = "<unknown>"
		) => c.RequireGotoNext(expr0, predicate0);

		/// <inheritdoc cref="RequireGotoNext(ILCursor, string, Func{Instruction, bool}[])"/>
		/// <param name="predicate0">
		/// First predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate1">
		/// Second predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <remarks>
		/// Convenience overload for <see cref="RequireGotoNext(ILCursor, string, Func{Instruction, bool}[])"/>
		/// that auto-generates the description string from the caller argument expressions.
		/// </remarks>
		public void RequireGotoNext(
			Func<Instruction, bool> predicate0,
			Func<Instruction, bool> predicate1,
			[CallerArgumentExpression(nameof(predicate0))]
			string expr0 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate1))]
			string expr1 = "<unknown>"
		) => c.RequireGotoNext($"{expr0}, {expr1}", predicate0, predicate1);

		/// <inheritdoc cref="RequireGotoNext(ILCursor, string, Func{Instruction, bool}[])"/>
		/// <param name="predicate0">
		/// First predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate1">
		/// Second predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate2">
		/// Third predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <remarks>
		/// Convenience overload for <see cref="RequireGotoNext(ILCursor, string, Func{Instruction, bool}[])"/>
		/// that auto-generates the description string from the caller argument expressions.
		/// </remarks>
		public void RequireGotoNext(
			Func<Instruction, bool> predicate0,
			Func<Instruction, bool> predicate1,
			Func<Instruction, bool> predicate2,
			[CallerArgumentExpression(nameof(predicate0))]
			string expr0 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate1))]
			string expr1 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate2))]
			string expr2 = "<unknown>"
		) => c.RequireGotoNext($"{expr0}, {expr1}, {expr2}", predicate0, predicate1, predicate2);

		/// <inheritdoc cref="RequireGotoNext(ILCursor, string, Func{Instruction, bool}[])"/>
		/// <param name="predicate0">
		/// First predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate1">
		/// Second predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate2">
		/// Third predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate3">
		/// Fourth predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <remarks>
		/// Convenience overload for <see cref="RequireGotoNext(ILCursor, string, Func{Instruction, bool}[])"/>
		/// that auto-generates the description string from the caller argument expressions.
		/// </remarks>
		public void RequireGotoNext(
			Func<Instruction, bool> predicate0,
			Func<Instruction, bool> predicate1,
			Func<Instruction, bool> predicate2,
			Func<Instruction, bool> predicate3,
			[CallerArgumentExpression(nameof(predicate0))]
			string expr0 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate1))]
			string expr1 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate2))]
			string expr2 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate2))]
			string expr3 = "<unknown>"
		) => c.RequireGotoNext($"{expr0}, {expr1}, {expr2}, {expr3}", predicate0, predicate1, predicate2, predicate3);

		/// <inheritdoc cref="RequireGotoPrev(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		/// <param name="predicate0">
		/// First predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <remarks>
		/// Convenience overload for <see cref="RequireGotoPrev(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		/// that auto-generates the description string from the caller argument expression.
		/// </remarks>
		public void RequireGotoPrev(
			MoveType moveType,
			Func<Instruction, bool> predicate0,
			[CallerArgumentExpression(nameof(predicate0))]
			string expr0 = "<unknown>"
		) => c.RequireGotoPrev(expr0, moveType, predicate0);

		/// <inheritdoc cref="RequireGotoPrev(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		/// <param name="predicate0">
		/// First predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate1">
		/// Second predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <remarks>
		/// Convenience overload for <see cref="RequireGotoPrev(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		/// that auto-generates the description string from the caller argument expressions.
		/// </remarks>
		public void RequireGotoPrev(
			MoveType moveType,
			Func<Instruction, bool> predicate0,
			Func<Instruction, bool> predicate1,
			[CallerArgumentExpression(nameof(predicate0))]
			string expr0 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate1))]
			string expr1 = "<unknown>"
		) => c.RequireGotoPrev($"{expr0}, {expr1}", moveType, predicate0, predicate1);

		/// <inheritdoc cref="RequireGotoPrev(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		/// <param name="predicate0">
		/// First predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate1">
		/// Second predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate2">
		/// Third predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <remarks>
		/// Convenience overload for <see cref="RequireGotoPrev(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		/// that auto-generates the description string from the caller argument expressions.
		/// </remarks>
		public void RequireGotoPrev(
			MoveType moveType,
			Func<Instruction, bool> predicate0,
			Func<Instruction, bool> predicate1,
			Func<Instruction, bool> predicate2,
			[CallerArgumentExpression(nameof(predicate0))]
			string expr0 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate1))]
			string expr1 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate2))]
			string expr2 = "<unknown>"
		) => c.RequireGotoPrev($"{expr0}, {expr1}, {expr2}", moveType, predicate0, predicate1, predicate2);

		/// <inheritdoc cref="RequireGotoPrev(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		/// <param name="predicate0">
		/// First predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate1">
		/// Second predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate2">
		/// Third predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate3">
		/// Fourth predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <remarks>
		/// Convenience overload for <see cref="RequireGotoPrev(ILCursor, string, MoveType, Func{Instruction, bool}[])"/>
		/// that auto-generates the description string from the caller argument expressions.
		/// </remarks>
		public void RequireGotoPrev(
			MoveType moveType,
			Func<Instruction, bool> predicate0,
			Func<Instruction, bool> predicate1,
			Func<Instruction, bool> predicate2,
			Func<Instruction, bool> predicate3,
			[CallerArgumentExpression(nameof(predicate0))]
			string expr0 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate1))]
			string expr1 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate2))]
			string expr2 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate2))]
			string expr3 = "<unknown>"
		) => c.RequireGotoPrev($"{expr0}, {expr1}, {expr2}, {expr3}", moveType, predicate0, predicate1, predicate2, predicate3);

		/// <inheritdoc cref="RequireGotoPrev(ILCursor, string, Func{Instruction, bool}[])"/>
		/// <param name="predicate0">
		/// First predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <remarks>
		/// Convenience overload for <see cref="RequireGotoPrev(ILCursor, string, Func{Instruction, bool}[])"/>
		/// that auto-generates the description string from the caller argument expression.
		/// </remarks>
		public void RequireGotoPrev(
			Func<Instruction, bool> predicate0,
			[CallerArgumentExpression(nameof(predicate0))]
			string expr0 = "<unknown>"
		) => c.RequireGotoPrev(expr0, predicate0);

		/// <inheritdoc cref="RequireGotoPrev(ILCursor, string, Func{Instruction, bool}[])"/>
		/// <param name="predicate0">
		/// First predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate1">
		/// Second predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <remarks>
		/// Convenience overload for <see cref="RequireGotoPrev(ILCursor, string, Func{Instruction, bool}[])"/>
		/// that auto-generates the description string from the caller argument expressions.
		/// </remarks>
		public void RequireGotoPrev(
			Func<Instruction, bool> predicate0,
			Func<Instruction, bool> predicate1,
			[CallerArgumentExpression(nameof(predicate0))]
			string expr0 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate1))]
			string expr1 = "<unknown>"
		) => c.RequireGotoPrev($"{expr0}, {expr1}", predicate0, predicate1);

		/// <inheritdoc cref="RequireGotoPrev(ILCursor, string, Func{Instruction, bool}[])"/>
		/// <param name="predicate0">
		/// First predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate1">
		/// Second predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate2">
		/// Third predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <remarks>
		/// Convenience overload for <see cref="RequireGotoPrev(ILCursor, string, Func{Instruction, bool}[])"/>
		/// that auto-generates the description string from the caller argument expressions.
		/// </remarks>
		public void RequireGotoPrev(
			Func<Instruction, bool> predicate0,
			Func<Instruction, bool> predicate1,
			Func<Instruction, bool> predicate2,
			[CallerArgumentExpression(nameof(predicate0))]
			string expr0 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate1))]
			string expr1 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate2))]
			string expr2 = "<unknown>"
		) => c.RequireGotoPrev($"{expr0}, {expr1}, {expr2}", predicate0, predicate1, predicate2);

		/// <inheritdoc cref="RequireGotoPrev(ILCursor, string, Func{Instruction, bool}[])"/>
		/// <param name="predicate0">
		/// First predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate1">
		/// Second predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate2">
		/// Third predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <param name="predicate3">
		/// Fourth predicate used for <see cref="ILCursor"/> instruction matching.
		/// </param>
		/// <remarks>
		/// Convenience overload for <see cref="RequireGotoPrev(ILCursor, string, Func{Instruction, bool}[])"/>
		/// that auto-generates the description string from the caller argument expressions.
		/// </remarks>
		public void RequireGotoPrev(
			Func<Instruction, bool> predicate0,
			Func<Instruction, bool> predicate1,
			Func<Instruction, bool> predicate2,
			Func<Instruction, bool> predicate3,
			[CallerArgumentExpression(nameof(predicate0))]
			string expr0 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate1))]
			string expr1 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate2))]
			string expr2 = "<unknown>",
			[CallerArgumentExpression(nameof(predicate2))]
			string expr3 = "<unknown>"
		) => c.RequireGotoPrev($"{expr0}, {expr1}, {expr2}, {expr3}", predicate0, predicate1, predicate2, predicate3);
#pragma warning restore CS8620 // argument cannot be used due to differences in the nullability of reference types (i genuinely don't know why this sometimes gets reported, i think it's a false positive)
#pragma warning restore CS1573 // parameter has no matching <param> tag
#pragma warning restore IDE0079 // remove unnecessary suppression
	}
}
