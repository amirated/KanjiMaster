# Third-party tools & assets

Runtime/editor libraries and assets bundled with the project. (Kanji **data** licensing
is tracked separately in `docs/LICENSES.md`.)

## LeanTween (UI animation)

- **Purpose:** tween engine for UI animation. Used only through the project's own thin
  wrapper `KanjiMaster.UI.UITween` — game code never calls LeanTween directly.
- **Runtime dependency:** yes (ships in the build once integrated).
- **Source / distribution:** LeanTween by **Dented Pixel** (Russell Savage). Official
  sources: the free **"LeanTween"** package on the Unity Asset Store, and the repository
  at https://github.com/dentedpixel/LeanTween. Installed here as the Framework `.cs`
  files under `Assets/Plugins/LeanTween/`.
- **License:** **MIT License** — the bundled `Assets/Plugins/LeanTween/License.txt` states
  *"The MIT License (MIT) — Copyright (c) 2017 Russell Savage - Dented Pixel"* (full MIT
  permission/warranty text in that file). It additionally notes the easing equations are
  *"Open source under the BSD License. Copyright (c)2001 Robert Penner"*. Both are
  permissive; MIT/BSD require retaining the copyright + permission notice, which is
  satisfied by keeping `License.txt` in the project. The authoritative text is that file —
  do not paraphrase it.
- **Attribution:** when the in-app Credits/Licenses screen is built (see `docs/LICENSES.md`),
  include the LeanTween MIT notice (and the BSD easing notice) from `License.txt`.

### Status

**Installed and integrated.** LeanTween Framework `.cs` files + `License.txt` are in
`Assets/Plugins/LeanTween/`. It is given its own assembly (`LeanTween.asmdef`), referenced
by `KanjiRush.Runtime`, and the `LEANTWEEN_PRESENT` define is enabled, so `UITween` drives
the real LeanTween backend. The no-motion fallback is retained for builds without the
define. No production UI is animated yet.

## How the animation layer is gated

`UITween` (and its tests) compile in two modes, selected by the `LEANTWEEN_PRESENT`
scripting define:

- **Define OFF (current):** no LeanTween references are compiled; each `UITween` call
  applies the animation's **final state instantly** (no motion). The project builds
  cleanly and the UI is unchanged.
- **Define ON:** `UITween` drives LeanTween for real animation.

Because our runtime code lives in the `KanjiRush.Runtime` assembly definition (and asmdef
assemblies cannot reference the predefined `Assembly-CSharp`), LeanTween must live in its
own assembly that `KanjiRush.Runtime` references — hence the `LeanTween.asmdef`.

## Integration (done)

- `Assets/Plugins/LeanTween/LeanTween.asmdef` — puts LeanTween in a "LeanTween" assembly.
  (LeanTween's files use only `UnityEngine`, no `UnityEditor`/test refs, so a plain runtime
  asmdef is safe.)
- `Assets/Scripts/KanjiRush.Runtime.asmdef` `references` now include `"LeanTween"`.
- `Assets/Editor/LeanTweenDefineSetup.cs` auto-adds/removes the `LEANTWEEN_PRESENT`
  scripting define to match LeanTween's presence (no manual Player Settings editing; never
  silently falls back while LeanTween is installed).

Nothing else is required. If you ever remove LeanTween, the editor helper drops the define
and `UITween` reverts to the compile-safe no-motion fallback.

> Do NOT install DOTween or any other tween framework — the project standardizes on
> LeanTween behind `UITween`.
