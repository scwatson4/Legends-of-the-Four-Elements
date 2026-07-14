# AI Wiring Playbook — Claude + Unity MCP

How to make an AI assistant do the Unity Editor wiring for you. This is a
**copy-paste prompt script**: run the setup once, then feed the phase
prompts to Claude Code one at a time while Unity is open. Each phase maps
to [`EDITOR_WIRING.md`](EDITOR_WIRING.md) and ends with a verification step.

Works with Claude Code + [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp)
(recommended: free, MIT, ~47 editor tools). Cursor/Copilot with the same
MCP server also work — the prompts are tool-agnostic.

## One-time setup (~10 minutes)

1. Open this project in Unity 6000.0.43f1; let packages compile clean.
2. Unity: **Package Manager → + → Add package from git URL** → the URL in
   the CoplayDev README (needs Python 3.10+ and `uv` installed).
3. Unity: **Window → MCP for Unity** → click the auto-configure button for
   Claude Code (or copy the JSON into `claude mcp add`).
4. Terminal: `cd` into this repo, run `claude`, type `/mcp` — confirm the
   Unity tools are listed.
5. **Golden rules for every session:**
   - Keep Unity focused/open; the bridge talks to the live editor.
   - `git commit` after every successful phase so any AI mistake is one
     `git checkout` away from undone.
   - Work ONE phase per conversation; verify in the editor before moving on.
   - If the AI says it did something, spot-check one example yourself.

## The Greybox Protocol (read before P3/P4)

The whole game gets built with **placeholder visuals** — Unity primitives
and meshes from packs already in the project — and you swap in real art
later. One structural rule makes that swap painless, and the phase prompts
below instruct the AI to follow it:

**Every prefab is a plain root + a child named `Model`.**
- The ROOT holds everything that matters: all components (Unit,
  AttackController, NavMeshAgent, Animator, colliders...), the HealthBar
  and Indicator children, and VFX anchor children. Nothing visual.
- The `Model` child holds ONLY the placeholder mesh (a tinted capsule,
  a cube tower, a cylinder wall...).
- **Swapping art later = delete the Model child, drop in the real model as
  the new `Model` child, done.** No component rewiring, no NationData
  re-linking, no scene edits — every reference points at the root, which
  never changes.

Two extra rules:
1. **Units must be duplicated from AirbenderUnit/FirebenderUnit**, never
   built from scratch — combat runs through the Animator state machine
   (UnitAttackState/UnitFollowState live on its states), so the root's
   Animator + controller must survive. A capsule won't visibly animate;
   that's fine, the logic still runs. When you later drop in a real
   humanoid model, keep the root Animator/controller and set the new
   model's Avatar on it (Unity retargets the animations).
2. **Tint by kingdom — the baseline look**: every placeholder unit reads
   as its nation at a glance: **Fire = red, Water = blue, Earth = green,
   Air = yellow** (the exact `NationInfo.ThemeColor` palette the UI uses).
   The zero-effort way: add the **`NationColorizer`** component to each
   prefab's Model child — it tints at runtime to the OWNER's kingdom
   color, so one prefab reads correctly for every faction, tamed spirits
   recolor when they join you, and no material assets are needed at all.
   (Static per-nation materials work too; NationColorizer is just less
   work and smarter.) Neutrals render gray, dark spirits purple.

Where real assets already exist, use them instead of primitives: the bison
models, AirNationTemple / FireNationCitadel, the PolygonCharacters-Labourer
for villagers/workers, and any pack VFX for element effects. Placeholder
VFX can even be a colored point light or default particle system — the
`flamethrowerEffect` slot just needs SOMETHING to toggle.

## The kickoff prompt (paste first, every session)

> You have Unity MCP tools connected to my open Unity project
> "Legends of the Four Elements". Read `docs/EDITOR_WIRING.md`,
> `docs/ROSTERS.md`, and skim the scripts it references before acting.
> Rules: (1) never delete existing assets or scenes; (2) create new things
> in the folders the docs specify; (3) after each batch of changes, read
> the Unity console and fix any errors you caused; (4) tell me exactly
> what you created/modified so I can verify.

## Phase prompts (run in order)

