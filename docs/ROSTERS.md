# Nation Rosters, Buildings & Upgrades — Design Sheet

This is the complete faction design to enter into the four `NationData`
assets (costs/stats are starting values — tune after playtests). Every
mechanic referenced here is implemented in code; you supply prefabs and type
the numbers into the inspector. Category legend: **[I]**nfantry, **[A]**nimal,
**[V]**ehicle, **[W]**orker, **[AV]**atar — set on both the roster entry and
the prefab's `Unit.category`.

## How everyone earns silver (implemented)

| Source | Who | How |
|---|---|---|
| Kingdom tax | all factions | `MatchManager` income tick (default 25 / 10s) |
| Workers | all factions | `ResourceCollector` units haul loads from `ResourceNode`s (fish shoals, crystal deposits, coal seams, spirit groves) to any `ResourceDropoff` building |
| Economy buildings | all factions | `IncomeBuilding` trickle; the strong ones must be placed next to a matching node and drain it |
| Village tribute | whoever holds the village | `Village` control radius (default 15 / 8s) |
| Spirit bounties | whoever lands the kill | `Unit.killBounty` — set 40 on dark spirits, 15 on friendly ones ("spirit energy") |
| Survival waves | player | existing per-wave reward |

The AI earns through the exact same systems (its commander trains workers,
holds villages, and its treasury lives in `Faction.credits`). In the HUD,
relabel the credits counter "**Silver**" — the show's coinage.

## Population (implemented)

Every unit occupies population (`Unit.populationCost` — suggested: infantry/
worker **1**, animal **2**, vehicle **3**, Avatar **5**). Base cap is 25;
each `PopulationHousing` building adds more while it stands (put +20 on the
command center, +10 on dedicated housing). Training blocks at the cap for
players AND AI, so raiding enemy housing genuinely shrinks their army.
Wire a TMP label to `PopulationHUD` next to the Silver counter ("23 / 45").

---

## Air Nomads — mobility & evasion

**Units**

| Unit | Cat | Cost | Build | Notes / model source |
|---|---|---|---|---|
| Air Acolyte | W | 40 | 4s | forager; `ResourceCollector` |
| Airbender Monk | I | 50 | 3s | your existing AirbenderUnit; auto-gains `AirbenderMobility` — air-scooter sprint on long ground moves, and STAFF GLIDER flight on very long orders (soars over water/hills/buildings, steers around mountains, lands on the NavMesh) |
| Glider Warrior | I | 80 | 5s | faster (higher NavMeshAgent speed), lower HP |
| Winged Lemur | A | 30 | 2s | cheap fast scout, tiny HP, no attack (omit AttackController); `VisionSource` 30 — your fog-of-war eyes |
| Sky Bison | A | 200 | 12s | flying tank — big HP, knock-back wind attack; reuse your bison models! `VisionSource` 25 (sees far over the fog) |
| War Glider | V | 150 | 9s | "vehicle" of a nation with no machines: multi-monk glider, hit-and-run |
| **Avatar (Air-born)** | AV | 600 | 25s | see Avatar spec below |

**Buildings** (all get `Structure`; production ones also get `UnitSpawner`)

| Building | Cat | Cost | Notes |
|---|---|---|---|
| Air Temple | Command | — | command center + `ResourceDropoff` |
| Meditation Pavilion | Economy | 120 | `IncomeBuilding` 8/6s, stronger near a **Spirit Grove** node |
| Bison Stable | Production | 180 | trains Lemur / Sky Bison |
| Wind Cannon Pagoda | Defense | 140 | `DefenseTower` element **Air**: shots knock enemies back; special is a tornado that damages and scatters the pack |
| Nomad Dormitories | Special | 100 | `PopulationHousing` +10 — more beds, more monks |
| Sky Mooring | Special | 180 | `AirSupplyPost`: bison couriers FLY silver from your command center to this outpost — farther = bigger pay; couriers can be shot down |
| Spirit Shrine | Special | 250 | unlock flavor: place near Spirit Wilds; passive income + heals nearby units (add `Healer` with big radius) |

> **Mountain perches (Air only)**: every Air Nomad building can be placed on
> **steep mountainsides**, out of reach of ground armies (steep rock is off
> the NavMesh — only flyers and ranged fire threaten a perch; your own
> ground units can't reach it either, so perch support buildings, not
> dropoffs you need workers at). Perched Air buildings switch to a different
> design: give each prefab a disabled child named **`MountainModel`**
> (cliff-hugging/stilted architecture) and the game swaps it in
> automatically; without one, a greybox platform + struts is generated.
> Other nations' buildings refuse steep slopes entirely (a per-entry
> `mountainSite` override exists on BuildingEntry for exceptions).
> **Counterplay**: Earth's expensive *Mountain Breaker* upgrade lets
> earthbenders at the mountain's foot shake perched buildings apart with
> tremors — perches are safe from swords, not from the mountain itself.

