using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ONE-CLICK STARTER PACK: builds every data asset and greybox prefab the
/// game needs, straight from the ROSTERS.md design sheet - no Unity MCP, no
/// hand-typing. Run from the menu:
///
///     Legends ► Bootstrap ALL (assets + prefabs + nations)
///
/// or run the numbered steps individually. Everything is IDEMPOTENT: re-run
/// any time; existing assets are updated in place, never duplicated, and
/// your art swaps (Model children) are preserved because unit prefabs are
/// only created when missing.
///
/// What it makes:
///  1. The full upgrade TECH TREE (~40 UpgradeData assets: branches,
///     prerequisites, exclusive locks, research buildings).
///  2. Greybox prefabs: units duplicated from AirbenderUnit (so the combat
///     Animator survives), buildings/neutrals/nodes from tinted primitives
///     following the Greybox Protocol (root + Model child; Air buildings
///     also get a disabled MountainModel perch variant).
///  3. Four NationData assets + the NationDatabase at
///     Assets/Resources/NationDatabase.asset, fully wired.
/// </summary>
public static class LegendsBootstrap
{
    private const string UpgradeFolder = "Assets/Nations/Upgrades";
    private const string NationFolder = "Assets/Nations";
    private const string PrefabFolder = "Assets/Prefabs/Greybox";
    private const string ResourcesFolder = "Assets/Resources";
    private const string CampaignFolder = "Assets/Resources/Campaign";
    private const string EmblemFolder = "Assets/UI/Emblems";
    private const string BaseUnitPrefabPath = "Assets/BenderPrefabs/AirbenderUnit.prefab";

    // ==================================================================
    // Menu
    // ==================================================================

    [MenuItem("Legends/Bootstrap ALL (assets + prefabs + nations)", priority = 0)]
    public static void BootstrapAll()
    {
        BootstrapUpgrades();
        BootstrapPrefabs();
        BootstrapNations();
        Debug.Log("[LegendsBootstrap] ALL DONE. Check Assets/Nations and Assets/Prefabs/Greybox, then play!");
    }

    [MenuItem("Legends/Bootstrap 1 — Upgrade Tech Trees", priority = 20)]
    public static void BootstrapUpgrades()
    {
        EnsureFolder(UpgradeFolder);
        BuildAirTree();
        BuildWaterTree();
        BuildEarthTree();
        BuildFireTree();
        AssetDatabase.SaveAssets();
        Debug.Log("[LegendsBootstrap] Upgrade tech trees created/updated.");
    }

    [MenuItem("Legends/Bootstrap 2 — Greybox Prefabs", priority = 21)]
    public static void BootstrapPrefabs()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(CampaignFolder);
        BuildNeutralPrefabs();
        foreach (Nation nation in new[] { Nation.Air, Nation.Water, Nation.Earth, Nation.Fire })
        {
            BuildUnitPrefabs(nation);
            BuildBuildingPrefabs(nation);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[LegendsBootstrap] Greybox prefabs created (existing ones left untouched).");
    }

    [MenuItem("Legends/Bootstrap 3 — Nations + Database", priority = 22)]
    public static void BootstrapNations()
    {
        EnsureFolder(NationFolder);
        EnsureFolder(ResourcesFolder);

        NationData air = BuildNation(Nation.Air, "Air Nomads",
            "Mobility and evasion: gliders, bison, mountain perches and the open sky.",
            new Color(0.95f, 0.8f, 0.2f));
        NationData water = BuildNation(Nation.Water, "Water Tribe",
            "Sustain and control: healing, freezes, and the long fight won by outlasting.",
            new Color(0.25f, 0.5f, 0.95f));
        NationData earth = BuildNation(Nation.Earth, "Earth Kingdom",
            "Toughness and siege: walls, tanks, and benders who make the ground itself fight.",
            new Color(0.3f, 0.75f, 0.3f));
        NationData fire = BuildNation(Nation.Fire, "Fire Nation",
            "Aggression and machines: burn, press, and never let them breathe.",
            new Color(0.9f, 0.25f, 0.2f));

        NationDatabase db = LoadOrCreate<NationDatabase>(ResourcesFolder + "/NationDatabase.asset");
        db.nations = new[] { air, water, earth, fire };
        EditorUtility.SetDirty(db);

        AssetDatabase.SaveAssets();
        Debug.Log("[LegendsBootstrap] Nations wired into Assets/Resources/NationDatabase.asset.");
    }

    // ==================================================================
    // 1. Upgrade tech trees (mirrors the ROSTERS.md tables)
    // ==================================================================

    private static void BuildAirTree()
    {
        Upg("air_dmg", "Tempest Training", "Monks strike harder.", 150, 3,
            u => { u.damageBonus = 0.15f; u.categories = Inf(); });
        Upg("air_speed", "Gale Stride", "Everything moves faster.", 200, 2,
            u => { u.damageBonus = 0f; u.speedBonus = 0.10f; u.categories = new UnitCategory[0]; });
        Upg("air_shields", "Unbending Wind", "Stronger wind shields (Q).", 175, 3, u =>
        {
            u.damageBonus = 0f; u.improvesShields = true; u.categories = Inf();
            u.restrictToUnitTypes = new[] { Unit.UnitType.Airbender };
            u.prerequisiteUpgradeIds = new[] { "air_dmg" };
        });
        Upg("air_gliders", "Staff Gliders", "BRANCH: airbenders unlock true FLIGHT on very long orders.", 250, 1, u =>
        {
            u.damageBonus = 0f; u.grantsGliderFlight = true; u.categories = Inf();
            u.restrictToUnitTypes = new[] { Unit.UnitType.Airbender };
            u.prerequisiteUpgradeIds = new[] { "air_speed" };
            u.exclusiveWithUpgradeIds = new[] { "air_tornado" };
            u.requiredBuildingKeyword = "Pavilion";
        });
        Upg("air_tornado", "Tornado Summoning", "BRANCH: airbenders periodically conjure tornadoes in combat.", 450, 1, u =>
        {
            u.damageBonus = 0f; u.grantsTornadoSummon = true; u.categories = Inf();
            u.restrictToUnitTypes = new[] { Unit.UnitType.Airbender };
            u.prerequisiteUpgradeIds = new[] { "air_shields" };
            u.exclusiveWithUpgradeIds = new[] { "air_gliders" };
            u.requiredBuildingKeyword = "Pavilion";
        });
        Upg("air_bison", "Bison Plate Barding", "Sky bison and lemurs wear armor.", 200, 2,
            u => { u.damageBonus = 0f; u.healthBonus = 0.2f; u.categories = new[] { UnitCategory.Animal }; u.requiredBuildingKeyword = "Stable"; });
        Battlements("air_battlements");
        AvatarTracks("air");
    }

