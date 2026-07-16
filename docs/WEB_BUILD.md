# Shipping a Browser Version (Unity Web / WebGL)

**Short answer: yes.** Unity 6 builds this game for the browser, and the
codebase is already web-safe (audited — the one incompatible API, the
microphone, now compiles itself out of web builds). A lightweight
single-player build — skirmish + campaign, greybox or with art — can run on
itch.io or any static host. Here's the honest scope and the recipe.

## What works in the browser

| Feature | Browser | Notes |
|---|---|---|
| Skirmish vs AI, campaign, tutorials | ✅ | everything single-player just works |
| Saves (campaign progress, chi) | ✅ | PlayerPrefs → browser IndexedDB |
| Generated sounds/music | ✅ | WAVs play fine |
| Greybox + all game systems | ✅ | trees, perches, tremors, portals... |
| **Multiplayer** | ⚠️ | browsers can't use UDP — UnityTransport must be switched to **WebSockets** (one checkbox: `Use WebSockets` on the UnityTransport component, or use Unity Relay with wss). Host must be a non-browser build or a dedicated server. |
| **Voice command (V)** | ❌ | `UnityEngine.Microphone` doesn't exist on WebGL — VoiceCommander now disables itself gracefully in web builds (text commands via `ExecuteText` still work) |
| **Quest 3 hero mode** | ❌ | no Unity WebXR support — VR stays on the native Quest build |

## The recipe (~30 minutes the first time)

1. **Install the module**: Unity Hub → your 6000.0.43f1 install → Add
   modules → **Web Build Support**.
2. **Switch platform**: File → Build Profiles → Web → Switch Platform.
3. **Scenes**: include MainMenuScene + your gameplay scene(s) only. The
   generated `Skirmish_Generated` scene is a perfect first web build (it
   even self-configures when played directly).
4. **Player Settings for a LIGHT build**:
   - Publishing Settings → Compression Format: **Brotli**
   - Player → Managed Stripping Level: **High**
   - Quality: drop shadows to low/off, disable HDR on the URP asset
   - Resolution: Run In Background ON (RTS players tab out)
5. **Build** to a folder → you get `index.html` + a `Build/` folder.
6. **Host it**:
   - **itch.io** (easiest): zip the folder, upload as HTML game, check
     "This file will be played in the browser". Free, instant, shareable.
   - **GitHub Pages**: push the build to a `gh-pages` branch — note GitHub
     Pages doesn't serve Brotli with the right headers by default; use
     **Gzip** compression (or "Decompression Fallback" ON) for Pages.
   - Unity Play (play.unity.com) also hosts WebGL uploads free.

## Keeping it lightweight

- The heavy third-party packs (AnythingWorld, character packs, bison
  models) are only included **if referenced by a built scene** — a greybox
  web build that only references generated prefabs stays small
  (target: 30–60 MB compressed, mostly engine).
- Everything in `Assets/Resources/` ships ALWAYS (that's how Resources
  works). Ours is ~1 MB (audio + database) — fine.
- If build size balloons, open the Build Report (Editor.log after build)
  and look for texture/mesh entries — some pack got referenced.
- WebGL has no threads: keep `FindObjectsByType` polling modest on huge
  maps (the default map sizes are fine).

## Suggested web packaging

Ship the browser demo as: Boot Camp + the four academies + skirmish on the
generated map, single-player only, plastic-mode toggle on (it reads GREAT
in a browser demo). That's a complete, shareable taste of the game with no
multiplayer server to run.

## Platform audit status (this branch)

- ✅ No `UnityEditor` references in runtime code (the one offender,
  `TerrainMapLoader`, is now `#if UNITY_EDITOR`-guarded — it used to break
  EVERY device build: Quest, Web, standalone)
- ✅ Microphone/voice compiled out of web builds, permission-requested on
  Quest (`android.permission.RECORD_AUDIO`)
- ✅ Runtime-created primitives use `GreyboxMaterial.Harmonize` — no
  magenta objects under URP in device builds
- ✅ No `System.IO` file writes, no `Application.dataPath` reliance —
  saves are PlayerPrefs (IndexedDB on web, local on Quest)
- ✅ Input: project runs both input handlers; all `Input.*` calls work in
  builds
- ✅ No XR package required to compile — Quest packages are additive
  (see `VR_AND_VOICE.md`)
