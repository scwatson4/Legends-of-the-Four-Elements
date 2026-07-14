# Asset Shopping List — everything to obtain

Every model, effect, sound and sprite the finished game wants, in one place.
**Nothing on this list blocks playing** — the Greybox Protocol
(`AI_WIRING_PLAYBOOK.md`) runs the whole game on tinted primitives, and
every asset here swaps in later without touching wiring (delete the `Model`
child, drop in the real one). Use this as your shopping list on the Unity
Asset Store / Sketchfab / your own artists, and tick things off as you go.

**Formats**: models as FBX/glTF humanoids where possible (Unity retargets
the existing bender animations onto any humanoid Avatar rig); VFX as
prefab'd particle systems; audio as WAV/OGG; sprites as square PNGs
(256–512px for icons).

**Style target**: Army Men RTS / Halo Wars readability — chunky silhouettes
that read at RTS camera distance, in kingdom colors (Fire **red**, Water
**blue**, Earth **green**, Air **yellow**; neutrals gray, dark spirits
purple). `NationColorizer` tints whatever you plug in, so team-neutral
(gray/white) source models work great.

---

## 1. Unit models — humans (16)

One humanoid model each (or 4 bodies re-skinned per nation). All animate via
the shared controller (`isMoving`/`isAttacking` bools, `Die` trigger).

| Nation | Units |
|---|---|
| Air | Air Acolyte (worker), Airbender Monk, Glider Warrior |
| Water | Fisherman (worker), Waterbender Warrior, Healer |
| Earth | Miner (worker), Earthbender Soldier, Boulder Hurler |
| Fire | Coal Engineer (worker), Firebender Soldier, Fire Lancer |
| All 4 | **Avatar** ×4 — one themed hero model per nation (robes/armor of the origin element; these are your poster characters) |

*Already in the project*: PolygonCharacters-Labourer works for all four
workers; AirbenderUnit / FirebenderUnit bodies cover the two basic benders.

## 2. Unit models — animals (8)

| Nation | Animals |
|---|---|
| Air | Winged Lemur (scout), **Sky Bison** *(models already in project!)* |
| Water | Polar Bear Dog |
| Earth | Ostrich Horse (ridden), Badgermole |
| Fire | Komodo Rhino (ridden) |
| Neutral | Elephant Koi or similar ambient wildlife *(optional flavor)* |

## 3. Unit models — vehicles/ships (6)

| Nation | Vehicles |
|---|---|
| Air | War Glider (multi-monk glider wing) |
| Water | Ice Cutter (waterbending-driven warship) |
| Earth | Earth Tank (stone rig) |
| Fire | Tundra Tank, War Balloon |
| Water (piers) | small **Fishing Boat** (FishingPier launches these) |

## 4. Avatar mounts — ceremonial arrivals (4)

Assigned to `NationData.avatarMountPrefab`; the Avatar rides one onto the
battlefield when summoned:

- **Air**: flying bison *(reuse the bison model)*
- **Fire**: dragon
- **Water**: wave-serpent / giant koi (or a stylized cresting wave)
- **Earth**: none needed — the Earth Avatar erupts from the ground (VFX only)

## 5. Neutrals & spirits (7)

| Asset | Notes |
|---|---|
| Villager | civilian model (male/female variants nice-to-have); PolygonCharacters works |
| Village | cluster of huts / longhouse — one building prefab with a control radius |
| Friendly Spirit | glowing gentle creature (fox/rabbit/wisp energy) |
| Dark Spirit | menacing shadow creature — also saved at `Resources/Campaign/DarkSpirit` for boss-arena waves |
| **Umbriss, the First Shadow** | the final boss: a HUGE dark spirit at `Resources/Campaign/FinalBoss` — worth commissioning something special |
| **Colossal Spirit** | the sleeping giant an Avatar can merge with (Korra-scale kaiju) |
| Spirit Portal | glowing torii gate / rift — both a spawner and the travel network |

## 6. Buildings (≈28 + mountain variants)

