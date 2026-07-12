using System.Collections.Generic;

/// <summary>
/// The built-in 25-level campaign: five chapters of five levels, each chapter
/// ending in a boss. The barrier between worlds has ruptured; dark spirits
/// pour through, and four past Avatars have returned corrupted. Redeem them
/// one by one - each defeated Avatar joins your arsenal - then unite them
/// against Umbriss, the First Shadow, the source of the imbalance.
///
/// Everything here (titles, objectives, enemies, map seeds, dialogue) is
/// plain data - tweak freely. sceneName is left empty so levels load the
/// campaign menu's default scene; set per-chapter scenes there for themed
/// terrain, and the per-level mapSeed varies the generated layout.
/// Recurring voices: ELDER MIZA (your mentor), KESU (your scout).
/// </summary>
public static class DefaultCampaign
{
    private static DialogueLine D(string speaker, string text) => new DialogueLine(speaker, text);

    private static CampaignLevel L(string id, string title, string blurb,
        CampaignObjective objective, CampaignBoss boss, int seed, int chi,
        Nation[] enemies, params DialogueLine[] lines)
    {
        CampaignLevel level = new CampaignLevel
        {
            id = id,
            title = title,
            blurb = blurb,
            objective = objective,
            boss = boss,
            mapSeed = seed,
            chiReward = chi
        };
        if (enemies != null) level.enemyNations.AddRange(enemies);
        level.dialogue.AddRange(lines);
        return level;
    }

