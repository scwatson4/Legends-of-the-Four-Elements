using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Runs one campaign level from launch to victory:
///  - configures GameSetup and loads the level scene
///  - applies the player's permanent (chi-bought) upgrades
///  - plays the level's opening dialogue (game paused)
///  - spawns the chapter boss and tracks the objective
///    (destroy base / defeat boss / survive)
///  - on victory: saves progress, awards chi, and redeems defeated Avatars
///    into the player's arsenal
///  - lets the player summon redeemed Avatars (F1..F4 or UI buttons)
/// Created by CampaignMenuController.LaunchLevel; persists across the scene
/// load and destroys itself when the level ends and you leave.
/// </summary>
public class CampaignManager : MonoBehaviour
{
    public static CampaignManager Instance { get; private set; }

    public CampaignLevel CurrentLevel { get; private set; }

    private string targetSceneName;
    private CampaignProgress progress;
    private bool levelFinished;
    private bool setupDone;
    private float surviveRemaining;
    private bool surviving;
    private readonly HashSet<Nation> summonedThisLevel = new HashSet<Nation>();

    private static readonly KeyCode[] SummonKeys =
        { KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4 };

    // ------------------------------------------------------------------
    // Launching
    // ------------------------------------------------------------------

    public static void LaunchLevel(CampaignLevel level, Nation playerNation, string defaultSceneName)
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("CampaignManager");
            Instance = go.AddComponent<CampaignManager>();
            DontDestroyOnLoad(go);
        }
        Instance.StartLevel(level, playerNation, defaultSceneName);
    }

    private void StartLevel(CampaignLevel level, Nation playerNation, string defaultSceneName)
    {
        CurrentLevel = level;
        levelFinished = false;
        setupDone = false;
        surviving = false;
        escortActive = false;
        escortUnit = null;
        summonedThisLevel.Clear();
        progress = CampaignProgress.Load();

        // Configure the match: campaign mode, this level's enemies, its seed.
        GameSetup.PlayerNation = playerNation;
        GameSetup.Mode = GameMode.Campaign;
        GameSetup.MapSeed = level.mapSeed;
        GameSetup.AIOpponents.Clear();
        for (int i = 0; i < level.enemyNations.Count; i++)
        {
            GameSetup.AIOpponents.Add(new GameSetup.AIOpponent(level.enemyNations[i], i + 1));
        }

        targetSceneName = string.IsNullOrEmpty(level.sceneName) ? defaultSceneName : level.sceneName;
        Debug.Log($"[Campaign] Launching {level.id} '{level.title}' on {targetSceneName} (seed {level.mapSeed}).");
        SceneLoader.Load(targetSceneName); // with a lore quote while it loads
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameManager.VictoryEvent += OnVictory;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameManager.VictoryEvent -= OnVictory;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (CurrentLevel == null) return;

        if (scene.name != targetSceneName)
        {
            // Player went back to a menu: this run is over.
            Destroy(gameObject);
            return;
        }

        StartCoroutine(SetupLevel());
    }

    private IEnumerator SetupLevel()
    {
        yield return null; // let scene Awake/Start (MatchManager, bases) run

        ApplyPermanentUpgrades();
        SetupScratchStart();

        // A new chapter opens with a full-screen interlude (first visit only),
        // then the level's dialogue, then the fighting starts.
        bool showInterlude = !string.IsNullOrEmpty(CurrentLevel.interludeTitle) &&
                             !progress.IsLevelCompleted(CurrentLevel.id);
        if (showInterlude)
        {
            InterludeUI.Show(CurrentLevel.interludeTitle, CurrentLevel.interludeText,
                () => DialogueUI.Play(CurrentLevel.dialogue, OnDialogueFinished));
        }
        else
        {
            DialogueUI.Play(CurrentLevel.dialogue, OnDialogueFinished);
        }
    }

    private void OnDialogueFinished()
    {
        setupDone = true;

        if (CurrentLevel.boss != CampaignBoss.None)
        {
            SpawnBoss(CurrentLevel.boss);
        }

        if (CurrentLevel.objective == CampaignObjective.Survive)
        {
            surviveRemaining = CurrentLevel.surviveSeconds;
            surviving = true;
        }

        if (CurrentLevel.objective == CampaignObjective.Escort)
        {
            SetupEscortMission();
        }

        if (CurrentLevel.isTutorial && GetComponent<TutorialManager>() == null)
        {
            gameObject.AddComponent<TutorialManager>();
        }

        StartCoroutine(WaveLoop());
        StartCoroutine(DefeatWatchdog());
    }

    // ------------------------------------------------------------------
    // Escort objective: deliver the caravan alive to the golden beacon
    // ------------------------------------------------------------------

    private GameObject escortUnit;
    private Vector3 escortDestination;
    private bool escortActive;
    private const float EscortArrivalRadius = 10f;

    private void SetupEscortMission()
    {
        Vector3 startPos = FindPlayerStartPosition();

        // The caravan: the nation's animal (a laden sky bison / ostrich horse
        // convoy), falling back to a worker cart.
        GameObject prefab = FindRosterPrefab(GameSetup.PlayerNation, UnitCategory.Animal);
        if (prefab == null) prefab = FindWorkerPrefab(GameSetup.PlayerNation);
        if (prefab == null)
        {
            Debug.LogError("[Campaign] Escort mission needs an animal or worker prefab in the roster.");
            return;
        }

        escortUnit = Instantiate(prefab, startPos + new Vector3(0f, 0f, -4f), Quaternion.identity);
        escortUnit.name = "Relief Caravan";
        FactionUtility.SetFaction(escortUnit, FactionManager.LocalPlayerFactionId);
        escortActive = true;

        // Destination: the far side of the map, marked by a golden beacon.
        escortDestination = FindBossSpawnPosition();
        CreateBeacon(escortDestination);

        Debug.Log("[Campaign] ESCORT: bring the Relief Caravan to the golden beacon alive!");
    }

    private static void CreateBeacon(Vector3 position)
    {
        GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beacon.name = "EscortBeacon";
        Object.Destroy(beacon.GetComponent<Collider>());
        beacon.transform.position = position + Vector3.up * 0.1f;
        beacon.transform.localScale = new Vector3(EscortArrivalRadius, 0.15f, EscortArrivalRadius);

        Renderer renderer = beacon.GetComponent<Renderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        Color gold = new Color(1f, 0.85f, 0.2f);
        block.SetColor("_BaseColor", gold);
        block.SetColor("_Color", gold);
        renderer.SetPropertyBlock(block);

        MinimapPOI poi = MinimapPOI.Ensure(beacon, MinimapPOI.POIType.Custom);
        poi.colorOverride = gold;
        poi.discovered = true; // the destination is always on the map
    }

    private static GameObject FindRosterPrefab(Nation nation, UnitCategory category)
    {
        NationDatabase db = NationDatabase.Load();
        NationData data = db != null ? db.Get(nation) : null;
        if (data == null || data.units == null) return null;

        foreach (NationData.UnitEntry entry in data.units)
        {
            if (entry != null && entry.category == category && entry.prefab != null) return entry.prefab;
        }
        return null;
    }

    // ------------------------------------------------------------------
    // Scratch start: one Avatar + one builder + seed silver, no free base
    // ------------------------------------------------------------------

    private void SetupScratchStart()
    {
        // Seed silver: enough to found the base and get the economy going.
        if (PlayerResources.Instance != null)
        {
            PlayerResources.Instance.SetCredits(CurrentLevel.startingSilver);
        }

        Vector3 startPos = FindPlayerStartPosition();

        GameObject avatarPrefab = FindAvatarPrefab(GameSetup.PlayerNation);
        if (avatarPrefab != null)
        {
            GameObject avatar = Instantiate(avatarPrefab, startPos + new Vector3(2f, 0f, 0f), Quaternion.identity);
            FactionUtility.SetFaction(avatar, FactionManager.LocalPlayerFactionId);
        }
        else
        {
            Debug.LogError($"[Campaign] No Avatar prefab for {GameSetup.PlayerNation} - " +
                           "add one (category Avatar) to the nation's roster.");
        }

        GameObject workerPrefab = FindWorkerPrefab(GameSetup.PlayerNation);
        if (workerPrefab != null)
        {
            GameObject worker = Instantiate(workerPrefab, startPos + new Vector3(-2f, 0f, 0f), Quaternion.identity);
            FactionUtility.SetFaction(worker, FactionManager.LocalPlayerFactionId);
        }

        Debug.Log($"[Campaign] Scratch start: Avatar + builder + {CurrentLevel.startingSilver} silver. " +
                  "Press B (or the Found Base button) to place your command center!");
    }

    private Vector3 FindPlayerStartPosition()
    {
        // Hand-built scene with a player base already placed? Start beside it.
        foreach (CommandCenter cc in FindObjectsByType<CommandCenter>(FindObjectsSortMode.None))
        {
            if (FactionUtility.IsLocallyControlled(cc.gameObject))
            {
                return cc.transform.position + new Vector3(8f, 0f, 0f);
            }
        }

        // Otherwise the lowest-index StartLocation is the player's ground.
        StartLocation best = null;
        foreach (StartLocation location in FindObjectsByType<StartLocation>(FindObjectsSortMode.None))
        {
            if (best == null || location.index < best.index) best = location;
        }
        if (best != null) return best.transform.position;

        Debug.LogWarning("[Campaign] No StartLocation in this scene - starting at origin.");
        return Vector3.zero;
    }

    private static GameObject FindWorkerPrefab(Nation nation)
    {
        NationDatabase db = NationDatabase.Load();
        NationData data = db != null ? db.Get(nation) : null;
        if (data == null || data.units == null) return null;

        foreach (NationData.UnitEntry entry in data.units)
        {
            if (entry != null && entry.category == UnitCategory.Worker && entry.prefab != null)
            {
                return entry.prefab;
            }
        }
        // No dedicated worker in the roster yet: fall back to the basic unit.
        NationData.UnitEntry basic = data.GetUnit(0);
        return basic != null ? basic.prefab : null;
    }

    // ------------------------------------------------------------------
    // Enemy waves of slightly increasing difficulty
    // ------------------------------------------------------------------

    private IEnumerator WaveLoop()
    {
        int waveNumber = 0;
        yield return new WaitForSeconds(CurrentLevel.firstWaveDelay);

        while (!levelFinished && (GameManager.Instance == null || !GameManager.Instance.GameIsOver))
        {
            waveNumber++;
            int rawSize = CurrentLevel.waveBaseSize + (waveNumber - 1) * CurrentLevel.waveGrowth;
            int waveSize = Mathf.Min(30, Mathf.Max(1,
                Mathf.RoundToInt(rawSize * GameSetup.EnemyStrengthMultiplier)));
            SpawnWave(waveNumber, waveSize);

            yield return new WaitForSeconds(CurrentLevel.waveInterval);
        }
    }

    private void SpawnWave(int waveNumber, int waveSize)
    {
        // One wave source per surviving enemy base; boss arenas send spirits.
        List<CommandCenter> sources = new List<CommandCenter>();
        foreach (CommandCenter cc in FindObjectsByType<CommandCenter>(FindObjectsSortMode.None))
        {
            if (!FactionUtility.IsLocallyControlled(cc.gameObject)) sources.Add(cc);
        }

        if (sources.Count == 0)
        {
            SpawnSpiritWave(waveNumber, waveSize);
            return;
        }

        Debug.Log($"[Campaign] Wave {waveNumber} incoming: {waveSize} units from {sources.Count} base(s)!");
        foreach (CommandCenter source in sources)
        {
            int factionId = FactionUtility.GetFactionId(source.gameObject);
            Faction faction = FactionManager.Get(factionId);
            NationDatabase db = NationDatabase.Load();
            NationData data = db != null && faction != null ? db.Get(faction.nation) : null;
            if (data == null || data.units == null || data.units.Length == 0) continue;

            for (int i = 0; i < waveSize; i++)
            {
                NationData.UnitEntry entry = PickCombatEntry(data);
                if (entry == null) break;

                Vector2 circle = Random.insideUnitCircle.normalized * 8f;
                Vector3 pos = source.transform.position + new Vector3(circle.x, 0f, circle.y);

                GameObject unit = MatchManager.Instance != null
                    ? MatchManager.Instance.SpawnUnitFor(factionId, entry.prefab, pos)
                    : Instantiate(entry.prefab, pos, Quaternion.identity);
                if (MatchManager.Instance == null) FactionUtility.SetFaction(unit, factionId);

                EnemyAI brain = unit.GetComponent<EnemyAI>();
                if (brain == null) brain = unit.AddComponent<EnemyAI>();
                brain.chaseCommandCenters = true; // waves march on the player
            }
        }
    }

    private void SpawnSpiritWave(int waveNumber, int waveSize)
    {
        GameObject spiritPrefab = Resources.Load<GameObject>("Campaign/DarkSpirit");
        if (spiritPrefab == null)
        {
            if (waveNumber == 1)
            {
                Debug.LogWarning("[Campaign] No enemy base and no Resources/Campaign/DarkSpirit prefab - " +
                                 "this boss arena relies on scene SpiritPortals for pressure.");
            }
            return;
        }

        Vector3 origin = FindBossSpawnPosition();
        Debug.Log($"[Campaign] Wave {waveNumber}: {waveSize} dark spirits pour from the rupture!");
        for (int i = 0; i < waveSize; i++)
        {
            Vector2 circle = Random.insideUnitCircle.normalized * 10f;
            GameObject spirit = Instantiate(spiritPrefab,
                origin + new Vector3(circle.x, 0f, circle.y), Quaternion.identity);
            FactionUtility.SetFaction(spirit, FactionManager.HostileSpiritsFaction);
        }
    }

    private static NationData.UnitEntry PickCombatEntry(NationData data)
    {
        for (int attempts = 0; attempts < 6; attempts++)
        {
            NationData.UnitEntry candidate = data.units[Random.Range(0, data.units.Length)];
            if (candidate != null && candidate.prefab != null &&
                candidate.category != UnitCategory.Worker &&
                candidate.category != UnitCategory.Avatar)
            {
                return candidate;
            }
        }
        return null;
    }

    // ------------------------------------------------------------------
    // Defeat watchdog: with no base AND no units left, the level is lost
    // ------------------------------------------------------------------

    private IEnumerator DefeatWatchdog()
    {
        WaitForSeconds wait = new WaitForSeconds(3f);
        while (!levelFinished && (GameManager.Instance == null || !GameManager.Instance.GameIsOver))
        {
            yield return wait;
            if (!setupDone) continue;

            bool hasBase = false;
            foreach (CommandCenter cc in FindObjectsByType<CommandCenter>(FindObjectsSortMode.None))
            {
                if (FactionUtility.IsLocallyControlled(cc.gameObject)) { hasBase = true; break; }
            }
            if (hasBase) continue;

            bool hasUnits = false;
            if (UnitSelectionManager.Instance != null)
            {
                foreach (GameObject go in UnitSelectionManager.Instance.allUnitsList)
                {
                    if (go != null && FactionUtility.IsLocallyControlled(go)) { hasUnits = true; break; }
                }
            }
            if (hasUnits) continue;

            Debug.Log("[Campaign] No base, no forces - the mission is lost.");
            if (GameManager.Instance != null) GameManager.Instance.ShowDefeat();
            yield break;
        }
    }

    private void Update()
    {
        if (levelFinished || CurrentLevel == null) return;

        // Survive objective countdown.
        if (surviving && GameManager.Instance != null && !GameManager.Instance.GameIsOver)
        {
            surviveRemaining -= Time.deltaTime;
            if (surviveRemaining <= 0f)
            {
                surviving = false;
                Debug.Log("[Campaign] Survived! The assault breaks.");
                if (GameManager.Instance != null) GameManager.Instance.ShowVictory();
            }
        }

        // Escort objective: the caravan must live and arrive.
        if (escortActive && setupDone &&
            GameManager.Instance != null && !GameManager.Instance.GameIsOver)
        {
            if (escortUnit == null)
            {
                Debug.Log("[Campaign] The caravan was destroyed - the mission is lost.");
                GameManager.Instance.ShowDefeat();
            }
            else if (Vector3.Distance(escortUnit.transform.position, escortDestination) <= EscortArrivalRadius)
            {
                Debug.Log("[Campaign] The caravan arrives safely - the road is open!");
                GameManager.Instance.ShowVictory();
            }
        }

        // Summon redeemed Avatars: F1=Air F2=Water F3=Earth F4=Fire.
        for (int i = 0; i < SummonKeys.Length; i++)
        {
            if (Input.GetKeyDown(SummonKeys[i])) SummonRedeemedAvatar(i);
        }

        // B: found (or expand) your base - place a command center.
        if (Input.GetKeyDown(KeyCode.B) && BuildingPlacer.Instance != null)
        {
            BuildingPlacer.Instance.BeginCommandCenterPlacement();
        }
    }

    // ------------------------------------------------------------------
    // Permanent progression
    // ------------------------------------------------------------------

    private void ApplyPermanentUpgrades()
    {
        NationDatabase db = NationDatabase.Load();
        NationData data = db != null ? db.Get(GameSetup.PlayerNation) : null;
        if (data == null || data.upgrades == null) return;

        foreach (UpgradeData upgrade in data.upgrades)
        {
            if (upgrade == null) continue;
            int level = progress.GetPermanentUpgradeLevel(upgrade.upgradeId);
            if (level > 0)
            {
                UpgradeManager.SetLevelDirect(FactionManager.LocalPlayerFactionId, upgrade, level);
            }
        }
    }

    // ------------------------------------------------------------------
    // Bosses
    // ------------------------------------------------------------------

    private void SpawnBoss(CampaignBoss boss)
    {
        Vector3 position = FindBossSpawnPosition();

        if (boss == CampaignBoss.DarkSpirit)
        {
            GameObject prefab = Resources.Load<GameObject>("Campaign/FinalBoss");
            if (prefab == null) prefab = FindAvatarPrefab(Nation.Fire); // mechanical fallback
            if (prefab == null)
            {
                Debug.LogError("[Campaign] No final boss prefab. Place one at " +
                               "Assets/Resources/Campaign/FinalBoss.prefab (see docs/CAMPAIGN.md).");
                return;
            }

            GameObject bossGo = Instantiate(prefab, position, Quaternion.identity);
            if (bossGo.GetComponent<DarkSpiritBoss>() == null)
            {
                bossGo.AddComponent<DarkSpiritBoss>();
            }
            Debug.Log("[Campaign] Umbriss, the First Shadow, manifests!");
            return;
        }

        Nation nation = boss == CampaignBoss.CorruptedAvatarAir ? Nation.Air
            : boss == CampaignBoss.CorruptedAvatarWater ? Nation.Water
            : boss == CampaignBoss.CorruptedAvatarEarth ? Nation.Earth
            : Nation.Fire;

        GameObject avatarPrefab = FindAvatarPrefab(nation);
        if (avatarPrefab == null)
        {
            Debug.LogError($"[Campaign] No Avatar prefab in {nation}'s NationData roster " +
                           "(category Avatar) - cannot spawn the chapter boss.");
            return;
        }

        GameObject corrupted = Instantiate(avatarPrefab, position, Quaternion.identity);
        CorruptedAvatar bossComponent = corrupted.AddComponent<CorruptedAvatar>();
        bossComponent.Configure(nation, BossNameFor(nation));
        Debug.Log($"[Campaign] {bossComponent.bossName} has arrived!");
    }

    public static string BossNameFor(Nation nation)
    {
        switch (nation)
        {
            case Nation.Air: return "Zephyra of the Hollow Sky";
            case Nation.Water: return "Kalani of the Weeping Ice";
            case Nation.Earth: return "Boruk, the Mountain That Walks";
            default: return "Ashan, the Dawnbringer";
        }
    }

    private static GameObject FindAvatarPrefab(Nation nation)
    {
        NationDatabase db = NationDatabase.Load();
        NationData data = db != null ? db.Get(nation) : null;
        if (data == null || data.units == null) return null;

        foreach (NationData.UnitEntry entry in data.units)
        {
            if (entry != null && entry.category == UnitCategory.Avatar && entry.prefab != null)
            {
                return entry.prefab;
            }
        }
        return null;
    }

    private Vector3 FindBossSpawnPosition()
    {
        // Furthest StartLocation from the player's base, else opposite the base.
        Vector3 playerBase = Vector3.zero;
        foreach (CommandCenter cc in FindObjectsByType<CommandCenter>(FindObjectsSortMode.None))
        {
            if (FactionUtility.IsLocallyControlled(cc.gameObject))
            {
                playerBase = cc.transform.position;
                break;
            }
        }

        Vector3 best = playerBase + new Vector3(60f, 0f, 60f);
        float bestDistance = 0f;
        foreach (StartLocation location in FindObjectsByType<StartLocation>(FindObjectsSortMode.None))
        {
            float distance = Vector3.Distance(location.transform.position, playerBase);
            if (distance > bestDistance)
            {
                bestDistance = distance;
                best = location.transform.position;
            }
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(best, out hit, 25f, NavMesh.AllAreas)) return hit.position;
        return best;
    }

    // ------------------------------------------------------------------
    // Boss outcomes
    // ------------------------------------------------------------------

    public void OnCorruptedAvatarDefeated(CorruptedAvatar boss)
    {
        if (levelFinished) return;

        progress.RedeemAvatar(boss.avatarNation);
        Debug.Log($"[Campaign] {boss.bossName} is FREED from the shadow - " +
                  $"the {NationInfo.DisplayName(boss.avatarNation)} Avatar joins your cause! " +
                  $"(Summon with F{(int)boss.avatarNation + 1} in future battles.)");

        if (CurrentLevel.objective == CampaignObjective.DefeatBoss && GameManager.Instance != null)
        {
            // Savor the moment: slow motion, THEN the victory screen.
            GameFeel.Shake(0.5f, 1.2f);
            GameFeel.SlowMoThen(0.3f, 1.5f, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.ShowVictory();
            });
        }
    }

    public void OnDarkSpiritDefeated()
    {
        if (levelFinished) return;

        Debug.Log("[Campaign] The First Shadow unravels. The rupture seals. BALANCE IS RESTORED.");
        GameFeel.Shake(0.9f, 2f);
        GameFeel.SlowMoThen(0.25f, 2.5f, () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.ShowVictory();
        });
    }

    private void OnVictory()
    {
        if (levelFinished || CurrentLevel == null) return;
        levelFinished = true;

        progress.CompleteLevel(CurrentLevel.id, CurrentLevel.chiReward);
        Debug.Log($"[Campaign] '{CurrentLevel.title}' complete! +{CurrentLevel.chiReward} chi " +
                  $"(total {progress.chi}). Return to the campaign menu for the next mission.");
    }

    // ------------------------------------------------------------------
    // Redeemed Avatar summoning (F1..F4 or UI buttons)
    // ------------------------------------------------------------------

    /// <summary>0=Air 1=Water 2=Earth 3=Fire. Each redeemed Avatar once per level.</summary>
    public void SummonRedeemedAvatar(int nationIndex)
    {
        if (levelFinished) return;

        Nation nation = (Nation)Mathf.Clamp(nationIndex, 0, 3);
        if (!progress.IsAvatarRedeemed(nation))
        {
            Debug.Log($"[Campaign] The {NationInfo.DisplayName(nation)} Avatar has not been redeemed yet.");
            return;
        }
        if (summonedThisLevel.Contains(nation))
        {
            Debug.Log($"[Campaign] The {NationInfo.DisplayName(nation)} Avatar already fights beside you.");
            return;
        }

        GameObject prefab = FindAvatarPrefab(nation);
        if (prefab == null)
        {
            Debug.LogError($"[Campaign] No Avatar prefab for {nation} in the NationDatabase.");
            return;
        }

        Vector3 spawn = Vector3.zero;
        foreach (CommandCenter cc in FindObjectsByType<CommandCenter>(FindObjectsSortMode.None))
        {
            if (FactionUtility.IsLocallyControlled(cc.gameObject))
            {
                spawn = cc.transform.position + new Vector3(6f, 0f, 6f);
                break;
            }
        }

        GameObject avatar = Instantiate(prefab, spawn, Quaternion.identity);
        FactionUtility.SetFaction(avatar, FactionManager.LocalPlayerFactionId);
        summonedThisLevel.Add(nation);
        Debug.Log($"[Campaign] {BossNameFor(nation)} answers the call!");
    }
}
