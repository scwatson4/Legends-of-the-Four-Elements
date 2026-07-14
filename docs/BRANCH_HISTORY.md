# Branch History — `claude/four-nation-multiplayer-rts-47has0`

The complete chronicle of this branch: what each commit added and why.
Built entirely as **code + docs** (no Unity Editor in the loop), designed so
the original Level1 scene keeps working at every commit. Total: ~13,600
lines added across ~100 files, 20 feature commits (this doc's commit makes
one more).

**Reading order for newcomers:** `README.md` → `EDITOR_WIRING.md` (the
master wiring checklist) → `ROSTERS.md` (all the numbers) → this file for
the why-behind-what.

---

## Commit log (oldest → newest)

### 1. `9506e5e` — Four-nation faction system, neutrals, nation select, skirmish AI, multiplayer foundation
The architectural commit. Replaced the hardcoded two-value `Team` enum with
a full faction system (`FactionManager` diplomacy, team groups for
co-op/versus, `FactionMember` ownership, legacy fallback so old scenes
work). Added `NationData`/`NationDatabase` ScriptableObjects, `GameSetup`
menu-to-level settings, `MatchManager` multi-faction victory,
`AICommander` skirmish brains, villages/villagers, Friendly/Dark spirits
with the `Tameable` befriending mechanic, the nation-select controller, and
the entire Netcode-for-GameObjects multiplayer layer (host/join, per-player
factions, server-authoritative units, command relay RPCs). Also the first
three docs: FINISHING_GUIDE, MULTIPLAYER_SETUP, README overhaul.

