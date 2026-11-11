# Handoff — context for a new Claude Code session

> Paste-able context for continuing work on this project in a fresh
> session (cloud or local). Read this, then `NEXT_STEPS.md` and
> `BRANCH_HISTORY.md`, before writing any code.

## What this project is

**Legends of the Four Elements** — a Unity 6 (6000.0.43f1, URP) RTS in the
style of Army Men RTS / Halo Wars / C&C, themed on Avatar: The Last
Airbender. Four nations (Air/Water/Earth/Fire), buildable one-per-player
Avatar, 25-level campaign ("The Rupture") + Boot Camp + 4 optional nation
academies, branching upgrade tech trees with mutually-exclusive paths,
fog of war, biome buffs, mountain-perch Air buildings, spirit-portal
travel, multiplayer via Netcode for GameObjects, Quest 3 embodiment mode,
and OpenAI-compatible voice command. ~33 commits of pure code+docs built
WITHOUT a Unity editor in the loop; the owner runs Unity on their laptop.

## Where everything lives

- **Branch: `claude/four-nation-multiplayer-rts-47has0`** — ALL the game
  work. `main` is the original two-team prototype (plus a Windows clone
  fix). Keep developing on this branch unless told otherwise.
- Game code: `Legends of the Four Elements/Assets/Scripts/` (subfolders:
  Factions, Match, Economy, Buildings, Units, Campaign, Neutrals, UI,
  Upgrades, Multiplayer, Voice, World, Fun)
- Editor automation: `Assets/Editor/LegendsBootstrap.cs` (creates upgrade
  trees + greybox prefabs + nations from a menu) and
  `LegendsSceneBuilder.cs` (builds a playable scene)
- Generated assets: `Assets/Resources/Audio` (27 WAVs, auto-wired by
  SoundManager fallbacks), `Assets/UI/Emblems` + `Assets/UI/Icons`,
  `Assets/Models/Greybox` (10 OBJ props)
- Docs: `docs/` — `NEXT_STEPS.md` (owner's checklist), `BRANCH_HISTORY.md`
  (what every commit did and why), `EDITOR_WIRING.md` (master wiring
  checklist), `ROSTERS.md` (all design numbers + tech-tree tables),
  `CAMPAIGN.md`, `CONTROLS.md`, `ASSET_LIST.md`, `AI_WIRING_PLAYBOOK.md`
  (Unity MCP prompts), `MULTIPLAYER_SETUP.md`, `VR_AND_VOICE.md` (Quest
  pre-flight), `WEB_BUILD.md`, `FINISHING_GUIDE.md`

## House rules (learned the hard way — do not break)

1. **Never rename or delete existing .cs files**; serialized scene/prefab
   references die. Serialized enums are APPEND-ONLY (Unit.UnitType,
   UnitCategory, climate types...).
2. **Legacy compatibility**: the original Level1_Scene must keep working.
   `FactionUtility` falls back to the old `Team` enum (Player→faction 0,
   Enemy→1). Don't remove the fallbacks.
3. **All money through the static `Economy` API** (routes to PlayerResources
   HUD / Faction.credits AI / RTSNetworkPlayer in MP). Never touch wallets
   directly.
4. **Damage pipeline order** (UnitAttackState.Attack): biome multiplier →
   AttackStyleSet style → MetalBending/LavaBending → LightningRedirect →
   TakeDamage → IDamageInterceptor chain (shields/boss shroud/Avatar) →
   kill bounty. Insert new modifiers deliberately.
5. **Greybox Protocol**: every prefab = component root + `Model` child
   (Air buildings also a disabled `MountainModel` child). Runtime
   primitives must call `GreyboxMaterial.Harmonize()` (URP magenta fix).
6. **Systems self-bootstrap**: managers use RuntimeInitializeOnLoadMethod,
   UI builds itself in code, statics reset via SubsystemRegistration.
   Follow the pattern; nothing should require editor wiring to function.
7. **Tech tree**: upgrades gate via `UpgradeManager.CanPurchase`
   (prerequisites, exclusiveWith locks, requiredBuildingKeyword — the
   keyword matches BUILDING PREFAB NAMES, e.g. "Barracks").
8. **Platform discipline**: no UnityEditor APIs outside Assets/Editor (or
   wrap in #if UNITY_EDITOR); Microphone code stays WebGL-guarded; keep
   tracked file paths under ~200 chars (Windows MAX_PATH broke clones
   once); never commit big third-party asset packs.
9. **Secrets**: `Assets/Resources/openai_key.txt` and `openai_base_url.txt`
   are gitignored — never commit keys. Voice works with any
   OpenAI-compatible endpoint (owner uses LiteLLM).
10. **Commit + push to the branch after every coherent batch**
    (`git push -u origin claude/four-nation-multiplayer-rts-47has0`), and
    append the batch to `docs/BRANCH_HISTORY.md`.
11. **No Unity in cloud sessions**: you cannot compile or playtest. Verify
    with greps/balance checks, use only APIs verified to exist in the
    codebase, and tell the owner what to check in the editor.

## Current state / what's next

Everything through the platform audit is DONE and pushed (see
BRANCH_HISTORY entries 1–30). The single most important next event: the
owner opens the project in Unity 6000.0.43f1, runs **Legends ► Bootstrap
ALL** and **Legends ► Build Playable Skirmish Scene**, bakes the NavMesh,
plays, and reports the first console error — nothing has ever been
compiled. Expect small API fixups on first compile; fix whatever they
paste. After that: balance tuning from real play, main-menu wiring
(EDITOR_WIRING Phase 4), multiplayer LAN test, Quest 3 build
(VR_AND_VOICE pre-flight), art passes (ASSET_LIST), browser demo
(WEB_BUILD).
