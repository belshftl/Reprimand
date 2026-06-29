// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: GPL-3.0-only WITH AdditionRef-GPLv3-Celeste-Target-Platform-Exception

using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using System;

using Reprimand.Graphics;
using Microsoft.Xna.Framework.Graphics;

namespace Reprimand.Test;

internal static class SomeBullshit {
	public const float BarHeight = 22f;
	private static BasicEffect? ef;
	private static readonly VertexPositionColor[] triangle = {
		new(new Vector3(160, 100, 0), Color.Red),
		new(new Vector3(250, 190, 0), Color.Green),
		new(new Vector3(70, 190, 0), Color.Blue),
	};
	private static readonly VertexPositionColor[] triangle2 = {
		new(new Vector3(260, 100, 0), Color.Red),
		new(new Vector3(350, 190, 0), Color.Green),
		new(new Vector3(170, 190, 0), Color.Blue),
	};

	internal static void RegisterHooks() {
		IL.Celeste.Level.Render += il_levelRender;
	}

	internal static void UnregisterHooks() {
		IL.Celeste.Level.Render -= il_levelRender;
	}

	private static void il_levelRender(ILContext il) {
		ILCursor c = new(il);
		if (!c.TryGotoNext(MoveType.After, i => i.MatchCall(typeof(Glitch), "Apply")))
			throw new InvalidOperationException("failed to MatchCall(Glitch.Apply)");
		c.EmitDelegate(static () => {
			ef ??= new BasicEffect(Engine.Graphics.GraphicsDevice) {
				VertexColorEnabled = true,
				World = Matrix.Identity,
				View = Matrix.Identity,
				Projection = Matrix.CreateOrthographicOffCenter(
					0,
					Engine.Graphics.GraphicsDevice.Viewport.Width,
					Engine.Graphics.GraphicsDevice.Viewport.Height,
					0,
					0,
					1
				),
			};
		});
		c.EmitLdarg0();
		c.EmitDelegate(drawBullshit);
	}

	private static void drawBullshit(Level self) {
		using (GlobalSpriteBatch.Begin()) {
			Draw.Rect(-1f, -1f, 321f, BarHeight + 1f, Color.Black);
			using (GlobalSpriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(0f, -50f, 0f))) {
				Draw.Rect(-1f, 180f - BarHeight, 321f, BarHeight + 1f, Color.Green);
				using (GlobalSpriteBatch.Suspend()) {
					Engine.Graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
					Engine.Graphics.GraphicsDevice.DepthStencilState = DepthStencilState.None;
					foreach (EffectPass pass in ef!.CurrentTechnique.Passes) {
						pass.Apply();
						Engine.Graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangle, vertexOffset: 0, primitiveCount: 1);
					}
				}
			}
			Draw.Rect(-1f, 180f - BarHeight, 321f, BarHeight + 1f, Color.Black);
		}

		Draw.SpriteBatch.Begin();
		Draw.Rect(31f, -1f, 321f, BarHeight + 1f, Color.Red);
		using (GlobalSpriteBatch.Suspend()) {
			Draw.SpriteBatch.Begin();
			Draw.Rect(67f, 80f, 321f, BarHeight + 1f, Color.Pink);
			Draw.SpriteBatch.End();
			using (GlobalSpriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(180f, 110f, 0f))) {
				Draw.Rect(0f, 0f, 321f, BarHeight + 1f, Color.Orange);
			}

			Engine.Graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
			Engine.Graphics.GraphicsDevice.DepthStencilState = DepthStencilState.None;
			foreach (EffectPass pass in ef!.CurrentTechnique.Passes) {
				pass.Apply();
				Engine.Graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangle2, vertexOffset: 0, primitiveCount: 1);
			}
		}
		Draw.SpriteBatch.End();
	}
}