### 2. `b7315f4` — ATLA economy, full rosters/buildings/upgrades, Avatar hero, biome maps
The "make it Avatar" commit. One `Economy` API for every wallet (fixed a
real bug: AI spawners were spending the player's money). Resource nodes +
`ResourceCollector` workers + `IncomeBuilding`s + kill bounties.
`NationData` grew unit categories, building rosters and upgrade lists;
`BuildingPlacer` ghost construction; `DefenseTower`s; `UpgradeData`/
`UpgradeManager` faction-wide upgrade levels; the Waterbender `Healer`.
The first `AvatarUnit` (element cycling, Avatar State v1, exclusive energy
bending, one-per-player). `BiomeZone` elemental climates and the seeded
`MapGenerator`. ROSTERS.md design sheet.

### 3. `0bd2675` — Fog of war, exploration minimap, spirit portals, master wiring guide
Grid-based `FogOfWar` (hidden/explored/visible, renderer hiding,
`VisionSource` sight ranges, sightBonus upgrades like Seismic Sensing),
`MinimapController` with permanent POI discovery, `SpiritPortal` spawners,
and EDITOR_WIRING.md — the phase-by-phase master checklist.

### 4. `3962688` — The 25-level campaign
"The Rupture": five chapters fully written in code (`DefaultCampaign.cs`)
with dialogue for every level. `CampaignManager` orchestration,
`CampaignProgress` persistent saves + chi + permanent upgrades,
`CorruptedAvatar` chapter bosses (phase-shifting elements, redemption into
your arsenal, F1–F4 summons), `DarkSpiritBoss` finale (invulnerable unless
four Avatars stand together), typewriter `DialogueUI`, self-populating
campaign menu. CAMPAIGN.md.

### 5. `032603b` — Campaign scratch starts + escalating waves
Every mission now starts as Avatar + builder + seed silver — you found
your own base (B key, `commandCenterCost`). Enemy pressure became scripted
waves that grow per wave and per chapter; AI starting units defend instead
of rushing; defeat watchdog for baseless play.

### 6. `52eb43c` — Elemental towers + RTS control feel
Towers gained per-element behavior with a special every 4th shot (Fire
burn/lightning, Air knockback/tornado, Water slow/wave, Earth splash).
`BurnEffect`/`SlowEffect` status system. Control groups (Ctrl+1–9),
attack-move (F), double-click select-type; camera zoom moved to −/=.
CONTROLS.md.

### 7. `6a84fa0` — Rally points, difficulty, transports, audio barks, embodiment, voice command
Rally points on production buildings; Easy/Normal/Hard scaling enemy
economy/waves only; Sky Bison/War Balloon garrisons (board/unload/bail-out);
SoundManager bark + stinger slots; **`EmbodimentController`** — possess any
unit in first person (H), the Quest 3 gameplay layer with its XR HeadAnchor;
**`VoiceCommander`** — hold-V OpenAI speech → roster-aware intent →
reinforce/build/train. VR_AND_VOICE.md.

### 8. `b8a82e2` — Avatar energy system + randomized attack styles
Avatar State became energy-powered with three tiers (G 5s / Shift+G 10s /
Ctrl+G 20s ultimate), all four elements blazing at once, damage reduction,
and autonomous element-cycling + defensive stances when unsupervised.
`AttackStyleSet`: every unit alternates 3–4 themed techniques with
burn/slow/knockback riders. AI_WIRING_PLAYBOOK.md (Claude + Unity MCP
prompts) and the Quest 3 performance budget.

### 9. `491959a` — Population cap, healing upgrade, earthbent walls & gates
Halo Wars population (unit costs, `PopulationHousing` buildings raise the
cap, enforced for player/AI/server, `PopulationHUD`). `grantsHealing`
upgrades with per-unit-type restriction (Healing Waters). Stone Walls and
auto-opening Earth Gates that require living earthbenders
(`RequiresBenderPresence`, `Gate` NavMesh carving).

### 10. `5664f4c` — Greybox Protocol (playbook)
The build-with-placeholders doctrine: every prefab = component root +
`Model` child, so swapping in real art never touches wiring. Art-swap
procedure documented.

### 11. `e5cdd0a` — NationColorizer
Runtime kingdom-color tinting by OWNER (Fire red, Water blue, Earth green,
Air yellow; gray neutrals, purple dark spirits) via MaterialPropertyBlocks.
Tamed spirits recolor on joining. Doubles as team-color accents on final art.

### 12. `ece4ee9` — Boot Camp tutorial, sell & repair, superweapons, escort missions
Interactive `TutorialManager` watching real player actions (new campaign
Prologue). C&C sell (X, half refund) and worker repairs. `Superweapon` per
element (Comet Barrage / Flash Freeze / Great Storm / Stone Rampart),
4-minute charge, P-to-target, AI auto-fires. `Escort` objective — mission
3-1 became a caravan escort to a golden beacon.

### 13. `0eb7e90` — Placement fixes + tower upgrades
Two audit bugs fixed (ghost NavMeshObstacle carving; UI click-through).
Building upgrade category: towers/walls upgradable (damage/HP/fire
rate/range) via `StructureBaseStats`.

### 14. `22201f5` — Minimap jump, pause menu, selection info panel
Click/drag the minimap to move the camera (`RTSCameraController.JumpTo`).
Esc pause menu (self-built, yields to all other Esc uses). Bottom-left
selection panel showing HP/damage/Avatar energy/transport passengers.

### 15. `d9d5222` — Plastic Soldiers mode
F9: everyone becomes glossy single-color toy plastic in kingdom colors —
the Army Men homage. Pure visuals, batching-friendly, fully reversible.

### 16. `3878911` — Elemental ally shields (Q)
Benders shield nearby allies in their element: Air (40% reduction +
knockback immunity), Water (60-damage ice pool), Earth (60% but slower),
Fire (30% + scorch aura). AI benders auto-cast; Avatar shields in its
current element. Fixed the damage-interceptor cache to be refreshable.

### 17. `263cf21` — Shield mastery upgrades + battle formations
`improvesShields` upgrade tracks (strength/duration/cooldown per level,
per bender type, applies to all of that type). `FormationUtility` ring
formations for group moves/attack-move/reinforcements — no more dogpiles.

### 18. `8e870c7` — Polish pass
Workers flee combat and return to work. Smart health bars (damaged/
selected/recently-hit only). `GameFeel` camera shake + boss-death slow
motion. Corpses linger 10s but exit gameplay instantly. `SceneLoader`
lore-quote loading screens.

### 19. `037067f` — Roots, freezes, select-army, the Colossal Spirit
Earth Grip (root anywhere), Ice Prison (freeze only near real water —
`WaterProximity`), E = select entire army (double-tap centers camera), and
`ColossalSpirit`: a sleeping giant only an Avatar can awaken by MERGING
with it, fighting as the colossus for 60s (Korra-style).

### 20. *(this commit)* — Elite techniques, trade routes, fishing piers, interludes, advisor, dirge, this document
Lightning redirection (Iroh's technique, upgrade-granted, catches
firebender bolts AND tower lightning). Metalbending (top-tier Earth: bonus
damage vs vehicles and fortifications). Trade routes (controlled villages
dispatch raidable carts to your base). Fishing piers (water-edge building
launching boats that visibly work the shoals). Full-screen chapter
interludes. Elder Miza's contextual advice toasts. The Colossus Dirge
(a drumbeat the whole map hears while a giant walks).
*(Landed as `99e2205`.)*

### 21. `18b5d43` — Ceremonial Avatar arrivals
Avatars ARRIVE: bison descent (Air), dragon flight (Fire), wave ride
(Water), earth eruption (Earth). NationData.avatarMountPrefab slot with
greybox mount fallbacks; the Avatar is suspended (unhittable) during
transit.

### 22. `626519a` — Flight, spirit travel, sky supply lines, base completion levels
`FlyingMover` smooth surface-hugging flight (climbs small hills/buildings,
steers AROUND mountains). `AirbenderMobility`: auto air-scooter sprints and
staff-glider flight for airbenders. Buildings/CCs auto-carve the NavMesh
(`NavObstacleUtility`) so nothing paths through structures. Spirit portals
became a travel network (right-click to send units through the spirit world
to the linked portal). `AirSupplyPost` Sky Moorings with shoot-down-able
flying bison couriers. Campaign missions open at per-level base completion
(1-1 at 75%; several at 0% where the first befriended village grants a
500-silver alliance gift).

### 23. `34307fe` — Lavabending, mountain perches, nation academies, the asset shopping list
**Lavabending**: the Earth Kingdom's second ultimate (beside Metalbending) —
upgrade-granted; every strike ignites the victim, splashes molten rock onto
packed enemies, and melts fortifications (+40%); stacks with Metalbending.
**Mountain perches**: Air Nomad buildings can be raised on steep
mountainsides (>22°), out of reach of ground armies; slope validation for
everyone else; perched Air buildings SWAP DESIGNS via a `MountainModel`
prefab child (greybox platform + cliff struts generated when absent).
**Nation academies**: four optional prologue tutorials — The Western Spires
(Air mobility), The Tidecaller's Circle (Water sustain), The Granite Yard
(Earth fortification), The Ember Court (Fire aggression) — each forces its
nation and confirms every technique through new `TutorialSignals` action
counters; the campaign unlock chain skips optional levels so Chapter 1
still opens right after Boot Camp. Plus `ASSET_LIST.md` (the complete
model/VFX/audio/sprite shopping list) and a latent `Mathf` compile fix in
DefaultCampaign.

### 24. `977311d` — Mountain Breaker
The counter to mountain perches: Earth's priciest elite upgrade
(`grantsTremorAssault`). Earthbenders at a mountain's foot bend tremors up
through the rock, steadily shaking apart hostile perched buildings within
~25m — damage stacks per earthbender. Perches are safe from swords, not
from the mountain itself.

### 25. *(latest)* — Upgrade tech trees, branch abilities, and the generated starter pack
Upgrades became **branching tech trees**: prerequisites chain nodes,
**exclusive branches permanently lock each other out** (Lavabending ⛔
Metalbending, Healing Waters ⛔ Everfrost, Staff Gliders ⛔ Tornado
Summoning, Lightning Redirection ⛔ Flame Dive, Avatar Spirit ⛔ Fury), and
branch nodes are **researched at specific buildings** (Barracks vs Deep
Sanctum, Healing Hut vs Moon Shrine...). New branch-end abilities:
`TornadoSummon`, `FlameDive`, `FireSpray` (vehicle burn aura), `Everfrost`
(freeze anywhere) + Frozen Grasp duration scaling, Avatar Spirit
Communion/Elemental Fury tracks, and staff-glider flight became LEARNED
(`grantsGliderFlight`). Vehicle/animal lines got armor/damage nodes.
Plus the **generated starter pack**: `Legends ► Bootstrap ALL` editor menu
(creates the full tree, greybox prefabs for every unit/building/neutral,
NationData ×4, NationDatabase), 25 procedural WAV sounds auto-wired through
SoundManager fallbacks, 4 nation emblem PNGs, and 5 OBJ props.

---

## The system map (what talks to what)

- **Ownership**: `FactionMember`/`FactionUtility` → `FactionManager`
  diplomacy → consumed by combat, selection, fog, minimap, economy, AI.
- **Money**: everything → `Economy` → PlayerResources (HUD) / Faction
  treasuries (AI) / RTSNetworkPlayer (multiplayer server).
- **Combat**: `AttackController` (acquire) → Animator state machine
  (`UnitFollowState`/`UnitAttackState`) → damage pipeline: biome multiplier
  → attack style → metalbending → lightning redirect → interceptors
  (shields, boss shrouds, Avatar states) → `Unit.TakeDamage` → bounties.
- **Progression**: `UpgradeData` assets → `UpgradeManager` levels per
  faction → stat multipliers (`UnitBaseStats`/`StructureBaseStats`) +
  granted abilities (Healer, LightningRedirect, MetalBending, shields).
- **Campaign**: `DefaultCampaign` data → `CampaignManager` (scratch starts,
  waves, bosses, escort, tutorial, interludes) → `CampaignProgress` saves.
- **Presentation**: `GameFeel` (shake/slow-mo), `SceneLoader` (quotes),
  `DialogueUI`/`InterludeUI`, `AdvisorSystem`, SoundManager barks/stingers/
  dirge, `NationColorizer`/Plastic mode.

## Where to go from here

1. Open in Unity 6000.0.43f1 → zero console errors expected.
2. Run `AI_WIRING_PLAYBOOK.md` phases P1→P9 (or wire by hand via
   `EDITOR_WIRING.md`).
3. Play Boot Camp → chapter 1 → a skirmish. Tune numbers in the data assets.
4. Art pass via the Greybox Protocol's Model-child swaps.
5. Multiplayer LAN test → Quest 3 via `VR_AND_VOICE.md`.