    public static List<CampaignChapter> Build()
    {
        List<CampaignChapter> chapters = new List<CampaignChapter>();

        // ==============================================================
        // PROLOGUE - BOOT CAMP
        // ==============================================================
        CampaignChapter c0 = new CampaignChapter
        {
            title = "Prologue — Boot Camp",
            description = "Before the sky tears open: Elder Miza drills you in the arts of command."
        };
        CampaignLevel bootCamp = L("c0l1", "The Proving Grounds",
            "Learn to command: movement, building, harvesting, and the Avatar's gifts.",
            CampaignObjective.DestroyEnemyBase, CampaignBoss.None, 50, 50,
            new[] { Nation.Fire },
            D("Elder Miza", "Every master was once a student, Commander. Today the mountain is your classroom."),
            D("Kesu", "I 'borrowed' a Fire Nation training camp for the final exam. They were... not using it politely."),
            D("Elder Miza", "Follow my instructions at the top of your vision. We begin with the simplest art: seeing your own people."));
        bootCamp.isTutorial = true;
        c0.levels.Add(bootCamp);
        chapters.Add(c0);

        // ==============================================================
        // CHAPTER 1 - WHISPERS ON THE WIND
        // ==============================================================
        CampaignChapter c1 = new CampaignChapter
        {
            title = "Chapter 1 — Whispers on the Wind",
            description = "The sky itself screamed the night the barrier tore. Dark spirits " +
                          "spill into the mountain passes, and something ancient stirs above the temples."
        };
        c1.levels.Add(L("c1l1", "The Night the Sky Tore",
            "Hold your home temple against the first wave of chaos.",
            CampaignObjective.Survive, CampaignBoss.None, 101, 100,
            new[] { Nation.Fire },
            D("Elder Miza", "Commander... the lights over the mountains are not an aurora. The barrier between worlds has ruptured."),
            D("Kesu", "Dark spirits in the passes - and Fire Nation raiders using the chaos as cover! They're coming NOW."),
            D("Elder Miza", "Then we hold. Train your benders, keep the temple standing. Balance begins with survival.")));
        c1.levels.Add(L("c1l2", "Embers in the Pass",
            "Push the Fire Nation raiders' outpost off the mountain.",
            CampaignObjective.DestroyEnemyBase, CampaignBoss.None, 102, 120,
            new[] { Nation.Fire },
            D("Kesu", "The raiders dug in overnight. A war camp, right beneath the shrine of the winds."),
            D("Elder Miza", "Every kingdom is grabbing what it can while the world reels. Show them the wind still has teeth."),
            D("Kesu", "Silver's short. Put workers on the spirit groves and take the villages - the tribes will pay tribute to whoever protects them.")));
        c1.levels.Add(L("c1l3", "The Silent Village",
            "A mountain tribe is beset by dark spirits. Drive them out and win the villagers' trust.",
            CampaignObjective.DestroyEnemyBase, CampaignBoss.None, 103, 140,
            new[] { Nation.Earth },
            D("Kesu", "That village went quiet three days ago. Now I know why - spirits circle it like vultures, and an Earth Kingdom warband is 'requisitioning' what's left."),
            D("Elder Miza", "Protect the villagers and they will remember it. Abandon them and the spirits won't be the only monsters in these mountains."),
            D("Kesu", "Watch the spirit portals. They keep... breathing.")));
        c1.levels.Add(L("c1l4", "Two Banners",
            "Fire and Earth war parties fight over the high passes. Outlast them both.",
            CampaignObjective.DestroyEnemyBase, CampaignBoss.None, 104, 160,
            new[] { Nation.Fire, Nation.Earth },
            D("Elder Miza", "Two armies, one pass, and a sky full of spirits. The old world is tearing itself apart over the scraps of the new one."),
            D("Kesu", "Let them bloody each other, then sweep the field? ...No? We're doing this the hard way? Wonderful."),
            D("Elder Miza", "There are whispers, Commander. A figure above the clouds, bending wind like a hurricane given a mind. The spirits flee FROM it.")));
        c1.levels.Add(L("c1l5", "Zephyra of the Hollow Sky",
            "BOSS: The corrupted Air Avatar descends. Free her from the shadow.",
            CampaignObjective.DefeatBoss, CampaignBoss.CorruptedAvatarAir, 105, 400,
            null,
            D("Zephyra", "Little gnats on MY mountain. I kept balance for a lifetime... and the world let it SHATTER."),
            D("Elder Miza", "By the winds... that is Avatar Zephyra - dead four hundred years. The rupture dragged her back and the shadow crawled in with her."),
            D("Kesu", "The shadow is riding her, not replacing her. Break its hold - beat it out of her if we must!"),
            D("Elder Miza", "Defeat her, Commander. Not to destroy her - to redeem her. We will need her before the end.")));
        chapters.Add(c1);

        // ==============================================================
        // CHAPTER 2 - THE FROZEN TIDE
        // ==============================================================
        CampaignChapter c2 = new CampaignChapter
        {
            title = "Chapter 2 — The Frozen Tide",
            description = "Redeemed, Zephyra speaks of three siblings lost to the same shadow. " +
                          "The trail leads south, where the ice groans and the sea has turned against the shore."
        };
        c2.levels.Add(L("c2l1", "Landfall on Black Ice",
            "Establish a foothold on the frozen coast.",
            CampaignObjective.DestroyEnemyBase, CampaignBoss.None, 201, 150,
            new[] { Nation.Water },
            D("Zephyra", "I remember this coast warm. The shadow remembers it too - it wears my sister Kalani now, and the sea obeys her grief."),
            D("Kesu", "The local warbands think we're invaders. To be fair... we are currently invading."),
            D("Elder Miza", "Take the shore, but spare what can be spared. These are the people we came to save.")));
        c2.levels.Add(L("c2l2", "The Drowned Fleet",
            "A rogue Water Tribe armada raids every village on the coast. Sink its harbor.",
            CampaignObjective.DestroyEnemyBase, CampaignBoss.None, 202, 170,
            new[] { Nation.Water },
            D("Kesu", "They call themselves the Drowned Fleet. Raiders who decided that if the world ends, they'd rather be rich for it."),
            D("Zephyra", "Kalani would have frozen their masts herself, once. Sink their harbor and the coast breathes again.")));
        c2.levels.Add(L("c2l3", "Glacier of Whispers",
            "Dark spirits pour from a portal deep in the ice fields. Fight through and hold.",
            CampaignObjective.Survive, CampaignBoss.None, 203, 190,
            new[] { Nation.Water },
            D("Elder Miza", "The portal in that glacier is old - older than the rupture. The rupture merely kicked the door open."),
            D("Kesu", "Water benders thrive out here on the ice, ours and theirs. Fire folk... less so. Terrain is a weapon, Commander - use theirs against them."),
            D("Zephyra", "Hold until the tide of spirits thins. I will keep the sky off your backs.")));
        c2.levels.Add(L("c2l4", "The Weeping Palace",
            "Kalani's honor guard - benders frozen in her grief - defend the approach to the ice palace.",
            CampaignObjective.DestroyEnemyBase, CampaignBoss.None, 204, 210,
            new[] { Nation.Water, Nation.Water },
            D("Zephyra", "Her palace weeps. Every icicle is a memory she cannot put down."),
            D("Kesu", "Two garrisons between us and the throne. And I SWEAR the glaciers are moving on their own."),
            D("Elder Miza", "Steel yourselves. Tomorrow we fight the tide itself.")));
        c2.levels.Add(L("c2l5", "Kalani of the Weeping Ice",
            "BOSS: The corrupted Water Avatar awaits among the glaciers.",
            CampaignObjective.DefeatBoss, CampaignBoss.CorruptedAvatarWater, 205, 400,
            null,
            D("Kalani", "The sea takes everything back, sister. Even us. ESPECIALLY us."),
            D("Zephyra", "Kalani! Fight it! The shadow is not your grief - it only feeds on it!"),
            D("Elder Miza", "Strike true, Commander. Every Avatar we free is a blade against the First Shadow - and a soul pulled from the dark.")));
        chapters.Add(c2);

        // ==============================================================
        // CHAPTER 3 - KINGDOM OF DUST
        // ==============================================================
        CampaignChapter c3 = new CampaignChapter
        {
            title = "Chapter 3 — Kingdom of Dust",
            description = "Two Avatars stand with you. In the shattered Earth Kingdom, warlords carve " +
                          "the land into fiefdoms while Boruk - the Mountain That Walks - sleeps beneath the quarries."
        };
        c3.levels.Add(L("c3l1", "The Broken Road",
            "ESCORT: bring the relief caravan through warlord territory to the golden beacon alive.",
            CampaignObjective.Escort, CampaignBoss.None, 301, 200,
            new[] { Nation.Earth },
            D("Kesu", "Every mile of this road has a new 'king' taxing it. And we're walking a relief caravan straight down the middle of it."),
            D("Elder Miza", "The caravan must reach the beacon at the far pass. If it falls, the villages east of here starve this winter."),
            D("Kalani", "The earth here is angry. Boruk's dreams bleed into the stone - the warlords are drunk on it without knowing why. Guard the caravan closely.")));
        c3.levels.Add(L("c3l2", "Salt and Crystal",
            "Seize the great crystal quarries before the warlords bleed them dry.",
            CampaignObjective.DestroyEnemyBase, CampaignBoss.None, 302, 220,
            new[] { Nation.Earth, Nation.Fire },
            D("Elder Miza", "Whoever holds the quarries funds the war. Miners, Commander - the dull work of digging wins more battles than any duel."),
            D("Kesu", "A Fire Nation 'trade legation' got here first. Their trade goods appear to be tanks.")));
        c3.levels.Add(L("c3l3", "The Hungry Dark",
            "A spirit portal has swallowed a mining town. Survive the night beside it.",
            CampaignObjective.Survive, CampaignBoss.None, 303, 240,
            new[] { Nation.Earth },
            D("Kesu", "The miners dug too close to something. The town is... gone isn't the right word. Occupied."),
            D("Zephyra", "The shadow grows bolder. It is learning to hold ground in the living world."),
            D("Kalani", "Then tonight we teach it the cost of rent.")));
        c3.levels.Add(L("c3l4", "The Walls of Doran-Su",
            "Break the fortress city of the strongest warlord before Boruk wakes.",
            CampaignObjective.DestroyEnemyBase, CampaignBoss.None, 304, 260,
            new[] { Nation.Earth, Nation.Earth },
            D("Elder Miza", "Doran-Su has never fallen. Its warlord thinks that means it never will."),
            D("Kesu", "Badgermoles under the walls, boulder-hurlers on top. I'll draft the letter of apology to the architecture."),
            D("Kalani", "Quickly. The quarries trembled all night. He is waking.")));
        c3.levels.Add(L("c3l5", "Boruk, the Mountain That Walks",
            "BOSS: The corrupted Earth Avatar rises from the deep quarry.",
            CampaignObjective.DefeatBoss, CampaignBoss.CorruptedAvatarEarth, 305, 400,
            null,
            D("Boruk", "I held this kingdom on my shoulders for SIXTY years. Let it learn what falls when the mountain kneels to no one."),
            D("Zephyra", "Boruk was the stubbornest of us in life. The shadow will not have made that better."),
            D("Elder Miza", "Patience and pressure, Commander. Even mountains erode.")));
        chapters.Add(c3);

        // ==============================================================
        // CHAPTER 4 - ASHFALL
        // ==============================================================
        CampaignChapter c4 = new CampaignChapter
        {
            title = "Chapter 4 — Ashfall",
            description = "Three Avatars march behind you. In the volcanic heartland of the Fire Nation, " +
                          "Ashan the Dawnbringer burns his own people's cities - to deny the shadow fuel, he says. The shadow says it louder."
        };
        c4.levels.Add(L("c4l1", "The Cinder Shore",
            "Land under the guns of the Fire Navy and take the beachhead.",
            CampaignObjective.DestroyEnemyBase, CampaignBoss.None, 401, 250,
            new[] { Nation.Fire },
            D("Kesu", "Welcome to the Fire Nation: the beaches are black, the sea is warm, and everything past the tide line wants us dead."),
            D("Boruk", "Their tanks cannot roll on shattered ground. Point me at the shore batteries.")));
        c4.levels.Add(L("c4l2", "The Coal Roads",
            "Cut the refinery lines feeding Ashan's war machine.",
            CampaignObjective.DestroyEnemyBase, CampaignBoss.None, 402, 270,
            new[] { Nation.Fire, Nation.Fire },
            D("Elder Miza", "An army of machines drinks coal like water. Dry the river."),
            D("Kesu", "Two refinery fortresses. War balloons overhead. Deep breaths, everyone.")));
        c4.levels.Add(L("c4l3", "Rain of Embers",
            "Ashan answers. Survive the firestorm assault on your forward camp.",
            CampaignObjective.Survive, CampaignBoss.None, 403, 290,
            new[] { Nation.Fire, Nation.Fire },
            D("Kalani", "The sky is raining fire. He knows we are here."),
            D("Zephyra", "Ashan was the kindest of us. Remember that when his legions come screaming - it is the shadow screaming, not him."),
            D("Elder Miza", "Towers, healers, and grit, Commander. Weather the storm.")));
        c4.levels.Add(L("c4l4", "The Obsidian Gate",
            "Storm the volcanic fortress guarding the caldera.",
            CampaignObjective.DestroyEnemyBase, CampaignBoss.None, 404, 310,
            new[] { Nation.Fire, Nation.Earth },
            D("Kesu", "The Gate has stood a thousand years. On the bright side, the lava moat is very scenic."),
            D("Boruk", "I raised half these walls for their grandfathers. I know where I left the cracks.")));
        c4.levels.Add(L("c4l5", "Ashan, the Dawnbringer",
            "BOSS: Face the corrupted Fire Avatar in the caldera itself.",
            CampaignObjective.DefeatBoss, CampaignBoss.CorruptedAvatarFire, 405, 400,
            null,
            D("Ashan", "I burned my own harbors so the shadow would starve! And still it whispers... so let it have ASHES."),
            D("Zephyra", "Brother, the fire was never the answer -"),
            D("Ashan", "The fire is the ONLY answer left."),
            D("Elder Miza", "Free him, Commander. The last ember of dawn is still in there somewhere - and without it, the final night wins.")));
        chapters.Add(c4);

        // ==============================================================
        // CHAPTER 5 - HARMONIC RUIN
        // ==============================================================
        CampaignChapter c5 = new CampaignChapter
        {
            title = "Chapter 5 — Harmonic Ruin",
            description = "Four redeemed Avatars stand together for the first time in history. Beyond the " +
                          "greatest rupture waits the thing that broke the world: Umbriss, the First Shadow."
        };
        c5.levels.Add(L("c5l1", "The Long March",
            "Cross the blighted borderlands where the spirit wilds bleed into the world.",
            CampaignObjective.DestroyEnemyBase, CampaignBoss.None, 501, 350,
            new[] { Nation.Fire, Nation.Earth },
            D("Elder Miza", "The land itself is wrong here - the wilds are wider every dawn. Warbands fight over the last clean water."),
            D("Ashan", "Then we march through the middle of their little war. I have been patient for one lifetime already.")));
        c5.levels.Add(L("c5l2", "The Spirit Choir",
            "Portals sing to each other across the wilds. Hold your camp through the longest night.",
            CampaignObjective.Survive, CampaignBoss.None, 502, 380,
            new[] { Nation.Water },
            D("Kesu", "The portals are... singing. Calling each other. Calling something ELSE."),
            D("Kalani", "It is a summons. Umbriss gathers its children home before the end."),
            D("Zephyra", "Then we make this camp the stone the tide breaks on.")));
        c5.levels.Add(L("c5l3", "The Puppet Courts",
            "The last warlords have sworn themselves to the shadow. End their courts.",
            CampaignObjective.DestroyEnemyBase, CampaignBoss.None, 503, 410,
            new[] { Nation.Earth, Nation.Water, Nation.Fire },
            D("Elder Miza", "Kings kneeling to the dark for one more day on their thrones. Pity them - after you defeat them."),
            D("Boruk", "Three courts, three armies, one afternoon. I have moved slower avalanches.")));
        c5.levels.Add(L("c5l4", "The Threshold",
            "Fight to the lip of the Great Rupture and plant your banner at the edge of the spirit world.",
            CampaignObjective.DestroyEnemyBase, CampaignBoss.None, 504, 450,
            new[] { Nation.Fire, Nation.Water },
            D("Kesu", "The rupture is a wound in the sky you could march a fleet through. And its guardians know we're coming."),
            D("Ashan", "Good. I want it watching when four Avatars walk in the front door."),
            D("Elder Miza", "Rest tonight, Commander. Tomorrow, balance itself is the battlefield.")));
        c5.levels.Add(L("c5l5", "Umbriss, the First Shadow",
            "FINAL BOSS: Only the four redeemed Avatars, standing together, can wound the First Shadow. Summon them all and end the imbalance.",
            CampaignObjective.DefeatBoss, CampaignBoss.DarkSpirit, 505, 1000,
            null,
            D("Umbriss", "Before your elements had names, I WAS. The rupture is not a wound, little flame - it is a door, and I have propped it open."),
            D("Elder Miza", "No mortal weapon will bite it. No single Avatar ever could - that is why it corrupted them the moment they returned."),
            D("Zephyra", "Together, then. Air to lift, water to bind, earth to hold, fire to burn."),
            D("Ashan", "Summon us all, Commander. Stand us side by side and the First Shadow learns why the world made Avatars at all."),
            D("Kesu", "For the villages. For the tribes. For EVERYONE. Light it up!")));
        chapters.Add(c5);

        ApplyDifficultyCurve(chapters);

        // Each chapter opens with a full-screen interlude on its first level.
        foreach (CampaignChapter chapter in chapters)
        {
            if (chapter.levels.Count == 0) continue;
            chapter.levels[0].interludeTitle = chapter.title;
            chapter.levels[0].interludeText = chapter.description;
        }
        return chapters;
    }

