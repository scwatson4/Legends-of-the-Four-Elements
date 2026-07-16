# Quest 3 "Enter the Fight" Mode + Voice Command

The vision: your laptop is the headquarters command screen; you put on the
Quest 3 and drop into the battle as any unit — including the Avatar — while
voice orders ("send in backup", "build a bison sanctuary", "get me five
earthbenders") command the wider war.

## What is already implemented (works on desktop TODAY)

### Embodiment / hero mode — `EmbodimentController`
Select exactly one of your units and press **H**: the camera drops into its
head. WASD runs (NavMesh-constrained, so no walking through cliffs), mouse
looks, left-click/Space attacks whatever is in front of you, H/Esc returns
to the command camera. Possess the Avatar and T/G (elements, Avatar State)
still work from the inside. The RTS keeps simulating around you — waves,
AI, fog, everything. This *is* the VR gameplay layer; VR just swaps what
renders and what steers.

Two integration points were built specifically for VR:
- `EmbodimentController.Instance.HeadAnchor` — a transform on the possessed
  unit's head. In VR you parent your **XR Origin** here instead of the
  desktop camera.
- `Possess(unit)` / `Release()` are public, so a VR menu or gaze-select can
  drive possession.

### Voice command — `VoiceCommander`
Hold **V**, speak, release. Pipeline: Unity `Microphone` → WAV → OpenAI
transcription (`gpt-4o-mini-transcribe`) → a tiny chat call that maps your
sentence onto your **actual roster** (the model is shown your real
unit/building names and returns JSON) → executed through the same
Economy/spawner code the mouse uses. Implemented intents:

| You say | What happens |
|---|---|
| "Send backup to this location" / "I need reinforcements" | your idle combat units move to you (embodied) or to your mouse cursor (commander) |
| "Let's build a new sanctuary for flying bisons" | fuzzy-matches your building roster (→ Bison Stable), pays silver, finds clear ground nearby, constructs it |
| "Get me 5 new earthbenders" | queues 5 at your command center like clicking the button 5 times |

Setup: put your API key in `Assets/Resources/openai_key.txt`
(**gitignored** — never commit it) or the `OPENAI_API_KEY` env var, add a
`VoiceCommander` to the level scene, optionally wire a status TMP label.
Test without a mic: call `ExecuteText("get me three waterbenders")` from a
UI button or the inspector. Expect ~2–4 s round-trip latency and normal
per-command API billing. Quest builds request the microphone permission at
runtime automatically.

**No OpenAI account? Any OpenAI-COMPATIBLE endpoint works** — LiteLLM
proxies, OpenRouter, Azure gateways. Two files, no wiring:
1. `Assets/Resources/openai_key.txt` → your key (e.g. your LiteLLM key)
2. `Assets/Resources/openai_base_url.txt` → your endpoint base URL, e.g.
   `https://your-litellm-host/v1` (also gitignored)

Then set the two model names on the VoiceCommander component to models
your endpoint actually routes. Caveat: **transcription** needs your proxy
to route an audio model (e.g. `whisper-1`) — many LiteLLM configs only
route chat models. If yours doesn't, voice-to-text won't work, but every
intent still works through `ExecuteText` (type instead of talk), which
only needs a chat model. Voice is 100% optional — the game never requires
a key; without one it just disables itself.

## Getting it onto the Quest 3 — two paths

### Path 1 (start here): Quest Link / Air Link — one machine
Plug in (or Air Link to) your gaming laptop and run the PC build with VR
enabled. When you press H, the headset IS the unit's head; take it off (or
just look at the monitor) for commander mode. This gets you the fantasy
with a fraction of the work — no Android build, full graphics.

Editor steps (~1 hour):
1. Package Manager: install **XR Plugin Management** + **OpenXR Plugin**
   (`com.unity.xr.management`, `com.unity.xr.openxr`).
2. Project Settings > XR Plug-in Management: enable **OpenXR** for
   Windows; add the **Meta Quest Touch Pro/Plus Controller Profiles** in
   OpenXR interaction profiles.
3. Scene: add an **XR Origin** (from XR Interaction Toolkit or a plain
   TrackedPoseDriver camera rig), disabled by default. A ~20-line script
   toggles it: on `Possess` → disable the desktop camera, enable the rig,
   parent it to `EmbodimentController.Instance.HeadAnchor`; on `Release` →
   reverse. (Keep the desktop path as fallback.)
4. Controllers: map left thumbstick → the move input, right thumbstick →
   yaw, trigger → attack, A → release, B → push-to-talk. Quickest route is
   the classic Input class (`Input.GetAxis("XRI Left Thumbstick...")` via
   Input System bindings) feeding the same methods `EmbodimentController`
   uses for WASD/mouse — its input reads are isolated in `DriveMovement` /
   `DriveLook` / `DriveAttack`, so this is a focused edit.

### Path 2 (the full dream): Quest as a second player — asymmetric co-op
Laptop **hosts** a multiplayer match (the existing Netcode layer) as the
commander; the Quest runs a native **Android build of this same project**,
joins over Wi-Fi as a client on the same team (co-op toggle), and plays
permanently embodied. This is genuinely the "one friend is the general,
one is the soldier" mode — and it's also the hardest remaining feature.

What it needs beyond Path 1:
1. Android build target + Meta Quest OpenXR feature group; Quest in
   developer mode; build & deploy via Link cable.
