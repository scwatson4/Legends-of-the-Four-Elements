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
        surviving = false;
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
        SceneManager.LoadScene(targetSceneName);
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

        // Story first, then the fighting starts.
        DialogueUI.Play(CurrentLevel.dialogue, OnDialogueFinished);
    }

    private void OnDialogueFinished()
    {
        if (CurrentLevel.boss != CampaignBoss.None)
        {
            SpawnBoss(CurrentLevel.boss);
        }

        if (CurrentLevel.objective == CampaignObjective.Survive)
        {
            surviveRemaining = CurrentLevel.surviveSeconds;
            surviving = true;
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

        // Summon redeemed Avatars: F1=Air F2=Water F3=Earth F4=Fire.
        for (int i = 0; i < SummonKeys.Length; i++)
        {
            if (Input.GetKeyDown(SummonKeys[i])) SummonRedeemedAvatar(i);
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
            GameManager.Instance.ShowVictory();
        }
    }

    public void OnDarkSpiritDefeated()
    {
        if (levelFinished) return;

        Debug.Log("[Campaign] The First Shadow unravels. The rupture seals. BALANCE IS RESTORED.");
        if (GameManager.Instance != null) GameManager.Instance.ShowVictory();
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
