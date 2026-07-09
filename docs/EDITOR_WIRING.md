# EDITOR WIRING — The Master Checklist

**This is the one document to keep open while finishing the game in the Unity
Editor.** It consolidates every system on this branch into one ordered
pass: do the phases top to bottom and check items off. Numbers and full
faction designs live in [`ROSTERS.md`](ROSTERS.md); multiplayer details in
[`MULTIPLAYER_SETUP.md`](MULTIPLAYER_SETUP.md).

Conventions used below:
- **"Recipe"** = the exact component list for a prefab. Order doesn't matter.
- *(auto)* = the code adds this itself at runtime; you don't need to.
- Every scene name you type into an inspector must match a scene in
  **File > Build Settings** exactly.

---

## Phase 0 — Project opens clean

- [ ] Open in Unity **6000.0.43f1** with internet available. Package Manager
      fetches `com.unity.netcode.gameobjects` and
      `com.unity.multiplayer.playmode` (already in the manifest).
- [ ] Console shows **zero compile errors**. (If a package failed to
      download: Window > Package Manager > refresh icon.)
- [ ] Sanity run: open `Level1_Scene`, press Play — the original Air-vs-Fire
      game must behave exactly as before. Nothing new is required for this.

## Phase 1 — Layers, tags, physics

Check **Project Settings > Tags and Layers**. You already use three layers —
confirm their exact names in the `UnitSelectionManager` inspector (fields
Clickable / Ground / Attackable). You need:

| Layer | Used for | Referenced by |
|---|---|---|
| `Clickable` (yours may differ) | your own selectable units | UnitSelectionManager.clickable, `Tameable.tamedLayerName` |
| `Ground` | terrain, walkable floor | UnitSelectionManager.ground, UnitMovement.ground, BuildingPlacer.groundMask, MapGenerator.groundMask |
| `Attackable` | enemy units/buildings, wild spirits | UnitSelectionManager.attackable |

- [ ] All new enemy-capable prefabs (spirits, buildings, all four nations'
      units when enemy-owned) live on the **Attackable** layer; your
      selectable units on **Clickable**. One prefab serves both sides — the
      layer just decides what *you* can click; ownership filtering is
      handled by code.
- [ ] Type `Tameable.tamedLayerName` = your clickable layer's exact string
      (default "Clickable") on spirit prefabs.

## Phase 2 — Data assets (the game's "database")

Create folder `Assets/Nations/`.

1. **Upgrades first** (Assets > Create > Legends > Upgrade). Make the
   upgrade assets listed per nation in ROSTERS.md. Field-by-field:
   - `upgradeId`: unique string, e.g. `earth_sensing` (never reuse across assets)
   - `baseCost` / `maxLevel`: from the tables
   - `damageBonus` / `healthBonus` / `speedBonus` / `sightBonus`: fraction
     per level (0.15 = +15%). Seismic Sensing uses **sightBonus** only.
   - `categories`: which unit categories it affects (empty = all)
2. **Four NationData assets** (Assets > Create > Legends > Nation Data):
   `AirNation`, `WaterNation`, `EarthNation`, `FireNation`.
   - Identity: nation enum, display name, description (shows in menu),
     theme color, emblem sprite.
   - `units`: fill from ROSTERS.md **in a stable order** — UI buttons and
     starting squads reference entries by index. Slot 0 MUST be the basic
     infantry (it's the starting-squad unit). Put the Avatar LAST. Set each
     entry's `category` correctly (Worker/Animal/Vehicle/Avatar matter to
     the AI, upgrades and the one-Avatar rule).
   - `buildings`: economy/production/defense entries from ROSTERS.md.
   - `commandCenterPrefab`: that nation's CC prefab (Phase 3).
   - `upgrades`: drag in that nation's upgrade assets.
3. **NationDatabase** (Assets > Create > Legends > Nation Database) saved as
   **exactly** `Assets/Resources/NationDatabase.asset` (create the
   `Resources` folder if needed) with all four NationData in its list.
   Everything loads it by that path — wrong location = "NationDatabase.asset
   not found" warnings.

## Phase 3 — Prefab recipes

