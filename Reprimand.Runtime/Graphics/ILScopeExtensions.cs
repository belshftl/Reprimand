// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: MIT

using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Reprimand.Runtime.Graphics;

/// <summary>
/// Helpers for wrapping existing IL ranges in backbuffer attachment behavior override scopes or
/// spritebatch suspension scopes.
/// </summary>
public static class ILScopeExtensions {
	private enum RegionRelation {
		Disjoint,
		Contained,
		Partial,
	}

	extension(ILCursor c) {
		/// <summary>
		/// Wraps an instruction range in a backbuffer attachment behavior override and moves the cursor
		/// past the newly emitted instructions.
		/// </summary>
		/// <param name="start">
		/// The first instruction executed with the override active.
		/// </param>
		/// <param name="endExclusive">
		/// The first instruction executed after the override.
		/// </param>
		/// <param name="behavior">
		/// The backbuffer attachment behavior.
		/// </param>
		/// <returns>
		/// The same <see cref="ILCursor"/> object that the call was performed on.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if either boundary is not in the manipulated method, or if the range is empty or reversed.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the range contains unsupported control flow, crosses an existing exception region, or
		/// otherwise cannot safely be wrapped.
		/// </exception>
		public ILCursor WrapInBackbufferAttachmentMode(Instruction start, Instruction endExclusive, BackbufferAttachBehavior behavior) {
			ArgumentNullException.ThrowIfNull(c);
			ArgumentNullException.ThrowIfNull(start);
			ArgumentNullException.ThrowIfNull(endExclusive);
			Instruction continuation = wrap<BackbufferAttachment.ILOverrideCookie>(
				c.Context,
				start,
				endExclusive,
				typeof(BackbufferAttachment).GetMethod(
					nameof(BackbufferAttachment.EnterOverrideForIL),
					BindingFlags.Static | BindingFlags.Public,
					binder: null,
					types: [
						typeof(BackbufferAttachBehavior),
					],
					modifiers: null
				) ?? throw new InternalStateException($"expected to find {nameof(BackbufferAttachment)}.{nameof(BackbufferAttachment.EnterOverrideForIL)}"),
				typeof(BackbufferAttachment).GetMethod(
					nameof(BackbufferAttachment.ExitOverrideForIL),
					BindingFlags.Static | BindingFlags.Public,
					binder: null,
					types: [
						typeof(BackbufferAttachment.ILOverrideCookie),
					],
					modifiers: null
				) ?? throw new InternalStateException($"expected to find {nameof(BackbufferAttachment)}.{nameof(BackbufferAttachment.ExitOverrideForIL)}"),
				c2 => c2.EmitLdcI4((int)behavior)
			);
			return c.Goto(continuation, MoveType.After);
		}

		/// <summary>
		/// Wraps an instruction range in a spritebatch suspension and moves the cursor past the newly
		/// emitted instructions.
		/// </summary>
		/// <param name="start">
		/// The first instruction executed inside the suspension.
		/// </param>
		/// <param name="endExclusive">
		/// The first instruction executed after the suspension.
		/// </param>
		/// <returns>
		/// The same <see cref="ILCursor"/> object that the call was performed on.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if either boundary is not in the manipulated method, or if the range is empty or reversed.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the range contains unsupported control flow, crosses an existing exception region, or
		/// otherwise cannot safely be wrapped.
		/// </exception>
		public ILCursor WrapInSpriteBatchSuspension(Instruction start, Instruction endExclusive) {
			ArgumentNullException.ThrowIfNull(c);
			ArgumentNullException.ThrowIfNull(start);
			ArgumentNullException.ThrowIfNull(endExclusive);
			Instruction continuation = wrap<GlobalSpriteBatch.ILSuspensionCookie>(
				c.Context,
				start,
				endExclusive,
				typeof(GlobalSpriteBatch).GetMethod(
					nameof(GlobalSpriteBatch.EnterSuspensionForIL),
					BindingFlags.Static | BindingFlags.Public,
					binder: null,
					types: [],
					modifiers: null
				) ?? throw new InternalStateException($"expected to find {nameof(GlobalSpriteBatch)}.{nameof(GlobalSpriteBatch.EnterSuspensionForIL)}"),
				typeof(GlobalSpriteBatch).GetMethod(
					nameof(GlobalSpriteBatch.ExitSuspensionForIL),
					BindingFlags.Static | BindingFlags.Public,
					binder: null,
					types: [
						typeof(GlobalSpriteBatch.ILSuspensionCookie),
					],
					modifiers: null
				) ?? throw new InternalStateException($"expected to find {nameof(GlobalSpriteBatch)}.{nameof(GlobalSpriteBatch.ExitSuspensionForIL)}"),
				null
			);
			return c.Goto(continuation, MoveType.After);
		}
	}