Every nation: command center, economy, production, defense tower, housing —
plus its specials. Full costs/components in `ROSTERS.md`.

| Nation | Buildings |
|---|---|
| Air | Air Temple (CC), Meditation Pavilion, Bison Stable, Wind Cannon Pagoda (tower), Nomad Dormitories, Sky Mooring, Spirit Shrine |
| Water | Ice Palace (CC), Fishing Dock, Healing Hut, Ice Spike Tower, Shipyard, Tribal Lodge, Moon Shrine (superweapon), **Fishing Pier** |
| Earth | Earthen Citadel (CC), Crystal Mine, Barracks, Badgermole Burrow, Rock Launcher Tower, **Stone Wall**, **Earth Gate**, Stone Tenements, Deep Sanctum (superweapon) |
| Fire | Fire Citadel (CC), Coal Refinery, War Academy, War Factory, Flame Turret, Garrison Quarters, War Sanctum (superweapon) |

**Air mountain variants**: every Air building wants a SECOND design for
mountainside placement — stilted/anchored/cliff-hugging architecture. Ship
it as a disabled child named **`MountainModel`** beside the normal `Model`
child; the game swaps them automatically when the building is perched on a
slope. (Without one, a greybox platform + struts is generated.)

*Already in project*: AirNationTemple and FireNationCitadel models.

## 7. Resource nodes & map objects (6)

- **Spirit Grove** (luminous trees), **Fish Shoal** (shimmering water patch),
  **Crystal Deposit** (green crystal outcrop), **Coal Seam** (black rock vein)
- Escort **golden beacon** (currently a gold cylinder — a shrine/banner is nicer)
- Trade **cart** + pack animal (TradeRoutes couriers; currently greybox cubes)

## 8. Terrain & biome dressing (6 climate sets)

For MapGenerator scenes and the five chapter-themed maps. Each climate wants
a small prop set (4–8 pieces) + ground textures:

| Climate | Props |
|---|---|
| Volcanic | lava rock, vents, scorched trees, ember particles, lava pools |
| Glacier | ice shards, frozen pools, snow drifts, aurora skybox |
| River Lands | reeds, river rocks, willows, water plane material |
| Windy Peaks | pine crags, prayer flags, cloud wisps, rope bridges |
| Stone Quarry | cut-stone blocks, scaffolds, boulder piles |
| Spirit Wilds | bioluminescent flora, twisted trees, floating motes |

Plus the basics: **mountain/cliff meshes** (tall = impassable, and the Air
Nomads build on their faces), hills, generic trees/rocks/bushes, water
shader, 2–3 skyboxes (day, dusk, spirit-night).

## 9. VFX (the big list)

Element attacks & riders:
- Bender attack effects ×4 (air slice, water whip, rock throw, fire stream) —
  the `flamethrowerEffect` slot per unit just needs SOMETHING to toggle
- Status: **burn** flames, **slow/chill** frost sparkle, **root** stone clamp,
  **freeze** ice block, knockback dust puff
- **Lava eruption** (lavabending strike: molten splash + ignited ground)
- Lightning bolt + **lightning redirection** arc (cyan)

Shields & Avatar:
- Q shields ×4 (wind sphere, ice dome, stone shell, flame ring)
- Avatar element effects ×4 (children `AirEffect`/`WaterEffect`/`EarthEffect`/`FireEffect`)
- **Avatar State aura** (glowing eyes/energy — all four elements orbiting)
- Energy-bending beam (taming spirits)
- Ceremonial arrival FX (dust ring on landing, wave crash, earth eruption)

Towers & superweapons:
- Tower shots ×4 + specials (lightning strike, tornado, wave, massive boulder)
- Superweapons ×4: Comet Barrage (meteors), Flash Freeze (ice nova),
  Great Storm (cyclone), Stone Rampart (walls bursting from the ground)