Build these in a `Assets/Prefabs/` hierarchy (or wherever you like). Copy
the existing `AirbenderUnit`/`FirebenderUnit` as your starting point — they
already have the animator, health bar child, indicator child, colliders and
NavMeshAgent set up correctly.

### 3.1 Bender / soldier (per nation)
Recipe: model + Animator (reuse the bender controller: needs `isMoving`,
`isFollowing`, `isAttacking`, `Die` params) · NavMeshAgent · colliders
(one solid + one trigger for target acquisition) · `Unit` (type + category
Infantry, healthTracker wired to the HealthBar child) · `UnitMovement`
(**disabled**; selection enables it) · `AttackController` (attack VFX in
flamethrowerEffect slot) · `EnemyAI` (idles when player-owned) ·
HealthBar + Indicator children.
- [ ] WaterbenderUnit, EarthbenderUnit (duplicate + reskin + retype)
- [ ] Optional specialists: Glider Warrior, Boulder Hurler, Fire Lancer,
      Healer (add `Healer` component) — stats per ROSTERS.md

### 3.2 Worker (per nation)
Recipe: bender recipe **minus** AttackController/EnemyAI, **plus**
`ResourceCollector`. `Unit.category = Worker`, low HP. Optional basket child
→ Carry Visual slot.

### 3.3 Animals & vehicles
Same recipe as benders with different models/stats; `Unit.category` =
Animal or Vehicle. Scouts/flyers get a `VisionSource` (lemur 30, bison 25,
war balloon 25). Big siege animals: raise `AttackController.unitDamage` and
`attackDistance`.

### 3.4 The Avatar (one per nation)
Recipe: bender recipe (HP ~400, damage ~25) + `AvatarUnit` + four disabled
child VFX (air/water/earth/fire) assigned to the element slots + aura VFX
for Avatar State + `VisionSource` ~20. `Unit.category = Avatar`. Roster
entry: cost ~600, category **Avatar**, last slot.

### 3.5 Spirits
- **FriendlySpirit**: model/VFX body · NavMeshAgent · `Unit` (type Spirit,
  killBounty 15) · `Spirit` (alignment Friendly) · `Tameable`
  (maxHealthFractionToTame 1, requiresEnergyBender ON) · layer Attackable.
- **DarkSpirit**: same, plus `AttackController` (+ spooky VFX, trigger
  collider) and the shared Animator params; `Spirit` alignment Dark;
  `Tameable.maxHealthFractionToTame = 0.5`; killBounty 40.
  *(auto: FactionMember, EnemyAI, minimap-hiding under fog)*

### 3.6 Villagers & villages
- **Villager**: small model · NavMeshAgent · `Unit` (type Villager, HP 30) ·
  `Villager`. No attack components.
- **Village**: hut models under an empty root + `Village` (assign villager
  prefab, count, radii). *(auto: MinimapPOI)*

### 3.7 Spirit portal
Empty root + portal VFX + `SpiritPortal` (assign both spirit prefabs, spawn
interval ~45s, max alive 3). *(auto: MinimapPOI)*

### 3.8 Resource nodes (4)
Model + collider + `ResourceNode` (type + totalAmount ~1500):
FishShoal, CrystalDeposit, CoalSeam, SpiritGrove.
*(auto: MinimapPOI)*

### 3.9 Buildings (per nation, see ROSTERS.md)
Recipe: model + solid collider + `Structure` (HP, healthTracker optional) +
role component:
- Economy: `IncomeBuilding` (tick amount/interval; tick **Requires Nearby
  Node** + type for mines/docks/refineries)
- Production: `UnitSpawner` (buttons call `QueueRosterUnit`)
- Defense: `DefenseTower` — set the **Element** per nation (Fire = burn +
  lightning special, Air = knockback + tornado, Water = slow + wave,
  Earth = splash boulders + massive boulder). Assign a Shot Effect VFX and
  a bigger Special Shot Effect VFX, plus `VisionSource` ~20 so towers watch
  the fog. Every 4th shot fires the special (Special Every N Shots).