> **Do this first — it replaces most of P2–P4**: in Unity, run
> **`Legends ► Bootstrap ALL`** (menu, from `Assets/Editor/LegendsBootstrap.cs`).
> It deterministically creates the upgrade tech trees, greybox unit/building/
> neutral prefabs, NationData ×4 and the NationDatabase — no AI involved, so
> nothing to spot-check. Then use P2–P4 below only as VERIFICATION prompts
> ("compare the created assets against ROSTERS.md and report mismatches")
> or to fill anything the bootstrap warned it skipped. P1 and P5–P9 (scenes,
> UI, multiplayer) still run as written. Generated placeholder sounds
> auto-wire themselves (SoundManager loads Resources/Audio when slots are
> empty), and the four nation emblems live in Assets/UI/Emblems.
>
> **Also skips most of P6**: run **`Legends ► Build Playable Skirmish
> Scene`** to generate a complete scene (ground, camera, match rig, a
> self-building GameHUD, StartLocations, scattered map) — bake the NavMesh
> and play. P6 by hand remains for custom scenes.

### P1 — Layers sanity check
> Check my Tags & Layers against Phase 1 of EDITOR_WIRING.md. Report the
> actual names of my clickable/ground/attackable layers (read them from
> the UnitSelectionManager in Level1_Scene) — don't change anything yet.

### P2 — Data assets
> Do Phase 2 of EDITOR_WIRING.md: create every UpgradeData asset from the
> ROSTERS.md tables (exact ids, costs, bonuses, categories — include
> Seismic Sensing's sightBonus, and the four elite techniques: Lightning
> Redirection with grantsLightningRedirect restricted to Firebender, plus
> Metalbending (grantsMetalBending), Lavabending (grantsLavaBending) and
> Mountain Breaker (grantsTremorAssault) all restricted to Earthbender,
> single expensive levels), then the four NationData assets with identity fields filled from
> ROSTERS.md, then the NationDatabase at exactly
> Assets/Resources/NationDatabase.asset with all four linked.
> Leave prefab slots empty for now.

### P3 — Unit prefab variants
> Follow the Greybox Protocol: every prefab = component root + a child
> named "Model" holding only the placeholder mesh, with a NationColorizer
> component on the Model child so each unit renders its kingdom's color at
> runtime (Fire red, Water blue, Earth green, Air yellow). Duplicate from
> AirbenderUnit so the Animator survives. Then do Phase 3.1–3.4: duplicate AirbenderUnit.prefab into WaterbenderUnit and
> EarthbenderUnit (set Unit.unitType and Unit.category), create the four
> worker prefabs (remove AttackController/EnemyAI, add ResourceCollector,
> category Worker), and the four Avatar prefabs (add AvatarUnit, HP 400,
> damage 25, category Avatar, four empty child GameObjects named
> AirEffect/WaterEffect/EarthEffect/FireEffect assigned to the element
> slots, plus an AvatarStateAura child). Set VisionSource 25 on a
> duplicated bison prefab as the Sky Bison. Keep every prefab on the same
> layer as AirbenderUnit. Then fill each NationData.units roster per
> ROSTERS.md order (slot 0 = basic bender, Avatar last).

### P4 — Nodes, neutrals, buildings
> Follow the Greybox Protocol (root + Model child, primitives with nation
> or gray tints; reuse existing pack meshes where they fit - bison,
> temples, labourer characters). Do Phase 3.5–3.9: spirit prefabs (Friendly + Dark with Spirit, Tameable,
> killBounty 15/40), a Villager and Village prefab, a SpiritPortal prefab,
> the four ResourceNode prefabs (one per type), and each nation's buildings
> from ROSTERS.md (Structure + IncomeBuilding/UnitSpawner/DefenseTower with
> the right element + ResourceDropoff where listed). Use simple primitive
> or existing pack meshes as placeholders — I'll re-skin later. Add
> ResourceDropoff to all four command center prefabs and set
> commandCenterCost 400 + the CC prefab reference on each NationData.
> Fill NationData.buildings lists, and set each NationData's Avatar Mount
> Prefab (use the bison model for Air; leave others empty for greybox).
> Add a Sky Mooring to the Air roster (Structure + AirSupplyPost, cost 180).
> On every AIR building prefab also add a DISABLED child named
> "MountainModel" containing a visibly different placeholder (e.g. the same
> primitive on stilt cylinders) — the game swaps it in automatically when
> the building is placed on a mountainside.
> Confirm every building prefab has a collider (pathing obstacles are added
> automatically at runtime). Save the DarkSpirit prefab copy at
> Assets/Resources/Campaign/DarkSpirit.prefab and a scaled-up variant at
> Assets/Resources/Campaign/FinalBoss.prefab.

### P5 — Menu UI
> Do Phase 4: in MainMenuScene build the NationSelectPanel (4 nation
> buttons, mode buttons, AI-count slider, difficulty buttons wired to
> SetDifficulty, map buttons, Start/Back) wired exactly per the table, a
> CampaignPanel per docs/CAMPAIGN.md (ScrollView + level button prefab +
> CampaignMenuController + chi/redeemed labels + a Campaign button on the
> main menu), and assign MainMenuController.nationSelectPanel. Use the
> scene's existing button style as the template. Then verify every
> onClick target resolves (list them).

