// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Celeste.Mod;
using Reprimand.Lifecycle;

namespace Reprimand;

internal static class NativeLibLoader {
	private const string runtimesDirectory = "RuntimePack/runtimes";

	private static IntPtr libfreetype = IntPtr.Zero;

	private static string getRid() {
		// TODO: think about freebsd support, presumably self-compile freetype for freebsd but idk
		string arch = RuntimeInformation.ProcessArchitecture switch {
			Architecture.X64 => "x64",
			Architecture.Arm64 => "arm64",
			_ => throw new NotSupportedException($"your device's architecture '{RuntimeInformation.ProcessArchitecture}' is currently not supported (supported: x64, arm64); sorry")
		};
		if (OperatingSystem.IsWindows())
			return "win-" + arch;
		if (OperatingSystem.IsMacOS())
			return "osx";
		if (OperatingSystem.IsLinux())
			return "linux-" + arch;
		throw new NotSupportedException("your OS is currently not supported (supported: Windows, OSX, Linux); sorry");
	}

	private static string getFreetypeFilename() {
		if (OperatingSystem.IsWindows())
			return "freetype.dll";
		if (OperatingSystem.IsMacOS())
			return "libfreetype.dylib";
		if (OperatingSystem.IsLinux())
			return "libfreetype.so";
		throw new NotSupportedException("your OS is currently not supported (supported: Windows, OSX, Linux); sorry");
	}

	private static bool matchLib(string target, string given) =>
		Regex.IsMatch(given, @"^(?:lib)?" + Regex.Escape(target) + @"(?:\.dll|(?:\.\d+(?:\.\d+)*)?\.dylib|\.so(?:\.\d+)*)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

	private static IntPtr resolvingUnmanagedDll(Assembly assembly, string libraryName) {
		if (matchLib("freetype", Path.GetFileName(libraryName)))
			return libfreetype;
		return IntPtr.Zero;
	}

	[OnLoadOneshot(Priority = -1)]
	internal static void Init() {
		string rid = getRid();

		string ftFilename = getFreetypeFilename();
		string ftVirtualPath = $"{runtimesDirectory}/{rid}/native/{ftFilename}";
		if (!Everest.Content.TryGet(ftVirtualPath, out ModAsset ftAsset))
			throw new InternalStateException($"file '{ftVirtualPath}' not found in mod directory");
		using Stream ftStream = ftAsset.Stream;
		string ftSha256 = Convert.ToHexString(SHA256.HashData(ftStream));
		ftStream.Position = 0;
		string ftOutRoot = Path.Combine(Everest.PathGame, "Reprimand", "cache", "freetype", rid);
		string ftOutDir = Path.Combine(ftOutRoot, ftSha256);
		string ftOutPath = Path.Combine(ftOutDir, ftFilename);
		Logger.Log(LogLevel.Info, "Reprimand/NativeLibLoader", "hii");
		if (Directory.Exists(ftOutRoot) && !Directory.Exists(ftOutDir)) {
			try {
				Logger.Log(LogLevel.Info, "Reprimand/NativeLibLoader", "freetype native lib directory exists but doesn't seem to have the right version, cleaning");
				Directory.Delete(ftOutRoot, recursive: true);
			} catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
				Logger.Log(LogLevel.Error, "Reprimand/NativeLibLoader", $"failed to clean freetype native lib cache directory: {e.GetType().Name}: {e.Message}");
			}
		}
		Directory.CreateDirectory(ftOutDir);
		using (FileStream ftDstStream = File.Create(ftOutPath))
			ftStream.CopyTo(ftDstStream);

		libfreetype = NativeLibrary.Load(ftOutPath);
		Assembly target = typeof(FreeTypeSharp.FT).Assembly;
		AssemblyLoadContext alc = AssemblyLoadContext.GetLoadContext(target) ?? throw new InternalStateException("expected to have FreeTypeSharp loaded");
		alc.ResolvingUnmanagedDll += resolvingUnmanagedDll;
	}
}