    /// <summary>
    /// Waves grow gently across the campaign: later chapters send bigger
    /// waves, sooner and more often. Levels keep any hand-set values by
    /// tweaking these after Build() if you want bespoke pacing.
    /// </summary>
    private static void ApplyDifficultyCurve(List<CampaignChapter> chapters)
    {
        for (int c = 0; c < chapters.Count; c++)
        {
            for (int l = 0; l < chapters[c].levels.Count; l++)
            {
                CampaignLevel level = chapters[c].levels[l];

                // Boot Camp stays gentle: one tiny wave, lots of breathing room.
                if (level.isTutorial)
                {
                    level.waveBaseSize = 1;
                    level.waveGrowth = 0;
                    level.waveInterval = 120f;
                    level.firstWaveDelay = 180f;
                    level.startingSilver = 700;
                    continue;
                }

                int tier = Mathf.Max(0, c - 1);                     // prologue doesn't count
                level.waveBaseSize = 2 + tier;                      // ch1: 2 ... ch5: 6
                level.waveGrowth = 1 + tier / 2;                    // ch1-2: +1, ch3-4: +2, ch5: +3
                level.waveInterval = Mathf.Max(40f, 75f - tier * 6f);
                level.firstWaveDelay = Mathf.Max(60f, 100f - tier * 8f);
                level.startingSilver = 600 + tier * 50;             // costs rise, so does the stake
            }
        }
    }
}