### P6 — Level scene rig
> Do Phase 5 on [Level1_Scene / my skirmish scene]: MatchController with
> MatchManager (database assigned), BuildingPlacer (ground mask), FogOfWar
> sized to the terrain, minimap RawImage + MinimapController on the HUD,
> 2-4 StartLocations, scatter 3 villages / 4 nodes / 2 spirits / 1 portal /
> 4 biome zone objects at sensible positions on the NavMesh, and add a
> VoiceCommander to the HUD. Wire the side panel: unit buttons →
> QueueRosterUnit(0..n), a Build tab → BeginPlacement(0..n) + a Found Base
> button → BeginCommandCenterPlacement, upgrade buttons →
> UpgradePurchaser.Purchase(0..n). Rename the credits label "Silver".

### P7 — Random map scene
> Do Phase 7: create RandomMap_Scene with a large ground-layer terrain,
> NavMeshSurface, the full Phase 5 rig, and a MapGenerator with all prefab
> lists assigned (6 biome zone prefabs — create them as empty roots with
> BiomeZone components per climate). Add the scene to Build Settings and
> to the menu's Map Scene Names.

### P8 — Multiplayer plumbing
> Follow docs/MULTIPLAYER_SETUP.md: NetworkManager + UnityTransport +
> NetworkBootstrap in MainMenuScene, NetworkPlayer prefab with
> RTSNetworkPlayer as the player prefab, add NetworkObject +
> NetworkTransform + NetworkAnimator + NetworkUnit to every unit prefab,
> NetworkCommandCenter to CCs, NetworkStructure to buildings, register all
> of them in the Network Prefabs list, and add NetworkMatchManager (with a
> NetworkObject) to the gameplay scenes. Build the multiplayer panel per
> the wiring table.

### P9 — Housekeeping + smoke test
> Add all scenes to Build Settings in a sensible order, write and run an
> editor script that rebuilds NavMesh where needed, then enter Play Mode
> in Level1_Scene, read the console, and fix every error or missing
> reference you introduced. Give me a summary of anything you could not
> fix and why.

## Verification prompts (use any time)

> Read the Unity console and summarize all errors/warnings with the asset
> or scene each one comes from.

> Open [scene], list every component with an unassigned serialized field
> that the docs say should be assigned.

> Compare NationData assets against ROSTERS.md and report mismatches.

## The art-swap pass (after everything plays)

**What to buy/collect**: the complete shopping list — every model, VFX,
sound and sprite the finished game wants, with priorities — lives in
[`ASSET_LIST.md`](ASSET_LIST.md).

When you've picked real models/VFX, the swap per prefab is mechanical —
and you can even delegate the boring half of it back to the AI:

1. Open the prefab. Delete the `Model` child.
2. Drag the real model in as the new `Model` child; scale to match the old
   footprint (colliders on the root show the intended size).
3. Units only: on the root Animator, assign the new model's Avatar
   (humanoid rig) so the existing bender animations retarget. If the model
   ships with its own animations you prefer, they must expose the same
   parameters: `isMoving`, `isFollowing`, `isAttacking` bools + `Die`
   trigger — or just keep the shared controller.
4. VFX: drop the real effect prefab onto the same anchor child and
   reassign the slot (flamethrowerEffect / element effects / aura /
   tower shot effects).
5. Play one match; the unit should behave identically, just prettier.

AI-assistable version: "Replace the Model child of WaterbenderUnit with
[asset path], scaled to the old bounds, and set its Avatar on the root
Animator" works well through Unity MCP once you've imported the asset and
chosen it yourself.

## What NOT to delegate to the AI

- **Art judgment** — which model/material/VFX looks right is your call;
  have the AI use placeholders, then you swap them.
- **XR device setup** (Quest developer mode, Link, on-device profiling) —
  physical-device work, see `VR_AND_VOICE.md`.
- **Playtesting and balance** — the AI can change numbers but only you can
  feel that chapter 2 is too hard.
- **The final boss fight tuning** — hand-tune Umbriss yourself; it's the
  climax.

## When it goes wrong

- Wrong/weird edit → `git status` + `git checkout -- <path>` (you committed
  between phases, right?).
- Buttons wired to nothing → run the verification prompt; UnityEvent wiring
  is the flakiest MCP operation. Worst case, ask the AI to generate a
  one-shot Editor script that wires the panel deterministically and run it
  via the menu.
- Bridge stops responding → re-open Window > MCP for Unity, restart the
  server, restart `claude`.