	private static Instruction wrap<TCookie>(
		ILContext il,
		Instruction start,
		Instruction endExclusive,
		MethodInfo enterMethod,
		MethodInfo exitMethod,
		Action<ILCursor>? emitEnterArguments
	) where TCookie : struct {
		List<ExceptionHandler> containedHandlers = validateRange(il, start, endExclusive);

		// apparently the version of MonoMod everest uses doesn't have ILContext.CreateLocal<T>(),
		// so it resolves the symbol at compile time and then fails at runtime
		VariableDefinition cookieLocal = new(il.Method.Module.ImportReference(typeof(TCookie)));
		il.Body.Variables.Add(cookieLocal);

		ILCursor c = new(il);

		// acquire outside the try since if it throws there's nothing to end
		c.Goto(start, MoveType.Before);
		emitEnterArguments?.Invoke(c);
		c.EmitCall(enterMethod);
		c.EmitStloc(cookieLocal);

		// use a dedicated nop for the eh boundary / leave target because when i didn't it messed things up
		c.Goto(endExclusive, MoveType.Before);
		ILLabel continuationLabel = c.DefineLabel();
		c.EmitLeave(continuationLabel);
		Instruction outerLeave = c.Prev;
		retargetContainedRegionEnds(containedHandlers, endExclusive, outerLeave);
		c.EmitLdloc(cookieLocal);
		Instruction handlerStart = c.Prev;
		c.EmitCall(exitMethod);
		c.EmitEndfinally();
		c.MarkLabel(continuationLabel);
		c.EmitNop();
		Instruction continuation = c.Prev;

		ExceptionHandler handler = new(ExceptionHandlerType.Finally) {
			TryStart = start,
			TryEnd = handlerStart,
			HandlerStart = handlerStart,
			HandlerEnd = continuation,
		};
		il.Body.ExceptionHandlers.Add(handler);

		return continuation;
	}

	private static List<ExceptionHandler> validateRange(ILContext il, Instruction start, Instruction endExclusive) {
		int startIdx = il.Instrs.IndexOf(start);
		if (startIdx < 0)
			throw new ArgumentException("the start instruction is not in this method", nameof(start));
		int endIdx = il.Instrs.IndexOf(endExclusive);
		if (endIdx < 0)
			throw new ArgumentException("the end instruction is not in this method", nameof(endExclusive));

		if (startIdx == endIdx)
			throw new ArgumentException("wrapped range must be non-empty", nameof(endExclusive));
		else if (startIdx > endIdx)
			throw new ArgumentException("wrapped range must have the start before the end", nameof(endExclusive));

		if (start.Previous is {} prevInstr && prevInstr.OpCode.OpCodeType == OpCodeType.Prefix)
			throw new InvalidOperationException("wrapped range begins between an IL prefix and its associated instruction");
		Instruction lastInstruction = il.Instrs[endIdx - 1];
		if (lastInstruction.OpCode.OpCodeType == OpCodeType.Prefix)
			throw new InvalidOperationException("wrapped range ends between an IL prefix and its associated instruction");

		HashSet<Instruction> range = new();
		for (int index = startIdx; index < endIdx; ++index)
			range.Add(il.Instrs[index]);
		List<ExceptionHandler> containedHandlers = validateExistingExceptionRegions(il, start, startIdx, endIdx);
		validateNoUnsupportedInstructions(range);
		validateNoCrossBoundaryBranches(il, range);
		return containedHandlers;
	}

	private static List<ExceptionHandler> validateExistingExceptionRegions(ILContext il, Instruction start, int wrappedStartIdx, int wrappedEndIdx) {
		List<ExceptionHandler> containedHandlers = new();
		foreach (ExceptionHandler handler in il.Body.ExceptionHandlers) {
			if (ReferenceEquals(handler.TryEnd, start) || ReferenceEquals(handler.HandlerEnd, start))
				throw new InvalidOperationException("wrapped range begins at the end of an existing exception region");
			RegionRelation relation = classifyExceptionHandler(il, handler, wrappedStartIdx, wrappedEndIdx);
			switch (relation) {
			case RegionRelation.Disjoint:
				break;
			case RegionRelation.Contained:
				containedHandlers.Add(handler);
				break;
			case RegionRelation.Partial:
				throw new InvalidOperationException("wrapped range partially overlaps an existing exception entry; its try, handler, and filter regions must all the fully contained or disjoint");
			default:
				throw new InternalStateException("out of range RegionRelation enum value");
			}
		}
		return containedHandlers;
	}

