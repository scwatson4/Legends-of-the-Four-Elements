using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A neutral tribe settlement. Spawns wandering villagers and works as a
/// control point: when exactly one team has combat units inside the control
/// radius, that team earns tribute credits over time - a reason to fight for
/// the map, Army Men RTS style.
/// </summary>
public class Village : MonoBehaviour
{
    [Header("Villagers")]
    public GameObject villagerPrefab;
    public int villagerCount = 4;
    public float villagerSpawnRadius = 6f;

    [Header("Control Point")]
    public float controlRadius = 15f;
    public float tributeInterval = 8f;
    public int tributeAmount = 15;

    private float tributeTimer;

    private void Start()
    {
        tributeTimer = tributeInterval;
        MinimapPOI.Ensure(gameObject, MinimapPOI.POIType.Village);

        if (villagerPrefab == null) return;
        for (int i = 0; i < villagerCount; i++)
        {
            Vector2 circle = Random.insideUnitCircle * villagerSpawnRadius;
            Vector3 pos = transform.position + new Vector3(circle.x, 0f, circle.y);
            GameObject villagerGo = Instantiate(villagerPrefab, pos, Quaternion.identity);

            Villager villager = villagerGo.GetComponent<Villager>();
            if (villager != null) villager.homePosition = transform.position;
        }
    }

    private void Update()
    {
        tributeTimer -= Time.deltaTime;
        if (tributeTimer > 0f) return;
        tributeTimer = tributeInterval;

        int controllingFaction = GetControllingFaction();
        if (controllingFaction == FactionManager.NoFaction) return;

        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.AwardCredits(controllingFaction, tributeAmount);
        }
        else if (controllingFaction == FactionManager.LocalPlayerFactionId &&
                 PlayerResources.Instance != null)
        {
            PlayerResources.Instance.AddCredits(tributeAmount);
        }
    }

    /// <summary>Who holds this village right now (used by trade routes too).</summary>
    public int ControllingFactionId() => GetControllingFaction();

    /// <summary>The single team holding the village, or NoFaction if empty/contested.</summary>
    private int GetControllingFaction()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, controlRadius);
        HashSet<int> teamsPresent = new HashSet<int>();
        int candidateFaction = FactionManager.NoFaction;

        foreach (Collider hit in hits)
        {
            Unit unit = hit.GetComponentInParent<Unit>();
            if (unit == null) continue;

            int factionId = FactionUtility.GetFactionId(unit.gameObject);
            if (factionId == FactionManager.NoFaction ||
                factionId == FactionManager.HostileSpiritsFaction) continue;

            Faction faction = FactionManager.Get(factionId);
            if (faction == null) continue;

            teamsPresent.Add(faction.teamGroup);
            candidateFaction = factionId;
        }

        return teamsPresent.Count == 1 ? candidateFaction : FactionManager.NoFaction;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, controlRadius);
    }
}
