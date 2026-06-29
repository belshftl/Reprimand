# Reprimand

A bunch of things that a decent mod loader should really be doing, but [Everest](https://everestapi.github.io) doesn't for some reason. Includes documentation and a library for code mods, under a combined project name.

---

**Current documentation**:
- none yet, sorry

**Current library features**:
- a replacement for `ILCursor.Goto{Next,Prev}` with much better match-fail exceptions:
- `Draw.SpriteBatch` tracker, and API for scope-based nestable spritebatches and batch suspension/resume

---

Current actively worked-on documentation:
- none yet, sorry

Currently actively worked-on library features:
- a basic Roslyn analyzer
- some kind of solution for XNA's "swapping to the backbuffer clears it" problem, I think FNA has some sort of flag to preserve the contents, maybe then add some kind of global mod capability-request system so that if any mod wants it then it's enabled but otherwise it's disabled

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
- `[OnLoad]`, `[OnUnload]`, `[OnLoadWithOptionalDep]`, `[OnUnloadWithOptionalDep]`, etc. implemented using roslyn sourcegen
- more comprehensive roslyn analyzer, Everest's kinda sucks, as well as maybe some additional sourcegen utilities
- better state management utilities for entities and some kind of interface with "on SRT savestate save" / "on SRT savestate load"
- make the Lönn lua plugin generator fancier
- hi-res input event stream API and input override layers
- standard expression evaluator, `EntityData.Expr()`, ergonomic expression reeval/hot-reload
- opt-in expression eval for string fields in every entity including ones without explicit support, basic reflective hot reload for common cases
- new dialogue format with better hot reload and conditional/random/generated dialogue values and caching of evaluated values
- SRT-like reusable custom hotkeys menu so that you can add things like descriptions to buttons and headers/subheaders and split them into sections and such