- Any building that should accept worker deliveries: `ResourceDropoff`
- Layer: Attackable. 
- [ ] **Command centers** for Water/Earth: duplicate AirNationTemple /
      FireNationCitadel, reskin; keep `CommandCenter` + add
      `ResourceDropoff`.

## Phase 4 — Main menu scene

Open `MainMenuScene`:

1. **Nation select panel** under the Canvas (`NationSelectController` on it):

   | UI element | Wire to |
   |---|---|
   | Air/Water/Earth/Fire buttons | `NationSelectController.SelectNation(0/1/2/3)` |
   | Survival / Skirmish buttons | `SetMode(0)` / `SetMode(1)` |
   | AI count slider (1–3) | `SetAIOpponentCount` |
   | Map buttons (one per scene) | `SelectMap(0..n)` |
   | Start button | `StartGame` |
   | Back button | `MainMenuController.CloseNationSelect` |
   | optional TMP labels | selectedNationLabel / description / selectedMapLabel |

   Inspector fields: **Level Scene Name** = your default gameplay scene;
   **Map Scene Names** = every playable map scene (exact names);
   **Randomize Seed Each Match** = ON.
2. On `MainMenuController`: assign **Nation Select Panel**. (Unassigned =
   old behavior.)
3. Multiplayer panel: see Phase 8.

## Phase 5 — Gameplay scene(s)

Do this for Level1 (retrofit) and/or a duplicated `Skirmish1_Scene`:

Keep from Level1: GameManager (+ win/lose panels), UnitSelectionManager,
UnitSelectionBox, PlayerResources HUD, CursorManager, camera rig, side
panel, SoundManager, music.

Add:
- [ ] `MatchController` empty GameObject → `MatchManager`: assign
      NationDatabase; **Spawn Bases At Start Locations** ON for skirmish
      scenes / OFF for the hand-built Level1; income defaults are fine.
- [ ] 2–4 empty GameObjects with `StartLocation` (index 0..3) on baked
      NavMesh, far apart. (Skirmish scenes only.)
- [ ] `BuildingPlacer` empty GameObject: groundMask = your Ground layer.
- [ ] `FogOfWar` empty GameObject at the map center: mapSize to cover the
      terrain, defaults otherwise. Optional: a URP Unlit-Transparent
      material in **Overlay Material** (otherwise a fallback shader is
      used). Overlay height above your tallest unit but below the camera.
- [ ] Minimap: on the HUD canvas add a RawImage in a corner (~200×200) and
      a `MinimapController` (assign the RawImage).
- [ ] Scatter neutrals: 2–4 villages, 4 resource nodes near/between bases,
      a couple of spirits, 1–2 **spirit portals**, and biome zones (Phase 6).
- [ ] Side panel wiring:
      - Unit buttons → CC's `UnitSpawner.QueueRosterUnit(0..n)` (indices =
        NationData roster order; include a button for the Avatar's slot)
      - Build tab buttons → `BuildingPlacer.BeginPlacement(0..n)`
      - Upgrade buttons → an `UpgradePurchaser.Purchase(0..n)` on the panel
      - Rename the credits label "**Silver**"
- [ ] Survival-only scenes keep WaveManager + EnemyCommandCenter + WaveUI
      (auto-disabled in skirmish mode); pure skirmish scenes can delete them.
- [ ] Bake NavMesh; add scene to Build Settings.

## Phase 6 — Biome zones (elemental climates)

Make six zone prefabs: empty root + `BiomeZone` (climate + radius ~25) +
decorative props as children (lava rocks/smoke, ice shards, reeds/water,
swirling leaves, boulders, glowing flora). Gameplay comes from the
component; props are dressing. Drop several into each handmade map —
ideally with the matching resource node inside (coal in volcanic, fish by
rivers/glaciers, crystal in quarries, spirit grove + portal in spirit
wilds). Buff/debuff table is in ROSTERS.md.

## Phase 7 — Random map scene

1. New scene `RandomMap_Scene`: large terrain (Ground layer) + baked-ish
   NavMesh via a **NavMeshSurface** component on the terrain, plus the full
   Phase 5 rig (MatchManager with Spawn Bases ON, FogOfWar, minimap,
   BuildingPlacer, HUD...). **No** hand-placed StartLocations needed.
