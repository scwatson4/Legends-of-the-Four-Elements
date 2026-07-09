# Finishing Guide — Legends of the Four Elements

This branch adds the complete **code foundation** for turning the game from
"Air player vs Fire waves" into a four-nation RTS with neutral villagers,
tameable spirits, nation selection, skirmish AI, and online multiplayer
(Army Men RTS / Halo Wars style).

Code can't click buttons in the Unity Editor for you, though. This guide is
the ordered checklist of everything left to do in the Editor: creating prefab
variants, filling in data assets, wiring UI, and baking scenes. Follow it top
to bottom — each part builds on the previous one.

> **Good news:** nothing existing was broken. If you open the project and hit
> Play on Level1 right now, the original Air-vs-Fire survival game plays
> exactly as before. Every new system layers on top.

---

## What the new code does (architecture in 60 seconds)

| System | Files | What it does |
|---|---|---|
| Factions | `Assets/Scripts/Factions/` | Replaces the 2-value `Team` enum with any number of factions. `FactionMember` marks ownership; `FactionManager` decides who is hostile to whom (team groups = alliances). Old scenes fall back to Player=faction 0, Enemy=faction 1 automatically. |
| Nation data | `NationData.cs`, `NationDatabase.cs` | ScriptableObjects describing each nation: unit roster (prefab/cost/build time), command center prefab, colors, sounds. |
| Match flow | `Assets/Scripts/Match/` | `GameSetup` carries menu choices into the level. `MatchManager` registers factions, spawns bases at `StartLocation` markers, awards income, and decides victory (last team standing). `AICommander` is the skirmish AI: trains units, defends, launches attack waves. |
| Neutrals | `Assets/Scripts/Neutrals/` | `Village` spawns wandering `Villager`s and pays tribute credits to whichever team controls it. `Spirit` (Friendly or Dark) roams the map; Dark spirits attack everyone. `Tameable` lets your units befriend/tame them — right-click a spirit with units selected. Dark spirits must be weakened below a health threshold first. |
| Nation select | `NationSelectController.cs` | Menu panel: pick nation, pick Survival or Skirmish, pick AI count, start. |
| Multiplayer | `Assets/Scripts/Multiplayer/` | Netcode for GameObjects: host/join by IP, per-player nation pick, co-op or versus teams, server-authoritative units, health and command relay. See `docs/MULTIPLAYER_SETUP.md`. |

Key compatibility notes baked into the code:

- `EnemyAI` kept its name (renaming scripts breaks prefab wiring) but now
  drives units of **any** AI faction, and its `chaseCommandCenters` flag lets
  it defend or attack-move.
- `Unit.UnitType` gained `Waterbender`, `Earthbender`, `Villager`, `Spirit`
  (appended, so existing prefabs deserialize fine).
- You can no longer select enemy units (that was a bug — selection is
  filtered to your own faction).

---

## Part 0 — First open

1. Open the project in Unity **6000.0.43f1**. The Package Manager will fetch
   two new packages added to `Packages/manifest.json`:
   - `com.unity.netcode.gameobjects` (multiplayer)
   - `com.unity.multiplayer.playmode` (test multiple players in one Editor)
2. Wait for the compile. There should be **zero errors**. If a package fails
   to download, check your network and use *Window > Package Manager > Refresh*.
3. Press Play on `Level1_Scene` and confirm the original game still works.

## Part 1 — Water & Earth unit prefabs

You already have `Assets/BenderPrefabs/AirbenderUnit.prefab` and
`FirebenderUnit.prefab`. Make the other two nations the same way:

1. Duplicate `AirbenderUnit.prefab`, rename to `WaterbenderUnit.prefab`
   (and again for `EarthbenderUnit.prefab`).
2. Swap the character model/material for a Water/Earth look. You have
   material and model options in `Generals_2013_model_pack_v2`,
   `PolygonCharacters-Labourer`, `Axe Warrior` and the `ithappy` packs; a
   simple re-tint (blue / green) is fine as a first pass.
3. On the prefab's `Unit` component set **Unit Type** to `Waterbender` /
   `Earthbender`.