    private static void BuildWaterTree()
    {
        Upg("water_dmg", "Moonlight Discipline", "Warriors strike harder.", 150, 3,
            u => { u.damageBonus = 0.15f; u.categories = Inf(); });
        Upg("water_hp", "Glacial Hide", "Beasts and ships endure.", 150, 2,
            u => { u.damageBonus = 0f; u.healthBonus = 0.15f; u.categories = new[] { UnitCategory.Animal, UnitCategory.Vehicle }; });
        Upg("water_healing", "Healing Waters", "BRANCH: every waterbender heals nearby allies.", 200, 3, u =>
        {
            u.damageBonus = 0f; u.grantsHealing = true; u.healPerSecondPerLevel = 2; u.categories = Inf();
            u.restrictToUnitTypes = new[] { Unit.UnitType.Waterbender };
            u.prerequisiteUpgradeIds = new[] { "water_dmg" };
            u.exclusiveWithUpgradeIds = new[] { "water_everfrost" };
            u.requiredBuildingKeyword = "Healing";
        });
        Upg("water_frost", "Frozen Grasp", "Freezes and chills last longer.", 220, 2, u =>
        {
            u.damageBonus = 0f; u.improvesFreezing = true; u.freezeDurationBonus = 0.35f; u.categories = Inf();
            u.restrictToUnitTypes = new[] { Unit.UnitType.Waterbender };
            u.prerequisiteUpgradeIds = new[] { "water_dmg" };
            u.requiredBuildingKeyword = "Moon";
        });
        Upg("water_everfrost", "Everfrost", "BRANCH ULTIMATE: freeze enemies solid ANYWHERE - no water needed.", 500, 1, u =>
        {
            u.damageBonus = 0f; u.grantsEverfrost = true; u.categories = Inf();
            u.restrictToUnitTypes = new[] { Unit.UnitType.Waterbender };
            u.prerequisiteUpgradeIds = new[] { "water_frost" };
            u.exclusiveWithUpgradeIds = new[] { "water_healing" };
            u.requiredBuildingKeyword = "Moon";
        });
        Upg("water_shields", "Deep Ice", "Stronger ice shields (Q).", 175, 3, u =>
        {
            u.damageBonus = 0f; u.improvesShields = true; u.categories = Inf();
            u.restrictToUnitTypes = new[] { Unit.UnitType.Waterbender };
        });
        Upg("water_hulls", "Reinforced Hulls", "Warships endure.", 200, 2,
            u => { u.damageBonus = 0f; u.healthBonus = 0.2f; u.categories = new[] { UnitCategory.Vehicle }; u.requiredBuildingKeyword = "Shipyard"; });
        Battlements("water_battlements");
        AvatarTracks("water");
    }

    private static void BuildEarthTree()
    {
        Upg("earth_hp", "Neutral Jing", "Soldiers endure.", 150, 3,
            u => { u.damageBonus = 0f; u.healthBonus = 0.15f; u.categories = Inf(); });
        Upg("earth_dmg", "Master Sculpting", "Soldiers strike harder.", 150, 3,
            u => { u.damageBonus = 0.15f; u.categories = Inf(); });
        Upg("earth_sensing", "Seismic Sensing", "Earthbenders see far through the fog.", 180, 2,
            u => { u.damageBonus = 0f; u.sightBonus = 0.3f; u.categories = Inf(); });
        Upg("earth_shields", "Mountain's Patience", "Stronger stone shields (Q).", 175, 3, u =>
        {
            u.damageBonus = 0f; u.improvesShields = true; u.categories = Inf();
            u.restrictToUnitTypes = new[] { Unit.UnitType.Earthbender };
        });
        Upg("earth_lava", "Lavabending", "BRANCH ULTIMATE: strikes ignite, splash molten rock, melt walls.", 500, 1, u =>
        {
            u.damageBonus = 0f; u.grantsLavaBending = true; u.categories = Inf();
            u.restrictToUnitTypes = new[] { Unit.UnitType.Earthbender };
            u.prerequisiteUpgradeIds = new[] { "earth_dmg" };
            u.exclusiveWithUpgradeIds = new[] { "earth_metal" };
            u.requiredBuildingKeyword = "Barracks";
        });
        Upg("earth_metal", "Metalbending", "BRANCH ULTIMATE: tear machines and fortifications apart.", 500, 1, u =>
        {
            u.damageBonus = 0f; u.grantsMetalBending = true; u.categories = Inf();
            u.restrictToUnitTypes = new[] { Unit.UnitType.Earthbender };
            u.prerequisiteUpgradeIds = new[] { "earth_hp" };
            u.exclusiveWithUpgradeIds = new[] { "earth_lava" };
            u.requiredBuildingKeyword = "Sanctum";
        });
        Upg("earth_tremor", "Mountain Breaker", "SHARED ULTIMATE: tremors shake perched enemy buildings apart.", 650, 1, u =>
        {
            u.damageBonus = 0f; u.grantsTremorAssault = true; u.categories = Inf();
            u.restrictToUnitTypes = new[] { Unit.UnitType.Earthbender };
            u.prerequisiteUpgradeIds = new[] { "earth_sensing" };
        });
        Upg("earth_plating", "Reinforced Hide Plates", "Beasts and tanks endure.", 200, 2,
            u => { u.damageBonus = 0f; u.healthBonus = 0.2f; u.categories = new[] { UnitCategory.Animal, UnitCategory.Vehicle }; });
        Upg("earth_siege", "Siege Engines", "Tanks hit harder.", 220, 2,
            u => { u.damageBonus = 0.2f; u.categories = new[] { UnitCategory.Vehicle }; });
        Battlements("earth_battlements");
        AvatarTracks("earth");
    }