2. Empty GameObject → `MapGenerator`:
   - groundMask = Ground; mapSize to fit the terrain; edge margin ~25
   - biomeZonePrefabs = your six zone prefabs; biomeCount ~5
   - resourceNodePrefabs = the 4 node prefabs; villages/spirits/portal
     prefabs + counts
   - navMeshSurface = the terrain's NavMeshSurface (rebuilds after placing)
3. Add the scene to Build Settings and to the menu's **Map Scene Names**.
   Every match rolls a new seed → new layout; same seed = same map.

## Phase 8 — Multiplayer

Follow `MULTIPLAYER_SETUP.md` (NetworkManager + UnityTransport +
NetworkBootstrap in the menu; player prefab with `RTSNetworkPlayer`;
NetworkObject + NetworkTransform + NetworkAnimator + `NetworkUnit` on unit
prefabs; `NetworkCommandCenter` on CCs; **`NetworkStructure` on regular
buildings**; register everything in the Network Prefabs list;
`NetworkMatchManager` + StartLocations in the level scene). Multiplayer
extras already handled by code: building placement, upgrades and Avatar
uniqueness all validate on the server; fog of war is per-client
automatically; random maps use the shared `multiplayerSeed`.

## Phase 10 — Campaign mode

The 25-level story campaign ("The Rupture") is code-driven — dialogue,
bosses, progression and saves all bootstrap themselves. The only wiring is
the campaign menu panel and one final-boss prefab in Resources. Full
instructions + test checklist: **[`CAMPAIGN.md`](CAMPAIGN.md)**.

## Phase 9 — Full test matrix

- [ ] Level1 plays like before (survival waves, win + lose)
- [ ] Nation select: each nation's build buttons produce that nation's units
- [ ] Skirmish vs 2 AI: they harvest, expand their army, wave-attack, buy
      upgrades, eventually field their own Avatar
- [ ] Economy: worker harvests node → silver ticks up; refinery only places
      next to its node (ghost red elsewhere); village tribute; dark-spirit
      bounty
- [ ] Buildings: place, rotate (R), cancel (Esc); tower fires; enemies can
      destroy your buildings
- [ ] Upgrades: buy level → existing units hit harder; new units too
- [ ] Avatar: only one buildable; T cycles elements (VFX changes); G Avatar
      State; ONLY the Avatar can tame spirits (order a normal unit → nothing)
- [ ] Fog: map starts dark; explored areas stay dim; enemies vanish out of
      sight; bison/lemur reveal more; Seismic Sensing upgrade widens
      earthbender vision
- [ ] Minimap: discovered nodes/villages/portals stay marked; friendly dots
      green; enemies only while visible; enemy base appears once scouted
- [ ] Spirit portal keeps releasing spirits up to its cap
- [ ] Biomes: same fight in a volcano vs a glacier ends differently
- [ ] Random map twice → different layouts; bases/nodes/villages all on
      the NavMesh
- [ ] Multiplayer: host+join, versus and co-op; client can build units,
      place buildings, buy upgrades; each player fogged separately

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| "NationDatabase.asset not found" | asset not at `Assets/Resources/NationDatabase.asset` |
| Build button does nothing | roster index mismatch, or empty prefab slot in NationData |
| Units spawn but won't move | NavMesh not baked / spawn point off-mesh |
| Can't select new units | prefab not on the Clickable layer |
| Enemies never visible | they're being fogged: your units have tiny sight, or FogOfWar mapSize doesn't cover the terrain |
| Fog overlay invisible/pink | assign a URP Unlit Transparent material to FogOfWar.overlayMaterial |
| Tamed spirit unselectable | `tamedLayerName` doesn't match your clickable layer name |
| Ghost never turns green | groundMask wrong, or economy building far from its required node |
| AI does nothing | its NationData has empty prefabs, or no MatchManager in scene |
| MP: "prefab has no NetworkObject" errors | prefab not network-ready or not in Network Prefabs list |
| MP client sees no bases | scene missing NetworkMatchManager or StartLocations |
