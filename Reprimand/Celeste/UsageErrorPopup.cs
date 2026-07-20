// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Cil;
using Reprimand.Graphics;
using Reprimand.Lifecycle;
using Reprimand.MonoMod;

namespace Reprimand.Celeste;

internal static class UsageErrorPopup {
	public const string FontPath = "fonts/JetBrainsMonoNL-Bold.ttf";
	public static readonly Color OuterBgColor = Calc.HexToColorWithAlpha("201d21ef");
	public static readonly Color InnerBgColor = Calc.HexToColorWithAlpha("3e3b3fdf");
	public static readonly Color ErrorTextColor = Calc.HexToColor("f94e47");
	public static readonly Color BodyTextColor = Color.White * 0.9f;
	public static readonly Color FaintTextColor = Color.White * 0.2f;
	public static readonly Color MarkerLineColor = Calc.HexToColor("e72929");

	public const float Margin = 8f;
	public const float PopupW = 760f;
	public const float PopupH = 220f;

	private static readonly ConditionalWeakTable<Level, List<EntityUsageErrorInfo>> errors = new();
	private static FontSystem? fontSystem;
	private static SpriteFontBase? font;

	[OnLoad(UndoMethod = nameof(Unload))]
	[MemberNotNull(nameof(fontSystem))]
	public static void Load() {
		using Stream fontData = typeof(UsageErrorPopup).Assembly.GetManifestResourceStream(FontPath) ??
			throw new InternalStateException($"failed to find embedded resource '{FontPath}'");
		fontSystem = new FontSystem(
			new FontSystemSettings() {
				FontLoader = new FontStashSharp.Rasterizers.FreeType.FreeTypeLoader(),
				KernelWidth = 2,
				KernelHeight = 2,
			}
		) {
			UseKernings = false,
		};
		fontSystem.AddFont(fontData);
		On.Celeste.Level.LoadLevel += on_Level_LoadLevel; // to clear on level load
		On.Celeste.Level.LoadCustomEntity += on_Level_LoadCustomEntity; // thrown in ctor
		On.Monocle.EntityList.UpdateLists += on_EntityList_UpdateLists; // thrown in Added/Awake
		IL.Celeste.Level.Render += il_Level_Render;
	}

	public static void Unload() {
		On.Celeste.Level.LoadLevel -= on_Level_LoadLevel;
		On.Celeste.Level.LoadCustomEntity -= on_Level_LoadCustomEntity;
		On.Monocle.EntityList.UpdateLists -= on_EntityList_UpdateLists;
		IL.Celeste.Level.Render -= il_Level_Render;
	}

	private static void addError(Level level, EntityUsageErrorInfo info) {
		List<EntityUsageErrorInfo> list = errors.GetOrCreateValue(level);
		lock (list) // i know i complained once about people using normal objects as mutexes i'm sorry i'm a fraud
			list.Add(info);
	}

	private static void clearErrors(Level level) {
		List<EntityUsageErrorInfo> list = errors.GetOrCreateValue(level);
		lock (list)
			list.Clear();
	}

	private static EntityUsageErrorInfo[] getErrors(Level level) {
		if (errors.TryGetValue(level, out List<EntityUsageErrorInfo>? list))
			lock (list)
				return list.ToArray();
		return Array.Empty<EntityUsageErrorInfo>();
	}

	private static EntityUsageErrorInfo errFromEntity(Entity entity, string message, string thrownFrom) => new() {
		Entity = entity,
		Id = entity.SourceId.ID,
		Position = entity.Position,
		Message = message,
		ThrownFrom = thrownFrom,
	};

