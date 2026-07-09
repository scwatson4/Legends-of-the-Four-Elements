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

## Docs

- [`docs/FINISHING_GUIDE.md`](docs/FINISHING_GUIDE.md) — step-by-step Unity
  Editor checklist to finish the game (prefabs, data assets, UI wiring,
  scenes, assets).
- [`docs/MULTIPLAYER_SETUP.md`](docs/MULTIPLAYER_SETUP.md) — NetworkManager
  setup, prefab registration, LAN/Relay play, and testing.