    private static void BuildFireTree()
    {
        Upg("fire_dmg", "Sozin's Doctrine", "Soldiers strike harder.", 160, 3,
            u => { u.damageBonus = 0.15f; u.categories = Inf(); });
        Upg("fire_speed", "Forced March", "Infantry and cavalry move faster.", 150, 2,
            u => { u.damageBonus = 0f; u.speedBonus = 0.1f; u.categories = new[] { UnitCategory.Infantry, UnitCategory.Animal }; });
        Upg("fire_lightning", "Lightning Mastery", "Firebenders' bolts hit far harder.", 220, 2, u =>
        {
            u.damageBonus = 0.1f; u.categories = Inf();
            u.restrictToUnitTypes = new[] { Unit.UnitType.Firebender };
            u.prerequisiteUpgradeIds = new[] { "fire_dmg" };
            u.requiredBuildingKeyword = "Academy";
        });
        Upg("fire_redirect", "Lightning Redirection", "BRANCH ULTIMATE: catch lightning and hurl it back (Iroh's art).", 350, 2, u =>
        {
            u.damageBonus = 0f; u.grantsLightningRedirect = true; u.redirectChancePerLevel = 0.25f; u.categories = Inf();
            u.restrictToUnitTypes = new[] { Unit.UnitType.Firebender };
            u.prerequisiteUpgradeIds = new[] { "fire_lightning" };
            u.exclusiveWithUpgradeIds = new[] { "fire_dive" };
            u.requiredBuildingKeyword = "Academy";
        });
        Upg("fire_shields", "Inner Flame", "Stronger flame shields (Q).", 175, 3, u =>
        {
            u.damageBonus = 0f; u.improvesShields = true; u.categories = Inf();
            u.restrictToUnitTypes = new[] { Unit.UnitType.Firebender };
        });
        Upg("fire_dive", "Flame Dive", "BRANCH ULTIMATE: leap onto foes and slam down a burning fire ring.", 450, 1, u =>
        {
            u.damageBonus = 0f; u.grantsFlameDive = true; u.categories = Inf();
            u.restrictToUnitTypes = new[] { Unit.UnitType.Firebender };
            u.prerequisiteUpgradeIds = new[] { "fire_shields" };
            u.exclusiveWithUpgradeIds = new[] { "fire_redirect" };
            u.requiredBuildingKeyword = "Academy";
        });
        Upg("fire_plating", "Drill Plating", "War machines endure.", 200, 2,
            u => { u.damageBonus = 0f; u.healthBonus = 0.2f; u.categories = new[] { UnitCategory.Vehicle }; u.requiredBuildingKeyword = "Factory"; });
        Upg("fire_spray", "Dragon's Breath Nozzles", "War machines vent burning fuel at anything close.", 400, 1, u =>
        {
            u.damageBonus = 0f; u.grantsFireSpray = true; u.categories = new[] { UnitCategory.Vehicle };
            u.prerequisiteUpgradeIds = new[] { "fire_plating" };
            u.requiredBuildingKeyword = "Factory";
        });
        Battlements("fire_battlements");
        AvatarTracks("fire");
    }

    private static void Battlements(string id)
    {
        Upg(id, "Reinforced Battlements", "Towers hit harder, buildings stand longer.", 175, 3,
            u => { u.damageBonus = 0.15f; u.healthBonus = 0.15f; u.categories = new[] { UnitCategory.Building }; });
    }

    private static void AvatarTracks(string prefix)
    {
        Upg(prefix + "_avatar_spirit", "Spirit Communion",
            "AVATAR BRANCH: energy regenerates far faster (locks out Elemental Fury).", 300, 2, u =>
        {
            u.damageBonus = 0f; u.improvesAvatarEnergy = true; u.avatarEnergyRegenBonus = 0.3f;
            u.categories = new[] { UnitCategory.Avatar };
            u.exclusiveWithUpgradeIds = new[] { prefix + "_avatar_fury" };
        });
        Upg(prefix + "_avatar_fury", "Elemental Fury",
            "AVATAR BRANCH: Avatar State hits far harder (locks out Spirit Communion).", 300, 2, u =>
        {
            u.damageBonus = 0f; u.improvesAvatarPower = true; u.avatarStatePowerBonus = 0.15f;
            u.categories = new[] { UnitCategory.Avatar };
            u.exclusiveWithUpgradeIds = new[] { prefix + "_avatar_spirit" };
        });
    }

    private static UnitCategory[] Inf() => new[] { UnitCategory.Infantry };

    private static UpgradeData Upg(string id, string name, string description,
        int baseCost, int maxLevel, System.Action<UpgradeData> configure)
    {
        UpgradeData upgrade = LoadOrCreate<UpgradeData>($"{UpgradeFolder}/{id}.asset");
        upgrade.upgradeId = id;
        upgrade.displayName = name;
        upgrade.description = description;
        upgrade.baseCost = baseCost;
        upgrade.maxLevel = maxLevel;
        // Reset tree/grant fields so re-runs are deterministic.
        upgrade.prerequisiteUpgradeIds = new string[0];
        upgrade.exclusiveWithUpgradeIds = new string[0];
        upgrade.requiredBuildingKeyword = "";
        upgrade.restrictToUnitTypes = new Unit.UnitType[0];
        upgrade.healthBonus = 0f; upgrade.speedBonus = 0f; upgrade.sightBonus = 0f;
        upgrade.grantsHealing = false; upgrade.grantsLightningRedirect = false;
        upgrade.grantsMetalBending = false; upgrade.grantsLavaBending = false;
        upgrade.grantsTremorAssault = false; upgrade.grantsGliderFlight = false;
        upgrade.grantsTornadoSummon = false; upgrade.grantsEverfrost = false;
        upgrade.grantsFlameDive = false; upgrade.grantsFireSpray = false;
        upgrade.improvesShields = false; upgrade.improvesFreezing = false;
        upgrade.improvesAvatarEnergy = false; upgrade.improvesAvatarPower = false;
        configure(upgrade);
        EditorUtility.SetDirty(upgrade);
        return upgrade;
    }