	private static void on_Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
		clearErrors(self);
		orig(self, playerIntro, isFromLoader);
	}

	private static bool on_Level_LoadCustomEntity(On.Celeste.Level.orig_LoadCustomEntity orig, EntityData entityData, Level level) {
		try {
			return orig(entityData, level);
		} catch (EntityUsageException ex) {
			addError(level, new EntityUsageErrorInfo() {
				Entity = ex.Entity,
				Id = entityData.ID,
				Position = entityData.Position,
				Message = ex.Message,
				ThrownFrom = "constructor",
			});
			// don't RemoveSelf as the entity hasn't been added yet
			return false;
		} catch (TargetInvocationException invEx) when (invEx.InnerException is EntityUsageException ex) {
			addError(level, new EntityUsageErrorInfo() {
				Entity = ex.Entity,
				Id = entityData.ID,
				Position = entityData.Position,
				Message = ex.Message,
				ThrownFrom = "constructor",
			});
			// don't RemoveSelf as the entity hasn't been added yet
			return false;
		}
	}

	private static void on_EntityList_UpdateLists(On.Monocle.EntityList.orig_UpdateLists orig, EntityList self) {
		// i'm sorry for replacing the entire method body but i really don't have it in me to do this from an il hook
		// this is mostly a copypaste of the decompiled original method
		var lvl = self.Scene as Level;
		if (self.toAdd.Count > 0) {
			for (int i = 0; i < self.toAdd.Count; i++) {
				Entity ent = self.toAdd[i];
				if (self.current.Add(ent)) {
					self.entities.Add(ent);
					if (self.Scene is not null) {
						self.Scene.TagLists.EntityAdded(ent);
						self.Scene.Tracker.EntityAdded(ent);
						try {
							ent.Added(self.Scene);
						} catch (EntityUsageException ex) when (lvl is not null) {
							addError(lvl, errFromEntity(ent, ex.Message, "Added"));
							self.current.Remove(ent);
							self.entities.Remove(ent);
							self.Scene.TagLists.EntityRemoved(ent);
							self.Scene.Tracker.EntityRemoved(ent);
							Engine.Pooler.EntityRemoved(ent);
						}
					}
				}
			}
			self.unsorted = true;
		}
		if (self.toRemove.Count > 0) {
			for (int j = 0; j < self.toRemove.Count; j++) {
				Entity ent = self.toRemove[j];
				if (self.current.Remove(ent)) {
					self.entities.Remove(ent);
					if (self.Scene is not null) {
						ent.Removed(self.Scene);
						self.Scene.TagLists.EntityRemoved(ent);
						self.Scene.Tracker.EntityRemoved(ent);
						Engine.Pooler.EntityRemoved(ent);
					}
				}
			}
			self.toRemove.Clear();
			self.removing.Clear();
		}
		if (self.unsorted) {
			self.unsorted = false;
			self.entities.Sort(EntityList.CompareDepth);
		}
		if (self.toAdd.Count <= 0)
			return;
		self.toAwake.AddRange(self.toAdd);
		self.toAdd.Clear();
		self.adding.Clear();
		foreach (Entity ent in self.toAwake) {
			if (ReferenceEquals(ent.Scene, self.Scene)) {
				try {
					ent.Awake(self.Scene);
				} catch (EntityUsageException ex) when (lvl is not null) {
					addError(lvl, errFromEntity(ent, ex.Message, "Awake"));
					self.current.Remove(ent);
					self.entities.Remove(ent);
					self.Scene.TagLists.EntityRemoved(ent);
					self.Scene.Tracker.EntityRemoved(ent);
					Engine.Pooler.EntityRemoved(ent);
				}
			}
		}
		self.toAwake.Clear();
	}

	private static void il_Level_Render(ILContext il) {
		ILCursor c = new(il);
		c.RequireGotoNext(
			MoveType.After,
			static i => i.MatchLdarg0(),
			static i => i.MatchLdfld<Level>("SubHudRenderer"),
			static i => i.MatchLdarg0(),
			static i => i.MatchCallvirt<Renderer>("Render")
		);
		c.EmitLdarg0();
		c.EmitDelegate(draw);
	}

	private static void draw(Level self) {
		const float screenW = 1920f;
		if (fontSystem is null)
			throw new InternalStateException("expected font system to be active by now");
		font ??= fontSystem.GetFont(20f);
		float chr = font.MeasureString("M").X;
		int cols = Math.Max(1, (int)MathF.Floor((PopupW - chr) / chr));
		Vector2 pos = new(screenW - Margin - PopupW, Margin);
		foreach (EntityUsageErrorInfo info in getErrors(self)) {
			drawOne(self, font, in info, pos, cols);
			pos.Y += PopupH + Margin;
		}
	}

	private static void drawOne(Level level, SpriteFontBase font, in EntityUsageErrorInfo info, Vector2 origin, int cols) {
		Vector2 pen = Vector2.Zero;
		Rectangle popup = new((int)origin.X, (int)origin.Y, (int)PopupW, (int)PopupH);
		Rectangle oldScissor = Engine.Graphics.GraphicsDevice.ScissorRectangle;
		using (GlobalSpriteBatch.Begin(samplerState: SamplerState.LinearClamp, rasterizerState: RasterizerState.CullNone, transformMatrix: Engine.ScreenMatrix)) {
			Vector2 target = level.WorldToScreen(info.Position);
			Draw.Line(popup.Center.ToVector2(), target, MarkerLineColor, thickness: 4f);
			Draw.Rect(target - Vector2.One * 8f, 16f, 16f, MarkerLineColor);
		}
		try {
			// assumes Engine.ScreenMatrix is non-rotating, i can't think of a reason for it to rotate
			Engine.Graphics.GraphicsDevice.ScissorRectangle = transformScissor(popup, Engine.ScreenMatrix, Engine.Graphics.GraphicsDevice.Viewport);
			using (GlobalSpriteBatch.Begin(samplerState: SamplerState.LinearClamp, rasterizerState: new RasterizerState() { CullMode = CullMode.None, ScissorTestEnable = true }, transformMatrix: Engine.ScreenMatrix)) {
				Draw.Rect(popup, OuterBgColor);

				const string header = "Entity or trigger usage error:";
				Vector2 headerSize = font.MeasureString(header + " ");
				GlobalSpriteBatch.Batch.DrawString(font, header, origin + pen + Vector2.One * Margin, ErrorTextColor);
				GlobalSpriteBatch.Batch.DrawString(font, $"(thrown from {info.ThrownFrom})", origin + pen + Vector2.One * Margin + Vector2.UnitX * headerSize.X, FaintTextColor);
				pen.Y += font.LineHeight;

				string? displayName = null;
				Type? type = info.Entity?.GetType();
				if (type?.GetCustomAttributes<CustomEntityAttribute>().FirstOrDefault() is CustomEntityAttribute a)
					displayName = a.IDs.FirstOrDefault();
				displayName ??= type?.Name ?? "<unknown>";

				GlobalSpriteBatch.Batch.DrawString(font, $"'{displayName}' with ID {info.Id} at x={info.Position.X:F2}, y={info.Position.Y:F2}:", origin + pen + Vector2.One * Margin, BodyTextColor);
				pen.Y += font.LineHeight / 2f; // this looked nicer at 1/2 than full height

				pen.X += Margin;
				Draw.Rect(origin + pen + Vector2.UnitY * font.LineHeight, PopupW - Margin * 2f, PopupH - pen.Y - Margin - font.LineHeight, InnerBgColor);
				pen.Y += font.LineHeight;

				string wrapped = wrapAssumingMonospace(info.Message ?? "<null>", cols);
				GlobalSpriteBatch.Batch.DrawString(font, wrapped, origin + pen + Vector2.One * Margin, BodyTextColor);
			}
			using (GlobalSpriteBatch.Begin(blendState: BlendState.Additive, samplerState: SamplerState.LinearWrap, rasterizerState: new RasterizerState() { CullMode = CullMode.None, ScissorTestEnable = true }, transformMatrix: Engine.ScreenMatrix))
				OVR.Atlas["overlay"].Draw(Vector2.Zero, Vector2.Zero, Color.White * 0.3f);
		} finally {
			 Engine.Graphics.GraphicsDevice.ScissorRectangle = oldScissor;
		}
	}

	private static Rectangle transformScissor(Rectangle r, Matrix m, Viewport vp) {
		var p0 = Vector2.Transform(new Vector2(r.Left,  r.Top), m);
		var p1 = Vector2.Transform(new Vector2(r.Right, r.Top), m);
		var p2 = Vector2.Transform(new Vector2(r.Left,  r.Bottom), m);
		var p3 = Vector2.Transform(new Vector2(r.Right, r.Bottom), m);

		float minX = MathF.Min(MathF.Min(p0.X, p1.X), MathF.Min(p2.X, p3.X));
		float minY = MathF.Min(MathF.Min(p0.Y, p1.Y), MathF.Min(p2.Y, p3.Y));
		float maxX = MathF.Max(MathF.Max(p0.X, p1.X), MathF.Max(p2.X, p3.X));
		float maxY = MathF.Max(MathF.Max(p0.Y, p1.Y), MathF.Max(p2.Y, p3.Y));

		int left = vp.X + (int)MathF.Floor(minX);
		int top = vp.Y + (int)MathF.Floor(minY);
		int right = vp.X + (int)MathF.Ceiling(maxX);
		int bottom = vp.Y + (int)MathF.Ceiling(maxY);

		return Rectangle.Intersect(new Rectangle(left, top, right - left, bottom - top), vp.Bounds);
	}

	private static string wrapAssumingMonospace(string text, int cols) {
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cols);

		StringBuilder sb = new(text.Length + text.Length / cols);
		int col = 0;

		for (int i = 0; i < text.Length; ) {
			if (text[i] == '\r') {
				++i;
				continue;
			}

			if (text[i] == '\n') {
				sb.Append('\n');
				col = 0;
				++i;
				continue;
			}

			while (i < text.Length && text[i] is ' ' or '\t')
				++i;
			if (i >= text.Length || text[i] is '\r' or '\n')
				continue;

			int wordStart = i;
			while (i < text.Length && text[i] is not ' ' and not '\t' and not '\r' and not '\n')
				++i;
			int wordLength = i - wordStart;
			if (col != 0) {
				if (col + 1 + wordLength <= cols) {
					sb.Append(' ');
					++col;
				} else {
					sb.Append('\n');
					col = 0;
				}
			}

			// hard wrap words longer than one line
			while (wordLength != 0) {
				int count = Math.Min(wordLength, cols - col);

				sb.Append(text, wordStart, count);
				wordStart += count;
				wordLength -= count;
				col += count;

				if (wordLength != 0) {
					sb.Append('\n');
					col = 0;
				}
			}
		}
		return sb.ToString();
	}
}
