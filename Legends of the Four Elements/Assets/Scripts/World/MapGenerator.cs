using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

/// <summary>
/// Seeded random map population: scatters elemental biome zones (volcanoes,
/// glaciers, rivers, windy peaks, quarries, spirit wilds), matching resource
/// nodes, villages, spirits and StartLocations across the play area. Same
/// seed = same map, so results are reproducible (and identical across
/// multiplayer clients when they share a seed).
///
/// Put one on an empty GameObject in a "Random Map" scene, assign the prefab
/// lists, and it runs in Awake - before MatchManager spawns the bases.
/// Full terrain *sculpting* (raising volcanoes etc.) stays an art task; this
/// component populates gameplay features on whatever terrain you give it.
/// </summary>
public class MapGenerator : MonoBehaviour
{
    [Header("Area (centered on this object)")]
    public Vector2 mapSize = new Vector2(200f, 200f);
    public LayerMask groundMask;
    public float edgeMargin = 25f;

    [Header("Seed")]
    [Tooltip("0 = use the seed chosen in the menu (GameSetup.MapSeed).")]
    public int seedOverride = 0;
    [Tooltip("Fixed seed used in multiplayer so every client builds the same map.")]
    public int multiplayerSeed = 12345;

    [Header("Biomes")]
    [Tooltip("Prefabs with a BiomeZone component + themed props (lava rocks, ice, reeds...).")]
    public GameObject[] biomeZonePrefabs;
    public int biomeCount = 5;
    public float minBiomeSpacing = 40f;

    [Header("Resources")]
    [Tooltip("Node prefabs (ResourceNode). Matching types spawn inside matching biomes.")]
    public GameObject[] resourceNodePrefabs;
    public int nodesPerBiome = 2;
    public int extraScatteredNodes = 4;

    [Header("Neutrals")]
    public GameObject villagePrefab;
    public int villageCount = 3;
    public GameObject friendlySpiritPrefab;
    public GameObject darkSpiritPrefab;
    public int spiritCount = 4;
    public GameObject spiritPortalPrefab;
    public int spiritPortalCount = 2;
    [Tooltip("Optional: ONE sleeping colossal spirit near the map center - the prize both sides race their Avatars toward.")]
    public GameObject colossalSpiritPrefab;

    [Header("Start Locations")]
    [Tooltip("Created evenly around the map edge unless the scene already has StartLocations.")]
    public int startLocationCount = 4;

    [Header("NavMesh")]
    [Tooltip("Optional: rebuilt after placement so units can path around new props.")]
    public NavMeshSurface navMeshSurface;

    private System.Random rng;

