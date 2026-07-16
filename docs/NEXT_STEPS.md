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

## 3. Build a playable scene — one click (~2 min)

- [ ] Menu → **Legends ► Build Playable Skirmish Scene**. It creates
      `Assets/Scenes/Skirmish_Generated.unity` with everything wired: ground
      (with hills + a steep mountain for testing perches), camera rig,
      lighting, the match rig (MatchManager + selection + placement + fog +
      sound), a **self-building GameHUD** (Train/Build/Upgrade panel — no
      manual button wiring), two StartLocations, and scattered nodes /
      village / portals / biome zones.
- [ ] **Bake the NavMesh**: Window ► AI ► Navigation (or add a
      NavMeshSurface to the Ground and Bake). Mark the ground + hills +
      mountain as Navigation Static first. The steep mountain stays
      unwalkable — that's what makes it a non-Air no-go and an Air perch site.
- [ ] Press **Play**. The scene is playable directly (SkirmishAutoConfig
      sets up Air vs. 1 AI when launched outside the menu).

*(The from-scratch alternative — hand-wiring Level1_Scene via EDITOR_WIRING
Phase 5, side-panel buttons to `QueueRosterUnit`/`BeginPlacement`/
`UpgradePurchaser.Purchase` — still works and is documented there if you'd
rather build your own scene. The generated scene is just the fast path.)*

## 4. Wire the main menu (~30 min, optional for a first playtest)

You can skip this and playtest the generated scene directly. When you want
the front end: **EDITOR_WIRING.md Phase 4** (or playbook **P5**) —
nation-select panel, campaign panel (`CampaignMenuController` +
level-button prefab), difficulty/map buttons, Start.

## 5. The 15-minute smoke test

Play the generated scene and tick these off — together they exercise
everything new:

- [ ] The HUD shows Silver + Population and a Train/Build/Upgrade panel;
      press **B** or Build ► Found Base to place your command center, then
      train a worker and a bender from the Train tab
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
- [ ] Quest 3 + voice command setup → VR_AND_VOICE.md — start with its
      **build pre-flight checklist** (the code side is audit-clean; only
      device/editor config remains). Needs your OpenAI key in
      `Assets/Resources/openai_key.txt` — gitignored, never commit.
- [ ] Browser demo → WEB_BUILD.md (single-player WebGL build for itch.io)
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
- The **generated scene must have its NavMesh baked** — units won't move
  until you do. It's the one manual step the scene builder can't do for you
  (NavMesh baking is editor-only and needs your Navigation-Static flags).
- If the Train tab says "Found your base first": you haven't placed a
  command center yet (the HUD trains from the CC's spawner). Press B.