2. A VR-first UI scene for the Quest client (join-by-IP panel in
   world space; auto-embody after spawn).
3. **Owner-authority movement for the embodied unit**: today clients relay
   move orders to the server (RTS-style), but direct VR steering wants the
   Quest to own its unit's transform. NGO supports per-object ownership —
   give the possessed unit's NetworkObject to the Quest client and switch
   its NetworkTransform to owner-authoritative while possessed. This is
   the one real engineering task; everything else is configuration.
4. Performance: Quest 3 is a mobile chip — expect to need lower-poly
   models, baked lighting, and reduced unit counts for standalone play.

Honest recommendation: wire Path 1 first — it exercises embodiment, voice,
and VR comfort with almost no risk. Attempt Path 2 once the flat game is
content-complete.

## Quest 3 build pre-flight (audited — code side is CLEAN)

A full platform audit of this branch found and fixed everything in code
that would break a Quest (Android/ARM64) build:

- ✅ **No editor APIs in runtime code** — `TerrainMapLoader` (NavMeshGenA)
  used `UnityEditor` from a runtime folder, which failed EVERY device
  build; it's now compiled out of builds (`#if UNITY_EDITOR`).
- ✅ **Microphone permission** — VoiceCommander now requests
  `android.permission.RECORD_AUDIO` at runtime on Android/Quest (Unity
  adds it to the manifest automatically because the Microphone API is
  referenced).
- ✅ **API key on Quest** — env vars don't exist on Android; put the key at
  `Assets/Resources/openai_key.txt` (already the fallback, gitignored).
- ✅ **URP-safe runtime primitives** — everything created with
  `GameObject.CreatePrimitive` at runtime (scooters, tornadoes, couriers,
  mounts, beacons...) goes through `GreyboxMaterial.Harmonize`, so nothing
  renders magenta on device.
- ✅ **No XR package dependency in code** — the project compiles with zero
  XR packages; they're additive when you're ready (below).
- ✅ **Saves** are PlayerPrefs (works on Quest), no `System.IO` writes.

What's left is pure editor/device configuration (can't be done from code):
1. Build Settings → **Android**, Texture Compression ASTC.
2. Player Settings: **IL2CPP**, target **ARM64** only, Graphics API
   **Vulkan** (or GLES3), Minimum API Level 29+.
3. Package Manager: add **OpenXR Plugin** (`com.unity.xr.openxr`) + enable
   the **Meta Quest** feature group in XR Plug-in Management (or use the
   Oculus plugin) — then assign the `HeadAnchor` camera per the sections
   above.
4. Quest in Developer Mode + USB debugging; Build & Run.
5. Internet access for voice: Player Settings → Internet Access
   **Require** (voice command calls OpenAI over HTTPS).

## Quest 3 performance — should you worry?

Short answer: **not for Path 1, yes-but-manageably for Path 2.**

- **Path 1 (Link/Air Link)**: your laptop's GPU renders everything; the
  Quest is just a display. An RTS scene that runs on the laptop runs over
  Link. No overheating concern beyond a normal VR session.
- **Path 2 (standalone Android build)**: the Quest 3 is a mobile chipset
  pushing two high-res eyes at 72–120 Hz. A full RTS battle is exactly the
  kind of scene that drops frames and heats the headset. It's doable —
  big battles exist in Quest games — but budget consciously:

  **Frame-rate budget rules of thumb (standalone Quest 3):**
  - ~50–80 active NavMesh agents max; cap wave sizes / unit counts in a
    "Quest profile" (add a `qualityCap` to MatchManager/CampaignManager).
  - No realtime shadows; bake lighting; one directional light.
  - URP mobile settings: disable post-processing or keep it to color
    grading; MSAA 4x instead of post AA; render scale ~1.0.
  - LODs on every unit model, aggressive far-distance culling; the fog of
    war actually HELPS (hidden enemies' renderers are already disabled).
  - Particle discipline: elemental VFX are the biggest risk — cap particle
    counts, no soft particles, no per-particle lights.
  - Enable **Fixed Foveated Rendering** (Meta XR settings) — free ~15%.
  - Script hotspots already have knobs: raise `FogOfWar.updateInterval`
    (0.2 → 0.4s), `MinimapController.refreshInterval` (0.4 → 1s), lower
    `FogOfWar.gridResolution` (128 → 64), and keep `EnemyAI.searchInterval`
    at 2s+. These scans (`FindObjectsByType`/`OverlapSphere`) are fine on
    desktop but are the first thing to throttle on mobile.
  - Thermals: 72 Hz refresh mode, and expect ~30–45 min comfortable
    sessions in heavy scenes.

  Practical plan: finish and tune the flat game first, then profile ONE
  battle scene on-device with the Unity Profiler before optimizing anything.

## Comfort & design notes
- Smooth locomotion in a unit's body can cause motion sickness; add a
  vignette during movement and offer snap-turn (both are standard XR
  Interaction Toolkit components).
- Keep the possessed unit's own model hidden from its camera (layer mask
  "hide from VR camera") so you don't see the inside of your own head —
  one culling-mask change on the rig camera.
- Voice works great in VR (the Quest mic feeds Unity's `Microphone` API
  the same way), so V-to-talk maps naturally onto a controller button.