    private void Awake()
    {
        int seed = NetworkGuard.IsNetworked ? multiplayerSeed
            : seedOverride != 0 ? seedOverride
            : GameSetup.MapSeed;
        if (seed == 0) seed = 1;

        rng = new System.Random(seed);
        Debug.Log($"MapGenerator: generating with seed {seed}.");

        GenerateStartLocations();
        List<BiomeZone> biomes = GenerateBiomes();
        GenerateResources(biomes);
        GenerateNeutrals();

        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }
    }

    // ------------------------------------------------------------------

    private void GenerateStartLocations()
    {
        if (FindObjectsByType<StartLocation>(FindObjectsSortMode.None).Length > 0) return;

        float ringX = mapSize.x / 2f - edgeMargin;
        float ringZ = mapSize.y / 2f - edgeMargin;
        float angleOffset = (float)rng.NextDouble() * Mathf.PI * 2f;

        for (int i = 0; i < startLocationCount; i++)
        {
            float angle = angleOffset + (Mathf.PI * 2f / startLocationCount) * i;
            Vector3 pos = transform.position +
                          new Vector3(Mathf.Cos(angle) * ringX, 0f, Mathf.Sin(angle) * ringZ);
            pos = SnapToGround(pos);

            GameObject marker = new GameObject($"StartLocation_{i}");
            marker.transform.position = pos;
            StartLocation location = marker.AddComponent<StartLocation>();
            location.index = i;
        }
    }

    private List<BiomeZone> GenerateBiomes()
    {
        List<BiomeZone> spawned = new List<BiomeZone>();
        if (biomeZonePrefabs == null || biomeZonePrefabs.Length == 0) return spawned;

        List<Vector3> taken = new List<Vector3>();
        int attempts = 0;

        while (spawned.Count < biomeCount && attempts < biomeCount * 20)
        {
            attempts++;
            Vector3 pos = RandomPoint();

            bool tooClose = false;
            foreach (Vector3 existing in taken)
            {
                if (Vector3.Distance(existing, pos) < minBiomeSpacing) { tooClose = true; break; }
            }
            if (tooClose) continue;

            GameObject prefab = biomeZonePrefabs[spawned.Count % biomeZonePrefabs.Length];
            GameObject zoneGo = Instantiate(prefab, pos, RandomYRotation());
            BiomeZone zone = zoneGo.GetComponentInChildren<BiomeZone>();
            if (zone != null) spawned.Add(zone);
            taken.Add(pos);
        }
        return spawned;
    }

    private void GenerateResources(List<BiomeZone> biomes)
    {
        if (resourceNodePrefabs == null || resourceNodePrefabs.Length == 0) return;

        // Themed nodes inside biomes (coal in volcanoes, fish by rivers...).
        foreach (BiomeZone biome in biomes)
        {
            GameObject nodePrefab = PickNodeForClimate(biome.climate);
            for (int i = 0; i < nodesPerBiome; i++)
            {
                Vector3 pos = biome.transform.position + RandomInsideCircle(biome.radius * 0.7f);
                Instantiate(nodePrefab, SnapToGround(pos), RandomYRotation());
            }
        }

        // Plus a few contested nodes in the open.
        for (int i = 0; i < extraScatteredNodes; i++)
        {
            GameObject nodePrefab = resourceNodePrefabs[rng.Next(resourceNodePrefabs.Length)];
            Instantiate(nodePrefab, SnapToGround(RandomPoint()), RandomYRotation());
        }
    }

    private GameObject PickNodeForClimate(BiomeZone.ClimateType climate)
    {
        ResourceNode.NodeType wanted;
        switch (climate)
        {
            case BiomeZone.ClimateType.Volcanic: wanted = ResourceNode.NodeType.CoalSeam; break;
            case BiomeZone.ClimateType.Glacier:
            case BiomeZone.ClimateType.RiverLands: wanted = ResourceNode.NodeType.FishShoal; break;
            case BiomeZone.ClimateType.StoneQuarry: wanted = ResourceNode.NodeType.CrystalDeposit; break;
            default: wanted = ResourceNode.NodeType.SpiritGrove; break;
        }

        foreach (GameObject prefab in resourceNodePrefabs)
        {
            ResourceNode node = prefab != null ? prefab.GetComponent<ResourceNode>() : null;
            if (node != null && node.nodeType == wanted) return prefab;
        }
        return resourceNodePrefabs[rng.Next(resourceNodePrefabs.Length)];
    }

    private void GenerateNeutrals()
    {
        if (villagePrefab != null)
        {
            for (int i = 0; i < villageCount; i++)
            {
                Instantiate(villagePrefab, SnapToGround(RandomPoint()), RandomYRotation());
            }
        }

        if (spiritPortalPrefab != null)
        {
            for (int i = 0; i < spiritPortalCount; i++)
            {
                Instantiate(spiritPortalPrefab, SnapToGround(RandomPoint()), RandomYRotation());
            }
        }

        // The sleeping giant waits near the middle of the world.
        if (colossalSpiritPrefab != null)
        {
            Vector3 nearCenter = transform.position + RandomInsideCircle(mapSize.x * 0.15f);
            Instantiate(colossalSpiritPrefab, SnapToGround(nearCenter), RandomYRotation());
        }

        for (int i = 0; i < spiritCount; i++)
        {
            GameObject prefab = (i % 2 == 0) ? darkSpiritPrefab : friendlySpiritPrefab;
            if (prefab == null) prefab = darkSpiritPrefab != null ? darkSpiritPrefab : friendlySpiritPrefab;
            if (prefab == null) return;
            Instantiate(prefab, SnapToGround(RandomPoint()), RandomYRotation());
        }
    }

    // ------------------------------------------------------------------

    private Vector3 RandomPoint()
    {
        float x = Mathf.Lerp(-mapSize.x / 2f + edgeMargin, mapSize.x / 2f - edgeMargin, (float)rng.NextDouble());
        float z = Mathf.Lerp(-mapSize.y / 2f + edgeMargin, mapSize.y / 2f - edgeMargin, (float)rng.NextDouble());
        return SnapToGround(transform.position + new Vector3(x, 0f, z));
    }

    private Vector3 RandomInsideCircle(float radius)
    {
        float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
        float distance = (float)rng.NextDouble() * radius;
        return new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
    }

    private Quaternion RandomYRotation()
    {
        return Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
    }

    private Vector3 SnapToGround(Vector3 position)
    {
        Vector3 rayStart = position + Vector3.up * 200f;
        RaycastHit hit;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 500f, groundMask))
        {
            return hit.point;
        }
        return position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, new Vector3(mapSize.x, 1f, mapSize.y));
    }
}
