// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using Celeste.Mod;
using System;

namespace Reprimand;

public class ReprimandModule : EverestModule {
	public const string Name = "Reprimand";

	public static ReprimandModule Instance { get; private set; }

	public ReprimandModule() {
		Instance = this;
#if DEBUG
		Logger.SetLogLevel(Name, LogLevel.Verbose);
#else
		Logger.SetLogLevel(Name, LogLevel.Info);
#endif
	}

	public override void Load() {
		// TODO something nicer than hardcoding these in
		Graphics.GlobalSpriteBatch.RegisterHooks();
		//Test.SomeBullshit.RegisterHooks();

		/*
		Monocle.Engine.Graphics.PreparingDeviceSettings += (s, e) => {
			e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = Microsoft.Xna.Framework.Graphics.RenderTargetUsage.PreserveContents;
		};
		*/
	}

	public override void Unload() {
		//Test.SomeBullshit.UnregisterHooks();
		Graphics.GlobalSpriteBatch.UnregisterHooks();
	}
}
