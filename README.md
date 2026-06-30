# Reprimand

A bunch of things that a decent mod loader should really be doing, but [Everest](https://everestapi.github.io) doesn't for some reason. Includes documentation and a library for code mods, under a combined project name.

**Still in early development!**

---

## Current documentation
- none yet, sorry

## Current library features
- a replacement for `ILCursor.Goto{Next,Prev}` with much better match-fail exceptions
- `Draw.SpriteBatch` tracker, and API for scope-based nestable spritebatches and batch suspension/resume
- API to be able to bind the backbuffer without clearing it, even if the `RenderTargetUsage` isn't set to `PreserveContents`
- a basic Roslyn analyzer

---

## Additional info

The library is still in early development, and as such is on major version 0. During this period, API stability will be attempted but is not promised.

Once the library exits in-dev and switches to major version 1, semver will be used as the versioning scheme. API stability will be guaranteed for a given major release, but:
- Major releases will **not** be developmental landmarks or be special in any way; the major version may be bumped at any time. Attempts will be made to not bump it unless necessary once the early-dev dust settles, but no guarantees.
- Existing APIs may be marked as deprecated without the major version changing, and then subsequently be removed in the next major release.

---

## The TODO corner

Current actively worked-on documentation:
- none yet, sorry

Currently actively worked-on library features:
- adding more features to the roslyn analyzer
- `[OnLoad]`, `[OnUnload]`, `[OnLoadWithOptionalDep]`, `[OnUnloadWithOptionalDep]`, etc. implemented ~~using roslyn sourcegen~~ In some way
- some way to soft-enforce "every hook has a detour ID" with the analyzer, it should only warn and not error though

Planned documentation (roughly in highest-to-lowest priority order but subject to be reordered at any time):
- manual hooking, native hooks, method cloning, `DynamicMethodDefinition`
- input stack behavior, where 2D axis input is normalized between circle/square, `Check`/`Released` interactions with buffering, buffering edge cases like changing `BufferTime` mid-buffer, whether a press+release within a single frame produces nothing or `Pressed`+`Released`, etc.
- `EntityData` and entity IDs
- `Level.Render()` structure, what matrices are applied where, and how/where to render HD elements
- I/O, accessing files in your mod even if your mod is zipped, loading native libraries
- reflection over other mods, hooking other mods, ordering against other hooks, etc
- shaders in XNA, maybe how to port over GLSL/WGSL/etc from shadertoy and such if I figure it out myself
- how to interop with SRT and how the save/load actually work

Planned library features (roughly in highest-to-lowest priority order but subject to be reordered at any time):
- simple [Lönn](https://github.com/CelestialCartographers/Loenn) lua plugin generator from attributes on entities/triggers/stylegrounds
- a more ergonomic to use logger API with custom sinks incl. a default `Logger` sink and an ingame GUI sink
- more comprehensive roslyn analyzer, Everest's kinda sucks, as well as maybe some additional sourcegen utilities
- better state management utilities for entities and some kind of interface with "on SRT savestate save" / "on SRT savestate load"
- make the Lönn lua plugin generator fancier
- hi-res input event stream API and input override layers
- standard expression evaluator, `EntityData.Expr()`, ergonomic expression reeval/hot-reload
- opt-in expression eval for string fields in every entity including ones without explicit support, basic reflective hot reload for common cases
- new dialogue format with better hot reload and conditional/random/generated dialogue values and caching of evaluated values
- SRT-like reusable custom hotkeys menu so that you can add things like descriptions to buttons and headers/subheaders and split them into sections and such