4. Replace the attack VFX: the `flamethrowerEffect` slot on `AttackController`
   accepts any particle GameObject. Good sources already in the project:
   - Water: `JMO Assets` (Cartoon FX water bursts), `Vefects`
   - Earth: `GabrielAguiarProductions`, `PyroParticles` (dust/rock),
     `ExplosiveLLC` (debris)
   - Air already uses the wind effect; Fire uses the flamethrower.
5. **Important for skirmish/multiplayer:** every bender prefab (all four
   nations) should have BOTH `UnitMovement` (disabled by default — selection
   enables it) AND `EnemyAI` on it. `EnemyAI` idles harmlessly on
   player-owned units and drives the unit when an AI faction owns it, so one
   prefab works for everyone. (If you forget, `MatchManager` adds `EnemyAI`
   at runtime, but having it on the prefab is cleaner.)
6. Keep the prefab on the same **layer** as your Airbender (`Clickable`) and
   the same tag, with the same `HealthTracker` child setup.

Also make command centers for the two new nations: duplicate
`Assets/Buildings/AirNationTemple.prefab` / `FireNationCitadel.prefab`, re-skin,
and make sure each has the `CommandCenter` component, a collider, and a
`HealthTracker`.

## Part 2 — Nation data assets

1. `Assets > Create > Legends > Nation Data` — create four assets:
   `AirNation`, `WaterNation`, `EarthNation`, `FireNation` (put them in a new
   `Assets/Nations/` folder).
