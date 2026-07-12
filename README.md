# Legends-of-the-Four-Elements
Legends of the Four Elements RTS game developed with Unity


In the Era of the Four Kingdoms, the balance between the physical and spirit worlds has been shattered. A massive disturbance in the Spirit World has resurrected four past elemental masters—one from each kingdom—each with their own ambitions and fractured memories. As spiritual energy ruptures, dark spirits flood the land, wreaking havoc on villages and twisting the elements themselves. Now, the kingdoms are in chaos, with rival factions vying for control, unsure whether these returned masters are saviors or harbingers of destruction. Will you restore balance, or claim power in the age of elemental war?

## Features (code complete — see docs for Editor wiring)

- **Four playable nations** — Air, Water, Earth and Fire, each defined by a
  `NationData` asset (unit roster, buildings, colors, sounds).
- **Nation selection** — pick your nation and mode (Survival waves or
  Skirmish vs AI commanders) from the main menu.
- **Neutral map beings** — tribal villages that pay tribute to whoever holds
  them, wandering villagers, and spirits: friendly ones you can befriend, and
  dark ones that raid everyone until tamed in battle.
- **Skirmish AI** — AI commanders that train armies and launch attack waves,
  Army Men RTS / Halo Wars style. Last team standing wins.
- **Online multiplayer** — host/join co-op or versus matches (Netcode for
  GameObjects, server-authoritative).
- **Living economy** — one Silver currency earned by kingdom tax, worker
  units harvesting fish/crystal/coal/spirit-grove nodes, income buildings,
  village tribute and spirit-hunting bounties; players and AI earn identically.
- **Full rosters** — humans, war animals (sky bison, badgermoles, komodo
  rhinos...), ships/tanks/war balloons, workers, buildings with ghost-preview
  placement, defense towers, and per-nation upgrade tracks
  (see [`docs/ROSTERS.md`](docs/ROSTERS.md)).
- **The Avatar** — each player's unique hero: bends all four elements,
  unleashes the Avatar State, and is the only being able to energy-bend wild
  spirits onto your side.
- **Maps & climates** — map selector with seeded random map generation, and
  elemental biomes (volcanoes, glaciers, rivers, windy peaks, quarries,
  spirit wilds) that empower or weaken units by element.

- **Fog of war & minimap** — unexplored land is hidden; scouts like winged
  lemurs and sky bison (and "seismic sensing" upgrades) see further, and
  discovered resources, villages and spirit portals stay on your minimap.
- **Story campaign: "The Rupture"** — 25 levels across 5 chapters with
  dialogue, chi progression and permanent upgrades. Defeat a corrupted past
  Avatar at the end of each chapter to redeem them into your arsenal, then
  unite all four against Umbriss, the First Shadow — the source of the
  imbalance, who can only be wounded while the Avatars stand together
  (see [`docs/CAMPAIGN.md`](docs/CAMPAIGN.md)).

## Docs

- [`docs/EDITOR_WIRING.md`](docs/EDITOR_WIRING.md) — **start here**: the
  master phase-by-phase checklist for wiring everything in the Unity Editor.
- [`docs/ROSTERS.md`](docs/ROSTERS.md) — the full faction design sheet
  (units, buildings, upgrades, Avatar, biomes, economy sources).
- [`docs/BRANCH_HISTORY.md`](docs/BRANCH_HISTORY.md) — the full chronicle of
  every commit and system on this branch.
- [`docs/CAMPAIGN.md`](docs/CAMPAIGN.md) — the 25-level story campaign:
  plot, bosses, redemption/summoning, chi progression, and its (tiny)
  wiring needs.
- [`docs/FINISHING_GUIDE.md`](docs/FINISHING_GUIDE.md) — background detail
  per system, same content the wiring checklist references.
- [`docs/MULTIPLAYER_SETUP.md`](docs/MULTIPLAYER_SETUP.md) — NetworkManager
  setup, prefab registration, LAN/Relay play, and testing.
- [`docs/CONTROLS.md`](docs/CONTROLS.md) — every mouse/keyboard control,
  control groups, attack-move, Avatar hotkeys, and the tower element table.
- [`docs/VR_AND_VOICE.md`](docs/VR_AND_VOICE.md) — hero/embodiment mode,
  OpenAI voice command setup, the Meta Quest 3 integration plan, and Quest
  performance budgets.
- [`docs/AI_WIRING_PLAYBOOK.md`](docs/AI_WIRING_PLAYBOOK.md) — copy-paste
  prompts for having Claude + Unity MCP do the editor wiring for you,
  phase by phase.
