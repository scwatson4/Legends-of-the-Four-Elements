using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class TerrainMapLoader : MonoBehaviour
{
    [Header("Terrain Prefabs")]
    public GameObject VolcanoPrefab;
    public GameObject RiverPrefab;
    public GameObject DesertPrefab;
    public GameObject MountainPrefab;
    public GameObject PlainsPrefab;
    public GameObject ClearedLandPrefab;

    [Header("Map Settings")]
    public string fileName = "rtsmap"; // Without .txt
    public float tileWidth = 1f;
    public float tileHeight = 0.5f;

    public List<Vector3> buildablePlots = new List<Vector3>();

    [Header("Map Generation Options")]
    public bool generateFromSeed = false;
    public int seed = 12345;
    public int generatedWidth = 20;
    public int generatedHeight = 20;

    [Header("Terrain References")]
    public Terrain terrain;
    public TerrainCollider terrainCollider;
    public NavMeshSurface navMeshSurface;

    private string[,] terrainMap;

    void Start()
    {
        if (generateFromSeed)
            GenerateMapFromSeed();
        else
            LoadMap();
    }

    void LoadMap()
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName + ".txt");

        if (!File.Exists(path))
        {
            Debug.LogError("Map file not found: " + path);
            return;
        }

        string[] lines = File.ReadAllLines(path);
        int height = lines.Length;
        int width = lines[0].Split(' ').Length;
        terrainMap = new string[width, height];

        for (int y = 0; y < height; y++)
        {
            string[] cells = lines[y].Split(' ');
            for (int x = 0; x < cells.Length; x++)
            {
                terrainMap[x, y] = cells[x];
            }
        }

        EnsureClearZonesOnOppositeSides();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                string code = terrainMap[x, y];
                if (IsInvalidPlacement(code, x, y))
                {
                    Debug.LogWarning($"Invalid adjacency: {code} at ({x},{y})");
                    continue;
                }

                Vector3 position = GridToIsometric(x, height - y - 1);
                InstantiateTile(code, position);
                UpdateTerrainHeight(x, y, position);
            }
        }

        RebuildNavMesh();
    }

    void GenerateMapFromSeed()
    {
        Random.InitState(seed);
        terrainMap = new string[generatedWidth, generatedHeight];

        for (int y = 0; y < generatedHeight; y++)
        {
            for (int x = 0; x < generatedWidth; x++)
            {
                float r = Random.value;
                string tile = r switch
                {
                    < 0.05f => "VOL",
                    < 0.15f => "MTN",
                    < 0.50f => "PLA",
                    < 0.80f => "DES",
                    < 0.95f => "RIV",
                    _       => "CLR"
                };
                terrainMap[x, y] = tile;
            }
        }

        for (int y = 0; y < generatedHeight; y++)
        {
            for (int x = 0; x < generatedWidth; x++)
            {
                if (terrainMap[x, y] == "RIV" && IsInvalidPlacement("RIV", x, y))
                {
                    terrainMap[x, y] = "PLA";
                }
            }
        }

        EnsureClearZonesOnOppositeSides();

        for (int y = 0; y < generatedHeight; y++)
        {
            for (int x = 0; x < generatedWidth; x++)
            {
                string code = terrainMap[x, y];
                Vector3 position = GridToIsometric(x, generatedHeight - y - 1);
                InstantiateTile(code, position);
                UpdateTerrainHeight(x, y, position);
            }
        }

        RebuildNavMesh();
    }

    Vector3 GridToIsometric(int x, int y)
    {
        float isoX = (x - y) * tileWidth / 2;
        float isoY = (x + y) * tileHeight / 2;
        return new Vector3(isoX, isoY, 0);
    }

    void InstantiateTile(string code, Vector3 position)
    {
        GameObject prefab = code switch
        {
            "VOL" => VolcanoPrefab,
            "RIV" => RiverPrefab,
            "DES" => DesertPrefab,
            "MTN" => MountainPrefab,
            "PLA" => PlainsPrefab,
            "CLR" => ClearedLandPrefab,
            _ => null
        };

        if (code == "CLR")
            buildablePlots.Add(position);

        if (prefab != null)
            Instantiate(prefab, position, Quaternion.identity, transform);
    }

    bool IsInvalidPlacement(string code, int x, int y)
    {
        if (code != "RIV") return false;

        string[] invalidNeighbors = { "VOL" };
        int[,] directions = new int[,] {
            { -1,  0 },
            {  1,  0 },
            {  0, -1 },
            {  0,  1 }
        };

        for (int i = 0; i < directions.GetLength(0); i++)
        {
            int nx = x + directions[i, 0];
            int ny = y + directions[i, 1];

            if (nx >= 0 && ny >= 0 && nx < terrainMap.GetLength(0) && ny < terrainMap.GetLength(1))
            {
                string neighbor = terrainMap[nx, ny];
                foreach (string bad in invalidNeighbors)
                {
                    if (neighbor == bad)
                        return true;
                }
            }
        }

        return false;
    }

    void EnsureClearZonesOnOppositeSides()
    {
        int width = terrainMap.GetLength(0);
        int height = terrainMap.GetLength(1);

        List<Vector2Int> clearZones = FindClearZones(2);

        // Ensure we place two clear zones on opposite sides of the map
        if (clearZones.Count < 2)
        {
            // Force placement of clear zones if they don't exist
            Vector2Int leftZone = new Vector2Int(1, 1);
            Vector2Int rightZone = new Vector2Int(width - 3, height - 3);

            CreateClearZone(leftZone.x, leftZone.y);
            CreateClearZone(rightZone.x, rightZone.y);
        }
        else
        {
            // Otherwise, just ensure there are clear zones on opposite sides
            Vector2Int leftZone = new Vector2Int(1, 1);
            Vector2Int rightZone = new Vector2Int(width - 3, height - 3);

            CreateClearZone(leftZone.x, leftZone.y);
            CreateClearZone(rightZone.x, rightZone.y);
        }
    }

    List<Vector2Int> FindClearZones(int size)
    {
        List<Vector2Int> found = new();
        int width = terrainMap.GetLength(0);
        int height = terrainMap.GetLength(1);

        for (int y = 0; y < height - (size - 1); y++)
        {
            for (int x = 0; x < width - (size - 1); x++)
            {
                bool isClear = true;
                for (int dy = 0; dy < size; dy++)
                {
                    for (int dx = 0; dx < size; dx++)
                    {
                        if (terrainMap[x + dx, y + dy] != "CLR")
                        {
                            isClear = false;
                            break;
                        }
                    }
                    if (!isClear) break;
                }

                if (isClear)
                    found.Add(new Vector2Int(x, y));
            }
        }

        return found;
    }

    void CreateClearZone(int startX, int startY)
    {
        for (int dy = 0; dy < 2; dy++)
        {
            for (int dx = 0; dx < 2; dx++)
            {
                terrainMap[startX + dx, startY + dy] = "CLR";
            }
        }
    }

    void UpdateTerrainHeight(int x, int y, Vector3 position)
    {
        // Assuming terrain has a heightmap (set height at x, y based on position)
        float terrainHeight = 0;

        switch (terrainMap[x, y])
        {
            case "VOL": terrainHeight = 10f; break;
            case "MTN": terrainHeight = 7f; break;
            case "PLA": terrainHeight = 1f; break;
            case "DES": terrainHeight = 0.5f; break;
            case "RIV": terrainHeight = 0.2f; break;
            case "CLR": terrainHeight = 0f; break;
        }

        // Set height on terrain for that specific tile
        TerrainData terrainData = terrain.terrainData;
        float[,] heights = terrainData.GetHeights(x, y, 1, 1);  // Get existing height for that tile

        heights[0, 0] = terrainHeight;  // Update height

        terrainData.SetHeights(x, y, heights);  // Apply the updated height
    }

    void RebuildNavMesh()
    {
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (terrainMap == null) return;

        Gizmos.color = Color.green;
        foreach (var pos in buildablePlots)
        {
            Gizmos.DrawWireCube(pos + Vector3.up * 0.1f, Vector3.one * 0.9f);
        }
    }
#endif
}