2. Fill each in: nation enum, display name, description (shows on the nation
   select screen), theme color, unit roster (slot 0 is the basic bender —
   it's also used as the starting squad), command center prefab.
3. Create the folder `Assets/Resources/` if missing, then
   `Assets > Create > Legends > Nation Database`, name it **exactly**
   `NationDatabase`, save it at `Assets/Resources/NationDatabase.asset`, and
   drag the four NationData assets into its list.

## Part 3 — Nation select in the main menu

1. Open `MainMenuScene`. Add a new panel under the Canvas: `NationSelectPanel`
   with:
   - four buttons (Air / Water / Earth / Fire),
   - two mode buttons or a dropdown (Survival / Skirmish),
   - a slider or buttons for AI opponent count (1–3),
   - a Start button and a Back button,
   - optional: a TMP label + description text for the selected nation.
2. Add the `NationSelectController` component to the panel. Wire:
   - nation buttons → `SelectNation(0..3)`
   - mode controls → `SetMode(0 or 1)`
   - AI count → `SetAIOpponentCount`
   - Start → `StartGame`
   - Back → `MainMenuController.CloseNationSelect`
   - set **Level Scene Name** to your gameplay scene's exact name
     (the file is `Level1_Scene.unity`, so use `Level1_Scene` — check what
     your Build Settings actually list; the old code loaded `"Level1"`).
3. On the existing `MainMenuController`, assign the new panel to
   **Nation Select Panel**. Play button now opens it. (Leave it unassigned
   and the menu behaves exactly like before.)

**How nation choice affects Survival mode:** your build buttons should call
`UnitSpawner.QueueRosterUnit(0/1/2...)` instead of the old hand-wired
prefab — then whatever nation you picked, the buttons build *that* nation's
roster. Re-wire the side panel build buttons accordingly.

## Part 4 — Skirmish scene

You can retrofit Level1 or (recommended) duplicate it as `Skirmish1_Scene`:

1. Delete the hand-placed player/enemy bases (skirmish spawns its own).
2. Create 2–4 empty GameObjects around the map, add `StartLocation` to each,
   set `index` 0..3 (0 = the human player). Keep them on baked NavMesh.
3. Create an empty `MatchController` GameObject, add `MatchManager`:
   - assign the NationDatabase,
   - tick **Spawn Bases At Start Locations**,
   - tune income (default: 25 credits / 10 s to every faction).
4. Keep `GameManager` (win/lose panels), `UnitSelectionManager`,
   `PlayerResources`, camera rig, and the side panel — same as Level1.
   You can delete `WaveManager`/`EnemyCommandCenter` from a pure skirmish
   scene (WaveManager auto-disables outside Survival anyway; the WaveUI
   countdown should be hidden too).
5. Add the scene to **Build Settings** and set it as the skirmish target
   (either point `NationSelectController.levelSceneName` at it, or use one
   scene for both modes).
6. Bake the NavMesh for the whole playable area.

Victory is last-team-standing: destroy every rival command center.

## Part 5 — Villages & spirits

**Layers first (Project Settings > Tags and Layers):** you already have
`Clickable`, `Ground`, `Attackable` (check the exact names in your
UnitSelectionManager inspector). Villagers can live on a non-clickable layer;
wild spirits should be on the **Attackable** layer so units can fight them,
and `Tameable.tamedLayerName` should be your clickable layer's exact name so
tamed spirits become selectable.

**Villager prefab:**
- Any small character model (the `PolygonCharacters-Labourer` pack is ideal).
- Components: `NavMeshAgent`, `Unit` (type `Villager`, maxUnitHealth ~30,
  drag in a `HealthTracker` child like the benders have), `Villager`.
- No AttackController — villagers are pacifists; they flee dark spirits.

**Village:**
- A hut/building model (check `ithappy` / `Mountain Terrain rocks and tree`
  packs), plus an empty parent with the `Village` component: assign the
  villager prefab, count, control radius, tribute amount.
- Place 2–4 villages between the bases — they're the map-control objective.

**Spirit prefabs (make two):**
- `FriendlySpirit`: glowing creature model or particle body (`JMO Assets`,
  `Vefects`, `FXIFIED`, or a flying bison variant!). Components:
  `NavMeshAgent`, `Unit` (type `Spirit`), `Spirit` (alignment Friendly),
  `Tameable` (maxHealthFractionToTame = 1). Layer: Attackable.
- `DarkSpirit`: same, but `Spirit` alignment Dark,
  `Tameable.maxHealthFractionToTame = 0.5`, plus `AttackController` (with a
  spooky VFX in the flamethrower slot, damage ~10, trigger collider) and an
  Animator with the same `isMoving/isFollowing/isAttacking/Die` parameters as
  the benders (you can reuse the bender animator controller). The `EnemyAI`
  brain and `FactionMember` are added automatically at runtime.
- Scatter spirits around the map edges and near villages.

**How taming plays:** select units → right-click a friendly spirit → they
walk over and channel ~6 s → it joins you (selectable, orderable, fights for
you). Dark spirits must first be beaten below half health, then right-clicked
to befriend. Assign `spiritAttackClip` on the SoundManager for their attacks.

## Part 6 — Multiplayer

Full walkthrough in **`docs/MULTIPLAYER_SETUP.md`** (NetworkManager setup,
prefab registration, testing with Multiplayer Play Mode, and how to upgrade
from direct-IP to Unity Relay so friends can join without port forwarding).

## Part 7 — Asset & polish checklist

Art / audio you still need (sources suggested from packs already imported):

- [ ] Water & Earth bender models + re-tinted materials (Part 1)
- [ ] Water & Earth command center buildings (Part 1)
- [ ] Water & Earth attack VFX and attack sounds → assign
      `waterbenderAttackClip` / `earthbenderAttackClip` on SoundManager
- [ ] Villager model + village huts
- [ ] Friendly & dark spirit models/VFX + `spiritAttackClip`
- [ ] Nation emblems (sprites) for the select screen (`NationData.emblem`)
- [ ] 4 nation-select portraits / descriptions (`NationData.description`)
- [ ] Optional: per-nation music variants (drop into `Level1Music`)

Gameplay tuning knobs worth a pass once it's playable:

- `MatchManager`: income rate, starting units
- `AICommander`: wave size / interval / decision interval (difficulty!)
- `Village`: tribute amount & radius
- `Tameable`: tame duration & health thresholds
- Unit costs/build times in each `NationData`

## Part 8 — Build & test

1. **Build Settings**: add `MainMenuScene` + your gameplay scene(s). Note the
   code that loads scenes by name uses the serialized fields you set in
   Part 3/6 — make sure names match exactly.
2. Test matrix:
   - Survival as each of the 4 nations (build buttons show right roster?)
   - Skirmish vs 1–3 AI (AI trains and attacks? village tribute works?)
   - Tame a friendly spirit; weaken + tame a dark spirit
   - Lose a match (base destroyed) and win one
   - Multiplayer: host + join, co-op and versus (see multiplayer doc)
