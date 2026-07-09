using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestrates a match with any number of factions: registers factions from
/// GameSetup, optionally spawns bases at StartLocations (skirmish), tracks
/// command centers, and decides victory/defeat (last team standing).
///
/// Add one MatchManager GameObject to each level scene. Scenes without it
/// (the original Level1) keep the legacy two-team behaviour.
/// </summary>
public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }

    [Header("Data")]
    public NationDatabase nationDatabase;

    [Header("Skirmish Setup")]
    [Tooltip("Spawn command centers + starting units at StartLocation markers. " +
             "Leave off for hand-built scenes like Level1.")]
    public bool spawnBasesAtStartLocations = false;
    public int startingUnitsPerFaction = 3;
    public float baseIncomeInterval = 10f;
    public int baseIncomeAmount = 25;

    private readonly Dictionary<int, List<CommandCenter>> commandCentersByFaction =
        new Dictionary<int, List<CommandCenter>>();

    private bool matchEnded;

    public GameMode Mode => GameSetup.Mode;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (nationDatabase == null) nationDatabase = NationDatabase.Load();

        // In multiplayer, factions are registered by the network layer instead.
        if (!NetworkGuard.IsNetworked)
        {
            BuildFactionsFromGameSetup();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BuildFactionsFromGameSetup()
    {
        FactionManager.Reset();

        FactionManager.Register(new Faction
        {
            id = 0,
            nation = GameSetup.PlayerNation,
            displayName = NationInfo.DisplayName(GameSetup.PlayerNation),
            teamGroup = 0,
            isAI = false,
            isLocalPlayer = true
        });

        // Survival with no explicit opponents still needs the Fire AI seat.
        if (GameSetup.AIOpponents.Count == 0)
        {
            GameSetup.AIOpponents.Add(new GameSetup.AIOpponent(Nation.Fire, 1));
        }

        int nextId = 1;
        foreach (GameSetup.AIOpponent opponent in GameSetup.AIOpponents)
        {
            FactionManager.Register(new Faction
            {
                id = nextId++,
                nation = opponent.nation,
                displayName = NationInfo.DisplayName(opponent.nation),
                teamGroup = opponent.teamGroup,
                isAI = true
            });
        }
    }

    private void Start()
    {
        if (spawnBasesAtStartLocations && !NetworkGuard.IsNetworked)
        {
            SpawnBases();
        }

        if (Mode == GameMode.Skirmish)
        {
            StartCoroutine(IncomeLoop());
        }
    }

    // ------------------------------------------------------------------
    // Base spawning (skirmish)
    // ------------------------------------------------------------------

    private void SpawnBases()
    {
        List<StartLocation> locations = new List<StartLocation>(
            FindObjectsByType<StartLocation>(FindObjectsSortMode.None));
        locations.Sort((a, b) => a.index.CompareTo(b.index));

        if (locations.Count == 0)
        {
            Debug.LogError("MatchManager: spawnBasesAtStartLocations is on but the scene " +
                           "has no StartLocation markers.");
            return;
        }

        int slot = 0;
        foreach (Faction faction in FactionManager.All)
        {
            if (faction.id == FactionManager.HostileSpiritsFaction) continue;
            if (slot >= locations.Count)
            {
                Debug.LogWarning($"MatchManager: not enough StartLocations for faction {faction.displayName}.");
                break;
            }
            SpawnBaseFor(faction, locations[slot].transform);
            slot++;
        }
    }

    public GameObject SpawnBaseFor(Faction faction, Transform location)
    {
        NationData data = nationDatabase != null ? nationDatabase.Get(faction.nation) : null;
        if (data == null || data.commandCenterPrefab == null)
        {
            Debug.LogError($"MatchManager: no command center prefab configured for {faction.displayName}. " +
                           "Fill in the NationData assets.");
            return null;
        }

        GameObject baseGo = Instantiate(data.commandCenterPrefab, location.position, location.rotation);
        FactionUtility.SetFaction(baseGo, faction.id);

        CommandCenter cc = baseGo.GetComponentInChildren<CommandCenter>();
        if (cc != null)
        {
            cc.team = faction.isLocalPlayer ? Team.Player : Team.Enemy;
        }

        // Starting units in a loose ring around the base.
        NationData.UnitEntry starterEntry = data.GetUnit(0);
        if (starterEntry != null && starterEntry.prefab != null)
        {
            for (int i = 0; i < startingUnitsPerFaction; i++)
            {
                float angle = (360f / Mathf.Max(1, startingUnitsPerFaction)) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * 6f;
                SpawnUnitFor(faction.id, starterEntry.prefab, location.position + offset);
            }
        }

        if (faction.isAI)
        {
            AICommander commander = baseGo.AddComponent<AICommander>();
            commander.Configure(faction.id, data, this);
        }

        return baseGo;
    }

    /// <summary>Spawns a unit and stamps it with faction ownership + AI brain.</summary>
    public GameObject SpawnUnitFor(int factionId, GameObject prefab, Vector3 position)
    {
        GameObject go = Instantiate(prefab, position, Quaternion.identity);
        FactionUtility.SetFaction(go, factionId);

        if (FactionManager.IsAIControlled(factionId))
        {
            // AI units need the autonomous brain even on player-nation prefabs.
            if (go.GetComponent<EnemyAI>() == null) go.AddComponent<EnemyAI>();
        }
        return go;
    }

    // ------------------------------------------------------------------
    // Economy
    // ------------------------------------------------------------------

    private IEnumerator IncomeLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(baseIncomeInterval);
        while (!matchEnded)
        {
            yield return wait;
            foreach (Faction faction in FactionManager.All)
            {
                if (faction.isDefeated || faction.id == FactionManager.HostileSpiritsFaction) continue;
                AwardCredits(faction.id, baseIncomeAmount);
            }
        }
    }

    public void AwardCredits(int factionId, int amount)
    {
        Faction faction = FactionManager.Get(factionId);
        if (faction == null) return;

        if (faction.isLocalPlayer && PlayerResources.Instance != null)
        {
            PlayerResources.Instance.AddCredits(amount);
        }
        else
        {
            faction.credits += amount;
        }
    }

    public bool TrySpendCredits(int factionId, int amount)
    {
        Faction faction = FactionManager.Get(factionId);
        if (faction == null) return false;

        if (faction.isLocalPlayer && PlayerResources.Instance != null)
        {
            return PlayerResources.Instance.SpendCredits(amount);
        }

        if (faction.credits < amount) return false;
        faction.credits -= amount;
        return true;
    }

    // ------------------------------------------------------------------
    // Command centers & victory
    // ------------------------------------------------------------------

    public void RegisterCommandCenter(CommandCenter cc)
    {
        int factionId = FactionUtility.GetFactionId(cc.gameObject);
        if (!commandCentersByFaction.TryGetValue(factionId, out List<CommandCenter> list))
        {
            list = new List<CommandCenter>();
            commandCentersByFaction[factionId] = list;
        }
        if (!list.Contains(cc)) list.Add(cc);
    }

    public void OnCommandCenterDestroyed(CommandCenter cc)
    {
        if (matchEnded) return;

        int factionId = FactionUtility.GetFactionId(cc.gameObject);
        if (commandCentersByFaction.TryGetValue(factionId, out List<CommandCenter> list))
        {
            list.Remove(cc);
            if (list.Count > 0) return; // faction still has a base standing
        }

        Faction faction = FactionManager.Get(factionId);
        if (faction == null) return;

        faction.isDefeated = true;
        Debug.Log($"MatchManager: {faction.displayName} has been defeated.");

        CheckMatchEnd();
    }

    private void CheckMatchEnd()
    {
        Faction local = FactionManager.Get(FactionManager.LocalPlayerFactionId);
        if (local == null || GameManager.Instance == null) return;

        if (FactionManager.IsTeamDefeated(local.teamGroup))
        {
            matchEnded = true;
            GameManager.Instance.ShowDefeat();
        }
        else if (FactionManager.AllEnemyTeamsDefeated())
        {
            matchEnded = true;
            GameManager.Instance.ShowVictory();
        }
    }
}
