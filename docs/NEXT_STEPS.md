# NEXT STEPS — your checklist for when you're back at the laptop

Everything on this branch was written **without a Unity Editor in the
loop**, so the very first session is about compiling, bootstrapping, wiring
two scenes, and reporting anything that misbehaves. Do these in order —
each step is short, and steps 1–5 get you to a playable skirmish.

## 1. Open and compile (~10 min)

- [ ] Pull branch `claude/four-nation-multiplayer-rts-47has0`, open the
      project in **Unity 6000.0.43f1**, let it import (first import is slow —
      new packages, 25 WAVs, OBJs, PNGs).
- [ ] Open the Console. **Goal: zero errors.** If there is ANY compile
      error, stop here and paste it back to Claude — that's the fastest fix
      loop, and nothing else works until the code compiles.

## 2. Run the bootstrap (~2 min)

- [ ] Menu bar → **Legends ► Bootstrap ALL (assets + prefabs + nations)**.
- [ ] Verify it produced:
  - `Assets/Nations/Upgrades/` — ~40 upgrade assets (the tech trees)
  - `Assets/Nations/` — AirNation / WaterNation / EarthNation / FireNation
  - `Assets/Resources/NationDatabase.asset`
  - `Assets/Prefabs/Greybox/` — units, buildings, spirits, villages, nodes,
    portal (+ `Assets/Resources/Campaign/DarkSpirit` & `FinalBoss`)
- [ ] Spot-check one nation asset (e.g. EarthNation): roster slots filled,
      buildings list filled, upgrades list filled, emblem sprite assigned.
- [ ] Any warnings it logged tell you what it skipped and why — paste them
      to Claude if unclear.

## 3. Wire the main menu (~30 min)

Follow **EDITOR_WIRING.md Phase 4** (or playbook prompt **P5** if using
Unity MCP + AI): nation-select panel, campaign panel
(`CampaignMenuController` + level-button prefab), difficulty buttons,
map buttons, Start.

## 4. Wire one gameplay scene (~45 min)

Follow **EDITOR_WIRING.md Phase 5** (or playbook **P6**) on Level1_Scene:

- [ ] MatchController: `MatchManager` (database), `BuildingPlacer`
      (ground mask), `FogOfWar`, minimap, 2–4 `StartLocation`s
- [ ] Side panel: unit buttons → `QueueRosterUnit(0..4)` (0=bender,
      1=worker, 2=beast, 3=war machine, 4=Avatar), build buttons →
      `BeginPlacement(0..n)`, **Found Base** → `BeginCommandCenterPlacement`,
      **upgrade buttons → `UpgradePurchaser.Purchase(0..n)`** (the tech tree
      needs these — and the Air/Water academies teach through them)
- [ ] Scatter a few villages / resource nodes / spirits / 2 portals /
      biome zones; rename the credits label "Silver"
- [ ] **Bake the NavMesh** with terrain + props static. Make sure your
      mountains are STEEP (>22°): steepness is what makes them unwalkable,
      un-buildable for non-Air, and valid Air perch sites.

## 5. The 15-minute smoke test

Play a skirmish and tick these off — together they exercise everything new:

- [ ] Sounds play with zero wiring (attacks, barks, building placed) —
      SoundManager auto-loads the generated WAVs
- [ ] Train a bender; buy **Tempest Training** (should succeed)
- [ ] Try **Tornado Summoning** immediately (should REFUSE: needs Unbending
      Wind + a Pavilion — the feedback label explains itself)
- [ ] As Earth: build a **Barracks**, research down to **Lavabending**,
      then try **Metalbending** (should refuse: sealed by Lavabending)
- [ ] As Air: drag a building ghost onto a steep mountainside — it turns
      green (red for every other nation) and the placed building switches
      to its stilted **MountainModel** perch design
- [ ] As Air: buy **Staff Gliders**, order an airbender far away → it flies
- [ ] As Earth with **Mountain Breaker**: march earthbenders to a mountain's
      foot under an enemy perch → tremors shake it apart
- [ ] Right-click a spirit portal with units selected → they emerge from
      the linked portal
- [ ] Campaign: play **Boot Camp**, then one academy (Granite Yard is the
      richest test); confirm chapter 1 unlocks after Boot Camp alone

## 6. Report back

Paste to Claude, in one message: any console errors, any smoke-test line
that failed, and anything that FELT wrong (costs, speeds, damage). Blind
tuning is now over — numbers can finally be balanced against real play.

## 7. After that (any order)

- [ ] Random-map scene → EDITOR_WIRING Phase 7 / playbook P7
- [ ] Multiplayer LAN test → MULTIPLAYER_SETUP.md / playbook P8
- [ ] Build Settings + full test matrix → EDITOR_WIRING Phase 9 / playbook P9
- [ ] Quest 3 + voice command setup → VR_AND_VOICE.md (needs your OpenAI
      key in `Assets/Resources/openai_key.txt` — gitignored, never commit)
- [ ] Art passes → ASSET_LIST.md (buy in the listed priority passes; swap
      via the Greybox Protocol's Model-child rule; the 5 OBJ props in
      `Assets/Models/Greybox/` are ready as set dressing)

## Known sharp edges (so they don't surprise you)

- The **research keywords check building names** — if you rename a
  bootstrap prefab (e.g. `EarthBarracks`), keep the keyword word
  (`Barracks`) in the name or the branch locks.
- The greybox **beasts and war machines reuse the bender animator** — they
  fight fine but look like people until you swap their Model child. That's
  the Greybox Protocol working as intended.
- OBJ props import with a default gray material — drop any material on
  them if they look flat.
- If gliders never trigger: the order must be ≥45m and the airbender must
  own the Staff Gliders upgrade this match (campaign chi can make it
  permanent).