World:
- Spirit portal swirl + **portal transit** flash (units entering/exiting)
- Air scooter swirl + glider trail
- Healing glow (Healer / Healing Waters)
- Building placement poof, construction dust, structure destruction rubble
- Colossal Spirit awakening quake + merge beam
- Sky Mooring bison courier glint *(optional)*

## 10. UI & 2D art

- **Nation emblems ×4** (`NationData.emblem`) + a neutral spirit sigil
- **Unit icons** — one per roster entry (≈30; `UnitEntry.icon`)
- **Building icons** — one per building (≈28; `BuildingEntry.icon`)
- **Upgrade icons** — one per upgrade (≈20; `UpgradeData.icon`)
- Boss portraits ×5 (Zephyra, Kalani, Boruk, Ashan, Umbriss) + Elder Miza &
  Kesu portraits for dialogue *(optional but great)*
- Main-menu key art, campaign chapter cards ×5, loading-screen art
- Cursor set (default, attack, repair, invalid), minimap POI dots (have
  colors already), victory/defeat banners
- A display font with an East-Asian brush feel (TMP asset)

## 11. Audio

Music:
- Main-menu theme, victory & defeat stingers
- Battle/ambient track per biome or per chapter (5–6 tracks)
- **Colossus Dirge** — the drumbeat while a colossal spirit walks *(slot exists in SoundManager)*

SFX (SoundManager slots):
- Attack sounds per element ×4 + generic melee, lightning crack
- Unit **barks** ×4 nations (select / move / attack acknowledgments — a few lines each)
- Building placed / sold / destroyed; construction hammering
- Harvest ticks, silver payout chime, upgrade complete
- Shield cast ×4, heal shimmer, freeze crack, burn crackle
- Avatar State activation roar; ceremonial arrival (bison bellow, dragon roar, wave crash, stone burst)
- Spirit portal hum + transit whoosh; spirit cries (friendly/dark)
- Superweapon alarms + impacts ×4
- UI clicks, dialogue advance blip, tutorial step chime
- Ambient loops per biome (wind, water, lava, spirit hum)

Voice *(optional, big win)*: narrated dialogue for Elder Miza, Kesu, the four
corrupted Avatars and Umbriss — `DialogueLine` can carry an AudioClip per
line with a tiny extension noted in `CAMPAIGN.md`.

## 12. Already in the project (reuse, don't buy)

- Sky bison models (multiple), AirNationTemple, FireNationCitadel
- PolygonCharacters-Labourer (workers/villagers)
- The existing bender animation controller + animations (retargets to any
  humanoid model)
- Terrain textures & basic environment from Level1_Scene

## 13. Already GENERATED (procedural placeholders shipped on this branch)

Usable today, replace at leisure:

- **25 sounds** in `Assets/Resources/Audio/` — element attacks, deaths,
  stingers, barks, the colossus dirge, shield/heal/freeze/lightning/tremor
  accents, superweapon alarm, portal whoosh. SoundManager auto-loads any of
  them into empty inspector slots, so audio works with zero wiring.
- **4 nation emblems** in `Assets/UI/Emblems/` (Air pinwheel, Water
  crescent-wave, Earth square coin, Fire flame) — the bootstrap assigns
  them to each NationData.
- **5 OBJ props** in `Assets/Models/Greybox/` — Torii Gate, 3-tier Pagoda,
  Wall Segment, Watch Tower, Perch Platform — drop them in as `Model`
  children or scene dressing.
- **Every greybox unit/building prefab** via the `Legends ► Bootstrap ALL`
  editor menu (see AI_WIRING_PLAYBOOK.md).

---

### Priority order (if buying in passes)

1. **Pass 1 — readability**: 4 bender bodies + 4 worker bodies + 4 CCs +
   towers ×4 + a tree/rock set. The game stops looking greybox.
2. **Pass 2 — identity**: Avatars ×4 + mounts, spirits, villages, remaining
   buildings, element attack VFX, barks.
3. **Pass 3 — spectacle**: superweapon VFX, bosses (Umbriss! the Colossus!),
   biome prop sets, music, portraits, mountain variants for Air.