	private static RegionRelation classifyExceptionHandler(ILContext il, ExceptionHandler handler, int wrappedStartIdx, int wrappedEndIdx) {
		bool hasContainedRegion = false;
		bool hasDisjointRegion = false;
		classify(handler.TryStart, handler.TryEnd, "try");
		if (handler.HandlerType == ExceptionHandlerType.Filter)
			classify(handler.FilterStart, handler.HandlerStart, "filter");
		classify(handler.HandlerStart, handler.HandlerEnd, "handler");
		if (hasContainedRegion && hasDisjointRegion)
			return RegionRelation.Partial;
		return hasContainedRegion ? RegionRelation.Contained : RegionRelation.Disjoint;

		void classify(Instruction? regionStart, Instruction? regionEnd, string regionName) {
			RegionRelation relation = classifyExceptionRegion(il, regionStart, regionEnd, wrappedStartIdx, wrappedEndIdx, regionName);
			switch (relation) {
			case RegionRelation.Disjoint:
				hasDisjointRegion = true;
				break;
			case RegionRelation.Contained:
				hasContainedRegion = true;
				break;
			case RegionRelation.Partial:
				hasContainedRegion = true;
				hasDisjointRegion = true;
				break;
			default:
				throw new InternalStateException("out of range RegionRelation enum value");
			}
		}
	}

	private static RegionRelation classifyExceptionRegion(
		ILContext il,
		Instruction? regionStart,
		Instruction? regionEnd,
		int wrappedStartIdx,
		int wrappedEndIdx,
		string regionName
	) {
		if (regionStart is null)
			throw new InvalidOperationException($"existing exception {regionName} has no start");
		int regionStartIdx = il.Instrs.IndexOf(regionStart);
		if (regionStartIdx < 0)
			throw new InvalidOperationException($"failed to find start of an existing exception {regionName} in this method");

		int regionEndIdx;
		if (regionEnd is null) {
			regionEndIdx = il.Instrs.Count;
		} else {
			regionEndIdx = il.Instrs.IndexOf(regionEnd);
			if (regionEndIdx < 0)
				throw new InvalidOperationException($"failed to find end of an existing exception {regionName} in this method");
		}

		if (regionStartIdx > regionEndIdx)
			throw new InvalidOperationException($"existing exception {regionName} has reversed boundaries");

		bool overlaps = wrappedStartIdx < regionEndIdx && regionStartIdx < wrappedEndIdx;
		if (!overlaps)
			return RegionRelation.Disjoint;
		bool contained = wrappedStartIdx <= regionStartIdx && regionEndIdx <= wrappedEndIdx;
		return contained ? RegionRelation.Contained : RegionRelation.Partial;
	}

	private static void validateNoUnsupportedInstructions(HashSet<Instruction> range) {
		foreach (Instruction instr in range)
			if (instr.OpCode.Code is Code.Ret or Code.Jmp or Code.Tail)
				throw new InvalidOperationException($"wrapped range contains unsupported instruction '{instr.OpCode}'");
	}

	private static void validateNoCrossBoundaryBranches(ILContext context, HashSet<Instruction> range) {
		foreach (Instruction instr in context.Instrs) {
			bool sourceInside = range.Contains(instr);
			foreach (Instruction target in getBranchTargets(instr)) {
				if (!context.Instrs.Contains(target))
					throw new InvalidOperationException("failed to find target of existing branch in this method");
				bool targetInside = range.Contains(target);
				if (sourceInside == targetInside)
					continue;
				if (sourceInside && !targetInside && instr.OpCode.Code is Code.Leave or Code.Leave_S)
					continue;
				throw new InvalidOperationException($"instruction '{instr}' branches {(sourceInside ? "out of" : "into")} the wrapped range");
			}
		}
	}

	private static void retargetContainedRegionEnds(List<ExceptionHandler> handlers, Instruction oldEnd, Instruction newEnd) {
		foreach (ExceptionHandler h in handlers) {
			if (ReferenceEquals(h.TryEnd, oldEnd))
				h.TryEnd = newEnd;
			if (ReferenceEquals(h.HandlerEnd, oldEnd))
				h.HandlerEnd = newEnd;
		}
	}

	private static IEnumerable<Instruction> getBranchTargets(Instruction instr) {
		switch (instr.Operand) {
		case Instruction target:
			yield return target;
			break;
		case Instruction[] targets:
			foreach (Instruction target in targets)
				yield return target;
			break;
		case ILLabel label:
			yield return label.Target ?? throw new InvalidOperationException("this method contains an unresolved IL label");
			break;
		case ILLabel[] labels:
			foreach (ILLabel label in labels)
				yield return label.Target ?? throw new InvalidOperationException("this method contains an unresolved IL label");
			break;
		}
	}
}
