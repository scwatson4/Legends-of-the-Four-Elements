# Campaign Mode — "The Rupture"

A 25-level story campaign in five chapters, fully driven by code
(`Assets/Scripts/Campaign/`). The complete script — every level's title,
objective, enemies, map seed and opening dialogue — ships in
`DefaultCampaign.cs`, so the campaign is playable the moment the menu is
wired; edit that file to rewrite the story.

## The story

The barrier between the physical and spirit worlds ruptures. Dark spirits
flood the land — and four past Avatars return with them, corrupted by
**Umbriss, the First Shadow**, the ancient dark spirit that caused the
imbalance. Each chapter ends with one corrupted Avatar as its boss; defeat
them to **redeem** them and add them to your arsenal. Only all of them,
standing together, can pierce the First Shadow's shroud in the finale.

| Chapter | Theme / terrain | Boss (level 5) | Redeems |
|---|---|---|---|
| 1 — Whispers on the Wind | windy peaks | **Zephyra of the Hollow Sky** | Air Avatar |
| 2 — The Frozen Tide | glaciers, coast | **Kalani of the Weeping Ice** | Water Avatar |
| 3 — Kingdom of Dust | quarries, ruins | **Boruk, the Mountain That Walks** | Earth Avatar |
| 4 — Ashfall | volcanic | **Ashan, the Dawnbringer** | Fire Avatar |
| 5 — Harmonic Ruin | spirit wilds | **Umbriss, the First Shadow** | — (finale) |

Recurring voices: **Elder Miza** (mentor) and **Kesu** (scout), plus the
Avatars themselves once redeemed.

## How the systems work (all implemented)

- **Dialogue**: every level opens with a paused, typewriter dialogue scene
  (`DialogueUI` — builds its own panel if you don't style one; click/space
  advances, Esc skips).
- **Objectives**: destroy the enemy base, defeat the boss (boss-arena levels
  have no enemy base), or survive a timed assault.
- **Varied maps**: each level carries its own `mapSeed`; on a MapGenerator
  scene every level's terrain layout, biomes, villages and portals differ.
  Optionally give each chapter its own themed scene (see wiring below).
- **Chapter bosses**: `CorruptedAvatar` — that nation's Avatar prefab scaled
  into a boss (8× HP, 2.5× damage, larger), fighting for the hostile-spirits
  faction. It **phase-shifts at 75/50/25% health**, cycling its bent element
  and hitting harder each phase.
- **Redemption**: killing a chapter boss permanently unlocks that Avatar.
  In any later campaign level, summon redeemed Avatars with **F1 (Air),
  F2 (Water), F3 (Earth), F4 (Fire)** — or UI buttons wired to
  `CampaignManager.SummonRedeemedAvatar(0..3)`. One each per level, free.
- **Final boss**: `DarkSpiritBoss` (20× HP, summons dark spirits) is
  **invulnerable** — `IDamageInterceptor` zeroes all damage — unless
  **4 Avatars** stand within its shroud radius. Your own built Avatar counts,
  so: build yours, summon the other three redeemed ones (the finale's
  dialogue tells the player this).
- **Progression**: victories award **chi**. In the campaign menu, chi buys
  *permanent* levels of your nation's normal upgrade tracks
  (`CampaignMenuController.BuyPermanentUpgrade`) which auto-apply at the
  start of every campaign level. Save data (completed levels, chi, upgrades,
  redeemed Avatars) persists in PlayerPrefs via `CampaignProgress`.
- **Level gating**: levels unlock in order; chapter N+1 opens when chapter
  N's boss falls.

## Editor wiring (add to EDITOR_WIRING.md's pass as "Phase 10")

1. **Campaign menu panel** in `MainMenuScene` (open it from a new "Campaign"
   button beside Play):
   - a ScrollView; assign its **Content** to `CampaignMenuController.listParent`
   - one Button prefab with a TextMeshProUGUI child → `levelButtonPrefab`
     (the 5 chapter headers + 25 level buttons generate themselves)
   - a Play button → `PlaySelectedLevel`
   - optional: chi label, redeemed-Avatars label, info label, nation buttons
     → `SelectNation(0..3)`, upgrade-shop buttons → `BuyPermanentUpgrade(0..n)`,
     and a reset button → `ResetCampaign`
   - set **Default Level Scene Name** — your `RandomMap_Scene` is the best
     choice (every mission gets its own generated terrain via the level seed)
2. **Chapter-themed maps (recommended)**: make five variants of the random
   map scene, each with that chapter's biome prefab list emphasized
   (windy peaks / glaciers / quarries / volcanic / spirit wilds) and set
   `sceneName` per level in `DefaultCampaign.cs` (or leave empty for the
   default scene — seeds alone still vary the layout).
3. **Final boss prefab**: create a big dark-spirit prefab (DarkSpirit recipe
   from EDITOR_WIRING.md at larger scale + dramatic VFX) and save it at
   **`Assets/Resources/Campaign/FinalBoss.prefab`**. Optionally also
   `Assets/Resources/Campaign/DarkSpirit.prefab` for its summons (otherwise
   it summons nothing and relies on scene portals). Chapter bosses need no
   prefabs — they reuse each nation's Avatar prefab automatically.
4. That's it: `CampaignManager`, `DialogueUI`, bosses and saves all
   bootstrap themselves at runtime.

## Testing checklist

- [ ] Campaign menu lists 5 chapters / 25 levels; only 1-1 unlocked at first
- [ ] Level intro dialogue plays paused; Esc skips; game resumes after
- [ ] Beat 1-1 (survive) → chi awarded, 1-2 unlocks after returning to menu
- [ ] Chapter boss spawns far from your base, phase-shifts elements as it
      drops, and dies → console announces redemption; campaign menu lists it
- [ ] In a later level, F1 summons the redeemed Air Avatar (once only)
- [ ] Chi shop: buy a permanent upgrade → next mission starts with it applied
- [ ] Finale: Umbriss takes zero damage until four Avatars stand beside it;
      with all four gathered it can be brought down → victory + big reward
- [ ] Quit and relaunch Unity → progress persists

## Extending it

Levels are plain data — add a sixth chapter, remix enemies, or hand-author
`sceneName`s per level in `DefaultCampaign.cs`. For voice-over narration,
add an AudioClip field to `DialogueLine` and play it per line in
`DialogueUI.NextLine`.
