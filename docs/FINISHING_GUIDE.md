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
| Economy | `Assets/Scripts/Economy/` | One `Economy` API for every wallet (player HUD, AI treasuries, multiplayer server). Harvestable `ResourceNode`s worked by `ResourceCollector` units, `ResourceDropoff` buildings, `IncomeBuilding` trickle buildings, kill bounties. |
| Buildings | `Assets/Scripts/Buildings/` | `Structure` health for any building, `DefenseTower`s, and `BuildingPlacer` ghost-preview construction (green/red, R to rotate). |
| Upgrades | `Assets/Scripts/Upgrades/` | `UpgradeData` assets per nation; purchased levels boost damage/HP/speed of existing and future units. AI buys them too. |
| Avatar | `Units/AvatarUnit.cs` | One-per-player hero: bends all four elements (T to cycle), Avatar State surge (G), and the only unit that can energy-bend spirits tame. |
| Maps & biomes | `Assets/Scripts/World/` | `BiomeZone` elemental climates that buff/weaken units by element, and `MapGenerator` seeded random maps (biomes, nodes, villages, spirits, start points). Map selector in the menu. |

Full faction design (every unit, building and upgrade with suggested costs):
**`docs/ROSTERS.md`** — treat it as the fill-in sheet for the NationData assets.

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
   - Workers harvest a node and deliver; income buildings tick; kill a dark
     spirit and get the bounty
   - Build the Avatar, cycle elements (T), pop Avatar State (G), energy-bend
     a spirit; confirm regular units CANNOT tame
   - Place a building (ghost turns red on units/buildings; refinery demands
     its node); tower shoots raiders
   - Buy an upgrade twice; confirm old AND new units hit harder
   - Fight inside a volcano zone as Fire, then as Water — damage should differ
   - Start a random-map match twice; layouts should differ (new seed each start)
   - Lose a match (base destroyed) and win one
   - Multiplayer: host + join, co-op and versus (see multiplayer doc)

---

# Round 2 additions — economy, buildings, upgrades, Avatar, maps

## Part 9 — Economy on the map

1. **Node prefabs** (make 4): a model + collider + `ResourceNode`, one per
   type — Fish Shoal, Crystal Deposit, Coal Seam, Spirit Grove. ~1500 silver
   each (0 = infinite). Suggested looks: rock/crystal meshes from your
   terrain packs; a shimmering particle for the grove; rippling water decal
   for the shoal.
2. **Worker prefabs** (one per nation, see ROSTERS.md): a bender-prefab
   duplicate minus AttackController, plus `ResourceCollector`,
   `Unit.category = Worker`. Optional basket/sack child assigned to
   **Carry Visual**.
3. Add `ResourceDropoff` to every command center prefab (and docks/refineries
   if you want shorter walk cycles).
4. Scatter nodes near (but not inside) bases and at contested spots — or let
   the MapGenerator do it (Part 12).
5. Set `killBounty` on spirit prefabs (dark 40, friendly 15) — hunting evil
   spirits is now an income stream, very Avatar.
6. Relabel the HUD credits text to **Silver**.

## Part 10 — Buildings & construction UI

1. Building prefabs per ROSTERS.md. Recipe: model + collider +
   `Structure` (+ `UnitSpawner` for production, `IncomeBuilding` for economy,
   `DefenseTower` for towers, `ResourceDropoff` where sensible). Layer:
   same as your attackable buildings so enemies can target them.
2. For economy buildings that should demand a node (Mine/Refinery/Dock):
   tick **Requires Nearby Node** on their `IncomeBuilding` and pick the type.
3. Fill the **buildings** list in each NationData asset.
4. Add one `BuildingPlacer` GameObject to each level scene: assign the
   ground mask (same as UnitSelectionManager's) and leave obstruction mask
   at Everything.
5. Side panel: add a "Build" tab with buttons wired to
   `BuildingPlacer.BeginPlacement(0..n)`. Left-click places, right-click/Esc
   cancels, R rotates. Unit selection pauses automatically while placing.

## Part 11 — Upgrades & the Avatar

1. Create `UpgradeData` assets (Assets > Create > Legends > Upgrade) from the
   ROSTERS.md tech-tree tables; drag them into each NationData's **upgrades**
   list. *(Or skip this entirely: the `Legends ► Bootstrap` editor menu
   creates the full tree — prerequisites, exclusive branches, research
   buildings — automatically.)*
2. Add an `UpgradePurchaser` to the side panel; wire upgrade buttons to
   `Purchase(0..n)` (index into the nation's upgrade list — same buttons work
   for all nations). Optional TMP label shows feedback.
3. **Avatar prefab per nation**: duplicate that nation's bender prefab,
   scale stats (HP ~400, damage ~25), add `AvatarUnit`, create four child
   VFX objects (air/water/earth/fire — you already own packs for each) and
   assign them, plus an aura VFX for the Avatar State. Set roster
   **category = Avatar**, cost ~600, and `Unit.category = Avatar` on the
   prefab. Add it as the LAST entry of each nation's unit roster with its own
   build button.
4. Spirits: leave `Tameable.requiresEnergyBender` ON — only the Avatar tames.

## Part 12 — Map selector & random maps

1. Build 2–3 handmade map scenes if you like (each: MatchManager +
   StartLocations + villages/spirits/nodes/biomes) **plus one "RandomMap"
   scene**: terrain + NavMeshSurface + GameManager/selection/HUD rig +
   MatchManager (spawn bases ON) + one **MapGenerator** GameObject.
2. MapGenerator setup: ground mask, map size to fit your terrain, and prefab
   lists — biome zone prefabs (next step), the 4 node prefabs, village and
   both spirit prefabs. Assign the scene's **NavMeshSurface** so paths
   rebuild after generation.
3. **Biome zone prefabs** (6): empty root + `BiomeZone` (climate + radius)
   + themed props as children — lava rocks & smoke (Volcanic), ice shards
   (Glacier), reeds/water decals (River Lands), swirling leaves (Windy
   Peaks), boulders (Stone Quarry), glowing flora (Spirit Wilds). Props are
   pure dressing; the zone component does the gameplay.
4. Menu: add a map row to the nation-select panel — buttons wired to
   `NationSelectController.SelectMap(0..n)`, and fill **Map Scene Names**
   with your scene names (all added to Build Settings). Leave
   **Randomize Seed Each Match** on: every match rolls a fresh layout.
5. Multiplayer note: MapGenerator uses a fixed shared seed when networked so
   all clients build the same map; vary `multiplayerSeed` per lobby later if
   you want.