    // ==================================================================
    // 2. Greybox prefabs
    // ==================================================================

    private static void BuildUnitPrefabs(Nation nation)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(BaseUnitPrefabPath) == null)
        {
            Debug.LogWarning($"[LegendsBootstrap] Base unit prefab missing at {BaseUnitPrefabPath} - skipping unit prefabs.");
            return;
        }

        string n = nation.ToString();
        Unit.UnitType benderType = BenderType(nation);

        // Bender - the basic infantry (slot 0).
        MakeUnitVariant($"{n}Bender", go =>
        {
            Unit unit = go.GetComponent<Unit>();
            unit.unitType = benderType; unit.category = UnitCategory.Infantry;
        });

        // Worker - no fists, just baskets.
        MakeUnitVariant($"{n}Worker", go =>
        {
            Unit unit = go.GetComponent<Unit>();
            unit.unitType = benderType; unit.category = UnitCategory.Worker;
            unit.maxUnitHealth = 60f;
            RemoveComponent<AttackController>(go);
            RemoveComponent<EnemyAI>(go);
            if (go.GetComponent<ResourceCollector>() == null) go.AddComponent<ResourceCollector>();
        });

        // Beast - the nation's animal (bison / polar bear dog / badgermole / rhino).
        MakeUnitVariant($"{n}Beast", go =>
        {
            Unit unit = go.GetComponent<Unit>();
            unit.unitType = benderType; unit.category = UnitCategory.Animal;
            unit.maxUnitHealth = 240f; unit.populationCost = 2;
            go.transform.localScale = go.transform.localScale * 1.35f;
            AttackController attack = go.GetComponent<AttackController>();
            if (attack != null) attack.unitDamage = 14;
            if (nation == Nation.Air && go.GetComponent<VisionSource>() == null)
            {
                go.AddComponent<VisionSource>(); // sky bison see far over the fog
            }
        });

        // War machine - the nation's vehicle.
        MakeUnitVariant($"{n}Warmachine", go =>
        {
            Unit unit = go.GetComponent<Unit>();
            unit.unitType = benderType; unit.category = UnitCategory.Vehicle;
            unit.maxUnitHealth = 300f; unit.populationCost = 3;
            go.transform.localScale = go.transform.localScale * 1.5f;
            AttackController attack = go.GetComponent<AttackController>();
            if (attack != null) attack.unitDamage = 16;
        });

        // The Avatar.
        MakeUnitVariant($"{n}Avatar", go =>
        {
            Unit unit = go.GetComponent<Unit>();
            unit.unitType = benderType; unit.category = UnitCategory.Avatar;
            unit.maxUnitHealth = 400f; unit.populationCost = 5;
            AttackController attack = go.GetComponent<AttackController>();
            if (attack != null) attack.unitDamage = 25;

            AvatarUnit avatar = go.GetComponent<AvatarUnit>();
            if (avatar == null) avatar = go.AddComponent<AvatarUnit>();
            avatar.airEffect = EnsureChild(go, "AirEffect");
            avatar.waterEffect = EnsureChild(go, "WaterEffect");
            avatar.earthEffect = EnsureChild(go, "EarthEffect");
            avatar.fireEffect = EnsureChild(go, "FireEffect");
            avatar.avatarStateAura = EnsureChild(go, "AvatarStateAura");
        });
    }

    /// <summary>Duplicates the base bender prefab (keeping its combat Animator)
    /// into PrefabFolder/name.prefab and applies `configure`. Skips existing
    /// prefabs so your art/stat edits survive re-runs.</summary>
    private static void MakeUnitVariant(string name, System.Action<GameObject> configure)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        GameObject contents = PrefabUtility.LoadPrefabContents(BaseUnitPrefabPath);
        contents.name = name;
        if (contents.GetComponent<NationColorizer>() == null) contents.AddComponent<NationColorizer>();
        configure(contents);
        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static void BuildBuildingPrefabs(Nation nation)
    {
        string n = nation.ToString();
        bool air = nation == Nation.Air;

        // Command center: big box + CommandCenter + dropoff + housing.
        MakeBuilding($"{n}CommandCenter", PrimitiveType.Cube, new Vector3(6f, 4f, 6f), air, go =>
        {
            go.AddComponent<CommandCenter>();
            go.AddComponent<ResourceDropoff>();
            go.AddComponent<PopulationHousing>().populationProvided = 20;
        });

        // Defense tower with the nation's element.
        MakeBuilding($"{n}Tower", PrimitiveType.Cylinder, new Vector3(1.6f, 3.5f, 1.6f), air, go =>
        {
            Structure s = go.AddComponent<Structure>(); s.maxHealth = 350f;
            DefenseTower tower = go.AddComponent<DefenseTower>();
            tower.element = TowerElementFor(nation);
            go.AddComponent<VisionSource>();
        });

        // Housing.
        MakeBuilding($"{n}Housing", PrimitiveType.Cube, new Vector3(3f, 2f, 3f), air, go =>
        {
            Structure s = go.AddComponent<Structure>(); s.maxHealth = 250f;
            go.AddComponent<PopulationHousing>().populationProvided = 10;
        });

        // Production hall (trains the roster via QueueRosterUnit indices).
        // NOTE: prefab names carry the research keywords (Stable, Barracks,
        // Academy...) because the tech tree checks the names of buildings
        // you OWN, and instantiated buildings are named after their prefab.
        MakeBuilding(ProductionPrefabName(nation), PrimitiveType.Cube, new Vector3(4.5f, 2.5f, 4.5f), air, go =>
        {
            Structure s = go.AddComponent<Structure>(); s.maxHealth = 450f;
            go.AddComponent<UnitSpawner>();
        });

        // Economy building tied to the nation's favorite node.
        MakeBuilding($"{n}Economy", PrimitiveType.Cylinder, new Vector3(2.5f, 1.5f, 2.5f), air, go =>
        {
            Structure s = go.AddComponent<Structure>(); s.maxHealth = 300f;
            IncomeBuilding income = go.AddComponent<IncomeBuilding>();
            income.incomePerTick = 8; income.tickInterval = 6f;
            income.requiresNearbyNode = true;
            income.requiredNodeType = FavoriteNode(nation);
            go.AddComponent<ResourceDropoff>();
        });

        // The superweapon shrine (also the research hall for keyworded branches).
        MakeBuilding(SpecialBuildingName(nation), PrimitiveType.Capsule, new Vector3(3f, 3f, 3f), air, go =>
        {
            Structure s = go.AddComponent<Structure>(); s.maxHealth = 500f;
            Superweapon weapon = go.AddComponent<Superweapon>();
            weapon.power = SuperweaponFor(nation);
        });

        // Nation-specific extras.
        if (nation == Nation.Air)
        {
            MakeBuilding("AirSkyMooring", PrimitiveType.Cylinder, new Vector3(2f, 2.5f, 2f), true, go =>
            {
                Structure s = go.AddComponent<Structure>(); s.maxHealth = 220f;
                go.AddComponent<AirSupplyPost>();
            });
            MakeBuilding("AirMeditationPavilion", PrimitiveType.Cylinder, new Vector3(3f, 1.2f, 3f), true, go =>
            {
                Structure s = go.AddComponent<Structure>(); s.maxHealth = 260f;
                IncomeBuilding income = go.AddComponent<IncomeBuilding>();
                income.incomePerTick = 8; income.tickInterval = 6f;
            });
        }
        if (nation == Nation.Water)
        {
            MakeBuilding("WaterHealingHut", PrimitiveType.Capsule, new Vector3(2.5f, 2f, 2.5f), false, go =>
            {
                Structure s = go.AddComponent<Structure>(); s.maxHealth = 280f;
                go.AddComponent<UnitSpawner>();
                Healer aura = go.AddComponent<Healer>(); aura.healRadius = 10f; aura.healAmount = 2;
            });
            MakeBuilding("WaterFishingPier", PrimitiveType.Cube, new Vector3(3.5f, 1f, 2f), false, go =>
            {
                Structure s = go.AddComponent<Structure>(); s.maxHealth = 220f;
                go.AddComponent<FishingPier>();
            });
            MakeBuilding("WaterShipyard", PrimitiveType.Cube, new Vector3(4.5f, 2f, 3.5f), false, go =>
            {
                Structure s = go.AddComponent<Structure>(); s.maxHealth = 420f;
                go.AddComponent<UnitSpawner>();
            });
        }
        if (nation == Nation.Fire)
        {
            MakeBuilding("FireWarFactory", PrimitiveType.Cube, new Vector3(5f, 2.5f, 4f), false, go =>
            {
                Structure s = go.AddComponent<Structure>(); s.maxHealth = 500f;
                go.AddComponent<UnitSpawner>();
            });
        }
        if (nation == Nation.Earth)
        {
            MakeBuilding("EarthStoneWall", PrimitiveType.Cube, new Vector3(4f, 2.2f, 0.8f), false, go =>
            {
                Structure s = go.AddComponent<Structure>(); s.maxHealth = 400f;
                go.AddComponent<RequiresBenderPresence>();
            });
            MakeBuilding("EarthGate", PrimitiveType.Cube, new Vector3(4f, 2.2f, 0.8f), false, go =>
            {
                Structure s = go.AddComponent<Structure>(); s.maxHealth = 450f;
                go.AddComponent<RequiresBenderPresence>();
                go.AddComponent<Gate>();
            });
        }

        // Point the Earth superweapon's rampart at the wall prefab.
        if (nation == Nation.Earth)
        {
            GameObject sanctum = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/EarthDeepSanctum.prefab");
            GameObject wall = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/EarthStoneWall.prefab");
            if (sanctum != null && wall != null)
            {
                Superweapon weapon = sanctum.GetComponent<Superweapon>();
                if (weapon != null && weapon.wallPrefab == null)
                {
                    weapon.wallPrefab = wall;
                    EditorUtility.SetDirty(sanctum);
                }
            }
        }
    }

    private static void BuildNeutralPrefabs()
    {
        // Villager: a small gray wanderer.
        MakeSimpleUnit("Villager", PrimitiveType.Capsule, 0.8f, new Color(0.65f, 0.65f, 0.6f), go =>
        {
            Unit unit = go.GetComponent<Unit>();
            unit.unitType = Unit.UnitType.Villager; unit.maxUnitHealth = 30f;
            go.AddComponent<Villager>();
        });

        // Friendly spirit: soft glow-white, tameable by the Avatar.
        MakeSimpleUnit("FriendlySpirit", PrimitiveType.Sphere, 1.1f, new Color(0.85f, 0.95f, 1f), go =>
        {
            Unit unit = go.GetComponent<Unit>();
            unit.unitType = Unit.UnitType.Spirit; unit.maxUnitHealth = 80f; unit.killBounty = 15;
            go.AddComponent<Spirit>().alignment = Spirit.Alignment.Friendly;
            go.AddComponent<Tameable>();
        });

        // Dark spirit: duplicated from the base bender so it can FIGHT.
        if (AssetDatabase.LoadAssetAtPath<GameObject>(BaseUnitPrefabPath) != null)
        {
            MakeUnitVariant("DarkSpirit", go =>
            {
                Unit unit = go.GetComponent<Unit>();
                unit.unitType = Unit.UnitType.Spirit; unit.category = UnitCategory.Infantry;
                unit.maxUnitHealth = 120f; unit.killBounty = 40;
                go.AddComponent<Spirit>().alignment = Spirit.Alignment.Dark;
                Tameable tame = go.AddComponent<Tameable>();
                tame.maxHealthFractionToTame = 0.5f;
            });

            // Campaign copies: boss-arena summons + the final boss (scaled up).
            CopyPrefab($"{PrefabFolder}/DarkSpirit.prefab", $"{CampaignFolder}/DarkSpirit.prefab");
            CopyPrefab($"{PrefabFolder}/DarkSpirit.prefab", $"{CampaignFolder}/FinalBoss.prefab", go =>
            {
                go.transform.localScale = go.transform.localScale * 3f;
                Unit unit = go.GetComponent<Unit>();
                unit.maxUnitHealth = 2400f; unit.killBounty = 500;
            });
        }

        // Village: a hut cluster with a control radius.
        string villagePath = $"{PrefabFolder}/Village.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(villagePath) == null)
        {
            GameObject village = new GameObject("Village");
            for (int i = 0; i < 3; i++)
            {
                GameObject hut = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hut.name = "Model";
                hut.transform.SetParent(village.transform, false);
                hut.transform.localPosition = Quaternion.Euler(0f, i * 120f, 0f) * Vector3.forward * 2.5f;
                hut.transform.localScale = new Vector3(2f, 1.6f, 2f);
                Tint(hut, new Color(0.6f, 0.5f, 0.38f));
            }
            Village v = village.AddComponent<Village>();
            v.villagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Villager.prefab");
            PrefabUtility.SaveAsPrefabAsset(village, villagePath);
            Object.DestroyImmediate(village);
        }

        // Spirit portal: a greybox torii gate + the travel/spawn logic.
        string portalPath = $"{PrefabFolder}/SpiritPortal.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(portalPath) == null)
        {
            GameObject portal = new GameObject("SpiritPortal");
            Color spirit = new Color(0.55f, 0.3f, 0.8f);
            AddPost(portal, new Vector3(-1.5f, 1.5f, 0f), new Vector3(0.3f, 3f, 0.3f), spirit);
            AddPost(portal, new Vector3(1.5f, 1.5f, 0f), new Vector3(0.3f, 3f, 0.3f), spirit);
            AddPost(portal, new Vector3(0f, 3.1f, 0f), new Vector3(4.2f, 0.3f, 0.4f), spirit);
            SpiritPortal sp = portal.AddComponent<SpiritPortal>();
            sp.friendlySpiritPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/FriendlySpirit.prefab");
            sp.darkSpiritPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/DarkSpirit.prefab");
            PrefabUtility.SaveAsPrefabAsset(portal, portalPath);
            Object.DestroyImmediate(portal);
        }

        // The four resource nodes.
        MakeNode("SpiritGroveNode", ResourceNode.NodeType.SpiritGrove, new Color(0.6f, 0.9f, 0.7f));
        MakeNode("FishShoalNode", ResourceNode.NodeType.FishShoal, new Color(0.4f, 0.7f, 1f));
        MakeNode("CrystalDepositNode", ResourceNode.NodeType.CrystalDeposit, new Color(0.5f, 1f, 0.6f));
        MakeNode("CoalSeamNode", ResourceNode.NodeType.CoalSeam, new Color(0.25f, 0.25f, 0.28f));
    }

    // ==================================================================
    // 3. NationData wiring
    // ==================================================================

    private static NationData BuildNation(Nation nation, string displayName, string description, Color color)
    {
        string n = nation.ToString();
        NationData data = LoadOrCreate<NationData>($"{NationFolder}/{n}Nation.asset");
        data.nation = nation;
        data.displayName = displayName;
        data.description = description;
        data.themeColor = color;
        data.commandCenterCost = 400;
        data.commandCenterPrefab = Prefab($"{n}CommandCenter") ?? data.commandCenterPrefab;
        data.towerPrefab = Prefab($"{n}Tower") ?? data.towerPrefab;

        Sprite emblem = LoadEmblem(nation);
        if (emblem != null) data.emblem = emblem;

        // Roster (slot 0 = basic bender, Avatar LAST - UI buttons index these).
        var units = new List<NationData.UnitEntry>
        {
            UnitEntry(BenderName(nation), $"{n}Bender", BenderCost(nation), 3f, UnitCategory.Infantry),
            UnitEntry(WorkerName(nation), $"{n}Worker", 40, 4f, UnitCategory.Worker),
            UnitEntry(BeastName(nation), $"{n}Beast", BeastCost(nation), 10f, UnitCategory.Animal),
            UnitEntry(VehicleName(nation), $"{n}Warmachine", VehicleCost(nation), 11f, UnitCategory.Vehicle),
            UnitEntry($"Avatar ({displayName})", $"{n}Avatar", 600, 25f, UnitCategory.Avatar),
        };
        units.RemoveAll(u => u == null);
        data.units = units.ToArray();

        // Buildings (names carry the research keywords: Pavilion, Moon, Barracks...).
        var buildings = new List<NationData.BuildingEntry>();
        AddBuilding(buildings, EconomyBuildingName(nation), $"{n}Economy", EconomyCost(nation), BuildingCategory.Economy);
        AddBuilding(buildings, ProductionBuildingName(nation), ProductionPrefabName(nation), ProductionCost(nation), BuildingCategory.Production);
        AddBuilding(buildings, TowerName(nation), $"{n}Tower", 140, BuildingCategory.Defense);
        AddBuilding(buildings, HousingName(nation), $"{n}Housing", 100, BuildingCategory.Special);
        AddBuilding(buildings, SpecialDisplayName(nation), SpecialBuildingName(nation), 250, BuildingCategory.Special);
        switch (nation)
        {
            case Nation.Air:
                AddBuilding(buildings, "Meditation Pavilion", "AirMeditationPavilion", 120, BuildingCategory.Economy);
                AddBuilding(buildings, "Sky Mooring", "AirSkyMooring", 180, BuildingCategory.Special);
                break;
            case Nation.Water:
                AddBuilding(buildings, "Healing Hut", "WaterHealingHut", 150, BuildingCategory.Production);
                AddBuilding(buildings, "Shipyard", "WaterShipyard", 200, BuildingCategory.Production);
                AddBuilding(buildings, "Fishing Pier", "WaterFishingPier", 150, BuildingCategory.Economy);
                break;
            case Nation.Earth:
                AddBuilding(buildings, "Stone Wall", "EarthStoneWall", 60, BuildingCategory.Defense);
                AddBuilding(buildings, "Earth Gate", "EarthGate", 120, BuildingCategory.Defense);
                break;
            case Nation.Fire:
                AddBuilding(buildings, "War Factory", "FireWarFactory", 220, BuildingCategory.Production);
                break;
        }
        data.buildings = buildings.ToArray();

        // Upgrades: everything with this nation's prefix, tree order.
        data.upgrades = LoadUpgrades(nation);

        EditorUtility.SetDirty(data);
        return data;
    }

    private static UpgradeData[] LoadUpgrades(Nation nation)
    {
        string prefix = nation.ToString().ToLowerInvariant() + "_";
        var found = new List<UpgradeData>();
        foreach (string guid in AssetDatabase.FindAssets("t:UpgradeData", new[] { UpgradeFolder }))
        {
            UpgradeData upgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>(AssetDatabase.GUIDToAssetPath(guid));
            if (upgrade != null && upgrade.upgradeId.StartsWith(prefix)) found.Add(upgrade);
        }
        found.Sort((a, b) => string.Compare(a.upgradeId, b.upgradeId, System.StringComparison.Ordinal));
        return found.ToArray();
    }

    // ==================================================================
    // Greybox helpers
    // ==================================================================

    /// <summary>Root + tinted Model child + collider + NationColorizer,
    /// plus a disabled MountainModel perch variant for Air buildings.</summary>
    private static void MakeBuilding(string name, PrimitiveType primitive, Vector3 size,
        bool airPerchVariant, System.Action<GameObject> configure)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        GameObject root = new GameObject(name);

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, size.y * 0.5f, 0f);
        collider.size = size;

        GameObject model = GameObject.CreatePrimitive(primitive);
        model.name = "Model";
        Object.DestroyImmediate(model.GetComponent<Collider>());
        model.transform.SetParent(root.transform, false);
        model.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
        model.transform.localScale = size;
        model.AddComponent<NationColorizer>();

        if (airPerchVariant)
        {
            // The mountainside design: the same hall raised on stilts with a
            // wider roof - visibly different from the flat-ground look.
            GameObject perch = new GameObject("MountainModel");
            perch.transform.SetParent(root.transform, false);

            GameObject hall = GameObject.CreatePrimitive(primitive);
            hall.name = "PerchHall";
            Object.DestroyImmediate(hall.GetComponent<Collider>());
            hall.transform.SetParent(perch.transform, false);
            hall.transform.localPosition = new Vector3(0f, size.y * 0.5f + 1.2f, 0f);
            hall.transform.localScale = new Vector3(size.x * 0.85f, size.y * 0.85f, size.z * 0.85f);
            hall.AddComponent<NationColorizer>();

            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            roof.name = "PerchRoof";
            Object.DestroyImmediate(roof.GetComponent<Collider>());
            roof.transform.SetParent(perch.transform, false);
            roof.transform.localPosition = new Vector3(0f, size.y + 1.6f, 0f);
            roof.transform.localScale = new Vector3(size.x * 1.4f, 0.15f, size.z * 1.4f);
            Tint(roof, new Color(0.9f, 0.75f, 0.25f));

            for (int i = 0; i < 4; i++)
            {
                GameObject stilt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stilt.name = "PerchStilt";
                Object.DestroyImmediate(stilt.GetComponent<Collider>());
                stilt.transform.SetParent(perch.transform, false);
                float sx = (i % 2 == 0 ? 1f : -1f) * size.x * 0.35f;
                float sz = (i < 2 ? 1f : -1f) * size.z * 0.35f;
                stilt.transform.localPosition = new Vector3(sx, 0.6f, sz);
                stilt.transform.localScale = new Vector3(0.2f, 1.3f, 0.2f);
                Tint(stilt, new Color(0.5f, 0.42f, 0.3f));
            }

            perch.SetActive(false); // MountainPerch.Apply enables it on slopes
        }

        configure(root);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    /// <summary>A from-scratch non-combat unit (villager, friendly spirit).</summary>
    private static void MakeSimpleUnit(string name, PrimitiveType primitive, float scale,
        Color color, System.Action<GameObject> configure)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        GameObject root = new GameObject(name);
        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.height = 2f * scale;
        collider.center = new Vector3(0f, scale, 0f);

        GameObject model = GameObject.CreatePrimitive(primitive);
        model.name = "Model";
        Object.DestroyImmediate(model.GetComponent<Collider>());
        model.transform.SetParent(root.transform, false);
        model.transform.localPosition = new Vector3(0f, scale, 0f);
        model.transform.localScale = Vector3.one * scale;
        Tint(model, color);
        model.AddComponent<NationColorizer>();

        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.speed = 3f;
        root.AddComponent<Unit>();
        configure(root);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void MakeNode(string name, ResourceNode.NodeType type, Color color)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        GameObject root = new GameObject(name);
        SphereCollider collider = root.AddComponent<SphereCollider>();
        collider.radius = 1.5f;

        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        model.name = "Model";
        Object.DestroyImmediate(model.GetComponent<Collider>());
        model.transform.SetParent(root.transform, false);
        model.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        model.transform.localScale = new Vector3(2.4f, 1.2f, 2.4f);
        Tint(model, color);

        root.AddComponent<ResourceNode>().nodeType = type;
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void CopyPrefab(string fromPath, string toPath, System.Action<GameObject> mutate = null)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(toPath) != null) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(fromPath) == null) return;

        AssetDatabase.CopyAsset(fromPath, toPath);
        if (mutate != null)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(toPath);
            mutate(contents);
            PrefabUtility.SaveAsPrefabAsset(contents, toPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static void AddPost(GameObject parent, Vector3 position, Vector3 scale, Color color)
    {
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = "Model";
        Object.DestroyImmediate(post.GetComponent<Collider>());
        post.transform.SetParent(parent.transform, false);
        post.transform.localPosition = position;
        post.transform.localScale = scale;
        Tint(post, color);
    }

    private static GameObject EnsureChild(GameObject parent, string name)
    {
        Transform existing = parent.transform.Find(name);
        if (existing != null) return existing.gameObject;
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        child.SetActive(false);
        return child;
    }

    private static void RemoveComponent<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component != null) Object.DestroyImmediate(component);
    }

    /// <summary>Persistent tint: prefab-referenced materials must be ASSETS
    /// (in-memory materials and property blocks don't survive saving), so
    /// each color becomes a small shared .mat in Assets/Nations/Materials.</summary>
    private static void Tint(GameObject go, Color color)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;

        EnsureFolder(NationFolder + "/Materials");
        string key = ColorUtility.ToHtmlStringRGB(color);
        string path = $"{NationFolder}/Materials/grey_{key}.mat";

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
        }
        renderer.sharedMaterial = material;
    }

    // ==================================================================
    // Asset plumbing
    // ==================================================================

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    private static GameObject Prefab(string name)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{name}.prefab");
    }

    private static NationData.UnitEntry UnitEntry(string name, string prefabName, int cost, float buildTime, UnitCategory category)
    {
        GameObject prefab = Prefab(prefabName);
        if (prefab == null) return null;
        return new NationData.UnitEntry
        {
            unitName = name, prefab = prefab, cost = cost, buildTime = buildTime, category = category
        };
    }

    private static void AddBuilding(List<NationData.BuildingEntry> list, string name, string prefabName, int cost, BuildingCategory category)
    {
        GameObject prefab = Prefab(prefabName);
        if (prefab == null) return;
        list.Add(new NationData.BuildingEntry { buildingName = name, prefab = prefab, cost = cost, category = category });
    }

    private static Sprite LoadEmblem(Nation nation)
    {
        string path = $"{EmblemFolder}/{nation.ToString().ToLowerInvariant()}_emblem.png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ==================================================================
    // Flavor tables
    // ==================================================================

    private static Unit.UnitType BenderType(Nation n) => n switch
    {
        Nation.Water => Unit.UnitType.Waterbender,
        Nation.Earth => Unit.UnitType.Earthbender,
        Nation.Fire => Unit.UnitType.Firebender,
        _ => Unit.UnitType.Airbender,
    };

    private static DefenseTower.TowerElement TowerElementFor(Nation n) => n switch
    {
        Nation.Water => DefenseTower.TowerElement.Water,
        Nation.Earth => DefenseTower.TowerElement.Earth,
        Nation.Fire => DefenseTower.TowerElement.Fire,
        _ => DefenseTower.TowerElement.Air,
    };

    private static Superweapon.PowerType SuperweaponFor(Nation n) => n switch
    {
        Nation.Water => Superweapon.PowerType.FlashFreeze,
        Nation.Earth => Superweapon.PowerType.StoneRampart,
        Nation.Fire => Superweapon.PowerType.CometBarrage,
        _ => Superweapon.PowerType.GreatStorm,
    };

    private static ResourceNode.NodeType FavoriteNode(Nation n) => n switch
    {
        Nation.Water => ResourceNode.NodeType.FishShoal,
        Nation.Earth => ResourceNode.NodeType.CrystalDeposit,
        Nation.Fire => ResourceNode.NodeType.CoalSeam,
        _ => ResourceNode.NodeType.SpiritGrove,
    };

    private static string BenderName(Nation n) => n switch
    {
        Nation.Water => "Waterbender Warrior",
        Nation.Earth => "Earthbender Soldier",
        Nation.Fire => "Firebender Soldier",
        _ => "Airbender Monk",
    };

    private static string WorkerName(Nation n) => n switch
    {
        Nation.Water => "Fisherman",
        Nation.Earth => "Miner",
        Nation.Fire => "Coal Engineer",
        _ => "Air Acolyte",
    };

    private static string BeastName(Nation n) => n switch
    {
        Nation.Water => "Polar Bear Dog",
        Nation.Earth => "Badgermole",
        Nation.Fire => "Komodo Rhino",
        _ => "Sky Bison",
    };

    private static string VehicleName(Nation n) => n switch
    {
        Nation.Water => "Ice Cutter",
        Nation.Earth => "Earth Tank",
        Nation.Fire => "Tundra Tank",
        _ => "War Glider",
    };

    private static int BenderCost(Nation n) => n == Nation.Air ? 50 : n == Nation.Earth ? 60 : 55;
    private static int BeastCost(Nation n) => n switch { Nation.Air => 200, Nation.Water => 130, Nation.Earth => 220, _ => 120 };
    private static int VehicleCost(Nation n) => n switch { Nation.Air => 150, Nation.Water => 170, Nation.Earth => 190, _ => 180 };
    private static int EconomyCost(Nation n) => n == Nation.Air ? 120 : 140;
    private static int ProductionCost(Nation n) => n switch { Nation.Air => 180, Nation.Water => 150, _ => 160 };

    private static string EconomyBuildingName(Nation n) => n switch
    {
        Nation.Water => "Fishing Dock",
        Nation.Earth => "Crystal Mine",
        Nation.Fire => "Coal Refinery",
        _ => "Spirit Grove Pavilion",
    };

    private static string ProductionBuildingName(Nation n) => n switch
    {
        Nation.Water => "Warrior Lodge",
        Nation.Earth => "Barracks",
        Nation.Fire => "War Academy",
        _ => "Bison Stable",
    };

    /// <summary>Prefab asset names double as research keywords: the tech tree
    /// checks owned buildings' names (Stable / Barracks / Academy / Factory...).</summary>
    private static string ProductionPrefabName(Nation n) => n switch
    {
        Nation.Water => "WaterWarriorLodge",
        Nation.Earth => "EarthBarracks",
        Nation.Fire => "FireWarAcademy",
        _ => "AirBisonStable",
    };

    private static string TowerName(Nation n) => n switch
    {
        Nation.Water => "Ice Spike Tower",
        Nation.Earth => "Rock Launcher Tower",
        Nation.Fire => "Flame Turret",
        _ => "Wind Cannon Pagoda",
    };

    private static string HousingName(Nation n) => n switch
    {
        Nation.Water => "Tribal Lodge",
        Nation.Earth => "Stone Tenements",
        Nation.Fire => "Garrison Quarters",
        _ => "Nomad Dormitories",
    };

    private static string SpecialBuildingName(Nation n) => n switch
    {
        Nation.Water => "WaterMoonShrine",
        Nation.Earth => "EarthDeepSanctum",
        Nation.Fire => "FireWarSanctum",
        _ => "AirSpiritShrine",
    };

    private static string SpecialDisplayName(Nation n) => n switch
    {
        Nation.Water => "Moon Shrine",
        Nation.Earth => "Deep Sanctum",
        Nation.Fire => "War Sanctum",
        _ => "Spirit Shrine",
    };
}