**Upgrades**: see the **Upgrade Tech Trees** section below (Air tree).

## Water Tribe — sustain & control

**Units**

| Unit | Cat | Cost | Build | Notes |
|---|---|---|---|---|
| Fisherman | W | 40 | 4s | `ResourceCollector`; loves Fish Shoals |
| Waterbender Warrior | I | 55 | 3s | balanced fighter |
| Healer | I | 90 | 6s | `Healer` component — the nation's identity |
| Polar Bear Dog | A | 130 | 8s | fast bruiser cavalry |
| Ice Cutter | V | 170 | 10s | waterbending-driven warship (drives on shore/ice = your NavMesh) |
| **Avatar (Water-born)** | AV | 600 | 25s | |

**Buildings**: Ice Palace (command + dropoff) · Fishing Dock (Economy 130,
`IncomeBuilding` requires **Fish Shoal** within 12) · Healing Hut (Production:
Healers; give the hut itself a small `Healer` aura) · Ice Spike Tower
(Defense 140 — `DefenseTower` element **Water**: shots slow, special is a
wave that hits and slows everything near the target) · Shipyard (Production:
Ice Cutters, 200).

Add a **Tribal Lodge** (Special 100, `PopulationHousing` +10).

**Upgrades**: see the **Upgrade Tech Trees** section below (Water tree —
the Healing branch vs. the Ice branch is the tribe's defining choice).

## Earth Kingdom — toughness & siege

**Units**

| Unit | Cat | Cost | Build | Notes |
|---|---|---|---|---|
| Miner | W | 40 | 4s | `ResourceCollector`; loves Crystal Deposits |
| Earthbender Soldier | I | 60 | 4s | high HP frontliner |
| Boulder Hurler | I | 110 | 7s | siege: raise attackDistance/damage, slow |
| Ostrich Horse Rider | A | 100 | 6s | fast cavalry |
| Badgermole | A | 220 | 14s | living siege engine, huge HP & building damage |
| Earth Tank | V | 190 | 11s | bender-crewed stone rig |
| **Avatar (Earth-born)** | AV | 600 | 25s | |

**Buildings**: Earthen Citadel (command + dropoff) · Crystal Mine (Economy
140, requires **Crystal Deposit**) · Barracks (Production) · Badgermole
Burrow (Production: animals) · Rock Launcher Tower (Defense 150 — `DefenseTower` element **Earth**: every
boulder splashes; special is a massive boulder, 2x damage full splash) ·
**Stone Wall** (Defense 60 — Structure + NavMeshObstacle (carve ON) +
`RequiresBenderPresence`: can only be raised while you have living
earthbenders; chain segments with R-rotation) · **Earth Gate** (Defense 120 —
wall recipe + `Gate`: slides into the ground for friendlies, seals against
enemies, and won't operate at all if your last earthbender falls) ·
**Stone Tenements** (Special 100, `PopulationHousing` +10).

**Upgrades**: see the **Upgrade Tech Trees** section below (Earth tree —
Lavabending and Metalbending live in different research buildings and lock
each other out; Mountain Breaker is shared by both paths).

## Fire Nation — aggression & machines

**Units**

| Unit | Cat | Cost | Build | Notes |
|---|---|---|---|---|
| Coal Engineer | W | 40 | 4s | `ResourceCollector`; loves Coal Seams |
| Firebender Soldier | I | 55 | 3s | your existing FirebenderUnit |
| Fire Lancer | I | 85 | 5s | spear + flame, anti-animal |
| Komodo Rhino Rider | A | 120 | 7s | shock cavalry |
| Tundra Tank | V | 180 | 10s | armored crawler |
| War Balloon | V | 240 | 15s | late-game flyer, strong vs buildings |
| **Avatar (Fire-born)** | AV | 600 | 25s | |

**Buildings**: Fire Citadel (command + dropoff) · Coal Refinery (Economy 140,
requires **Coal Seam**) · War Academy (Production: infantry) · War Factory
(Production: tanks/balloons, 220) · Flame Turret (Defense 140 — `DefenseTower` element **Fire**: shots ignite
targets (burn over time); special is a lightning strike for 3x damage).

Add **Garrison Quarters** (Special 100, `PopulationHousing` +10).

**Upgrades**: see the **Upgrade Tech Trees** section below (Fire tree —
the Lightning path vs. the Inferno path).

---

## The Avatar (all nations) — `AvatarUnit` component

- **Cost ~600, one per player** — enforced in code (build buttons and the
  multiplayer server both refuse a second while yours lives). If your Avatar
  dies you may eventually train a "reincarnated" one (that's just building it
  again — thematic!).
- **Stats**: ~400 HP, 25 damage, normal speed. Category `Avatar`.
- **Bends all four elements**: press **T** while selected to cycle
  Air→Water→Earth→Fire. Assign one attack VFX per element on the component;
  the active element also decides which biome zones empower it (bend Water on
  a glacier!).
- **Avatar State** (energy-powered, three tiers): **G** = 5s quick surge
  (30 energy, ~1.75× damage), **Shift+G** = 10s long surge (55 energy,
  ~2.25×), **Ctrl+G** = 20s ULTIMATE (100 energy, ~3×). While active the
  Avatar wields all four elements simultaneously (every element VFX lit)
  and takes reduced damage. Energy regenerates over time, faster in combat.
  Left unsupervised, the Avatar fights autonomously — cycling elements
  between attacks and bracing into defensive stances. Assign a glow aura VFX.
- **Energy bending**: the ONLY unit that can tame wild spirits
  (`Tameable.requiresEnergyBender` is on by default). Right-click a friendly
  spirit to befriend it; dark spirits must first be beaten below half health.
- Prefab recipe: everything a bender prefab has + `AvatarUnit` + 4 element
  VFX children + aura. Give each nation a themed model (element of origin is
  cosmetic).

## Superweapons (`Superweapon` component) — implemented

Put one on each nation's Special building. Charges from zero over 4 minutes;
press **P** when charged, then click the target (AI fires its own at your
base automatically):

| Nation | Building | Power |
|---|---|---|
| Fire | War Sanctum (or Fire Citadel) | **Comet Barrage** — 3 waves of falling fire: heavy damage + burn, hurts buildings too |
| Water | Moon Shrine | **Flash Freeze** — every enemy in the area frozen near-solid for 8s |
| Air | Spirit Shrine | **Great Storm** — damages and hurls every enemy away from the epicenter |
| Earth | Deep Sanctum | **Stone Rampart** — earthbends a ring of stone walls out of the ground (assign the wall prefab) |

Also implemented: **sell** any building for half its cost (hover + X) and
**worker repairs** (right-click a damaged friendly building with workers
selected; ~0.5 silver per HP).

**Shield mastery upgrades**: benders' Q shields are upgradable per bender
type — tick `improvesShields` on an UpgradeData and restrict it to that
nation's bender (buying a level empowers EVERY bender of that type at once,
current and future). Suggested per nation, 3 levels, 175 base: Air
"Unbending Wind", Water "Deep Ice", Earth "Mountain's Patience", Fire
"Inner Flame" — each level: +15% shield strength (reduction/absorb/aura),
+20% duration, -10% cooldown.

## Upgrade Tech Trees (implemented)

Upgrades form **branching trees** with three rules, all enforced in code
(`UpgradeManager.CanPurchase` — locked buys explain themselves in the UI):

1. **Prerequisites** (`prerequisiteUpgradeIds`): a node needs its parent at
   level 1+ first.
2. **Exclusive branches** (`exclusiveWithUpgradeIds`): buying one side
   permanently SEALS the other — real strategic identity per match.
3. **Research buildings** (`requiredBuildingKeyword`): branch nodes are
   studied in a specific hall — you must OWN a building whose name contains
   the keyword. Different buildings anchor different branches.

The **`Legends ► Bootstrap`** editor menu creates every asset below
automatically. Legend: ⛔ = mutually exclusive, 🏛 = research building.

### Air — Way of the Sky vs. Way of the Storm

| Node | Lv | Cost | Needs | Effect |
|---|---|---|---|---|
| Tempest Training | 3 | 150 | — | +15% damage (infantry) |
| Gale Stride | 2 | 200 | — | +10% speed (everything) |
| Unbending Wind | 3 | 175 | Tempest Training | stronger wind shields (Q) |
| **Staff Gliders** | 1 | 250 | Gale Stride · 🏛 Pavilion · ⛔ Tornado Summoning | airbenders unlock TRUE FLIGHT on very long orders (scooter is innate; flight is learned) |
| **Tornado Summoning** | 1 | 450 | Unbending Wind · 🏛 Pavilion · ⛔ Staff Gliders | airbenders periodically conjure tornadoes on their targets (AoE + scatter) |
| Bison Plate Barding | 2 | 200 | 🏛 Stable | +20% HP (animals — armored bison) |

### Water — the Healing path vs. the Ice path

| Node | Lv | Cost | Needs | Effect |
|---|---|---|---|---|
| Moonlight Discipline | 3 | 150 | — | +15% damage (infantry) |
| Glacial Hide | 2 | 150 | — | +15% HP (animals/vehicles) |
| Deep Ice | 3 | 175 | — | stronger ice shields (Q) |
| **Healing Waters** | 3 | 200 | Moonlight Discipline · 🏛 Healing Hut · ⛔ Everfrost | every waterbender heals nearby allies (2 HP/s per level) |
| Frozen Grasp | 2 | 220 | Moonlight Discipline · 🏛 Moon Shrine | freezes/chills last +35% per level |
| **Everfrost** | 1 | 500 | Frozen Grasp · 🏛 Moon Shrine · ⛔ Healing Waters | freeze enemies solid ANYWHERE — no water source needed |
| Reinforced Hulls | 2 | 200 | 🏛 Shipyard | +20% HP (warships) |

### Earth — the Molten path vs. the Metal path (+ the shared Mountain path)

| Node | Lv | Cost | Needs | Effect |
|---|---|---|---|---|
| Neutral Jing | 3 | 150 | — | +15% HP (infantry) |
| Master Sculpting | 3 | 150 | — | +15% damage (infantry) |
| Mountain's Patience | 3 | 175 | — | stronger stone shields (Q) |
| Seismic Sensing | 2 | 180 | — | +30% sight through fog per level |
| **Lavabending** | 1 | 500 | Master Sculpting · 🏛 Barracks · ⛔ Metalbending | strikes IGNITE victims, splash molten rock on packed enemies, melt buildings (+40%) |
| **Metalbending** | 1 | 500 | Neutral Jing · 🏛 Deep Sanctum · ⛔ Lavabending | tear machines (+60%) and fortifications (+30%) apart |
| **Mountain Breaker** | 1 | 650 | Seismic Sensing (either path may take it) | tremors shake apart hostile buildings PERCHED on mountainsides within ~25m |
| Reinforced Hide Plates | 2 | 200 | — | +20% HP (beasts/tanks) |
| Siege Engines | 2 | 220 | — | +20% damage (tanks) |

### Fire — the Lightning path vs. the Inferno path

| Node | Lv | Cost | Needs | Effect |
|---|---|---|---|---|
| Sozin's Doctrine | 3 | 160 | — | +15% damage (infantry) |
| Forced March | 2 | 150 | — | +10% speed (infantry/cavalry) |
| Lightning Mastery | 2 | 220 | Sozin's Doctrine · 🏛 War Academy | firebenders' bolts hit +10% harder per level |
| **Lightning Redirection** | 2 | 350 | Lightning Mastery · 🏛 War Academy · ⛔ Flame Dive | catch enemy lightning (bolts AND tower strikes) and hurl it back — 25%/50% |
| Inner Flame | 3 | 175 | — | stronger flame shields (Q) |
| **Flame Dive** | 1 | 450 | Inner Flame · 🏛 War Academy · ⛔ Lightning Redirection | firebenders LEAP onto foes and slam down a burning fire ring |
| Drill Plating | 2 | 200 | 🏛 War Factory | +20% HP (war machines) |
| **Dragon's Breath Nozzles** | 1 | 400 | Drill Plating · 🏛 War Factory | war machines vent burning fuel — anything close is continuously scorched |

### The Avatar (every nation) — Spirit vs. Fury

| Node | Lv | Cost | Needs | Effect |
|---|---|---|---|---|
| **Spirit Communion** | 2 | 300 | ⛔ Elemental Fury | Avatar energy regenerates +30% per level (more Avatar States) |
| **Elemental Fury** | 2 | 300 | ⛔ Spirit Communion | Avatar State damage +15% per level (bigger Avatar States) |

Every nation also gets **Reinforced Battlements** (3 lv, 175, Building
category): +15% tower damage & building HP per level. All bender-restricted
nodes use `restrictToUnitTypes` so animals and machines don't learn them.
Note: since Lavabending ⛔ Metalbending, their damage bonuses never coexist
in normal play (campaign chi can still grant odd combinations — they stack
harmlessly).

**Tower & wall upgrades**: give every nation a **Reinforced Battlements**
upgrade (3 levels, 175 base, categories = **Building**): per level
+15% tower damage (damageBonus), +15% building HP (healthBonus). For
Building-category upgrades, `speedBonus` means **fire rate** and
`sightBonus` means **tower range** — e.g. a Fire-only "Gunnery Drills"
(speedBonus 0.15) makes Flame Turrets shoot faster. Applies to standing
buildings and everything built afterward.

## Elemental climates (`BiomeZone`) — implemented modifiers

| Climate | Empowers | Weakens |
|---|---|---|
| Volcanic | Fire +25% | Water −20% |
| Glacier | Water +25% | Fire −20% |
| River Lands | Water +15% | Fire −10% |
| Windy Peaks | Air +25% | Earth −15% |
| Stone Quarry | Earth +25% | Air −15% |
| Spirit Wilds | Spirits +25% | all nations −10% |

Element resolution: Avatar = current element; benders = their discipline;
animals/vehicles/workers = their nation's element (a Tundra Tank struggles on
a glacier). `MapGenerator` also seeds matching resource nodes inside biomes —
coal in volcanic fields, fish along rivers/glaciers, crystals in quarries,
spirit groves in the wilds — so climates are worth fighting over twice.
