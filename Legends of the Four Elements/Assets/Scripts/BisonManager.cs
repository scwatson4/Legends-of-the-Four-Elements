using UnityEngine;

public class BisonManager : MonoBehaviour
{
    public GameObject bisonPrefab;
    public int bisonCount = 5;

    [Header("Spawn Settings")]
    public Vector3 spawnAreaSize = new Vector3(25f, 0f, 25f);
    public Vector3 individualRoamBounds = new Vector3(15f, 3f, 15f);

    [Header("Optional: Bison Size Variation")]
    public bool varySize = false;
    [Range(0.5f, 3f)] public float minScale = 0.8f;
    [Range(0.5f, 3f)] public float maxScale = 1.2f;

    void Start()
    {
        for (int i = 0; i < bisonCount; i++)
        {
            SpawnBison();
        }
    }

    void SpawnBison()
    {
        Vector3 spawnCenter = transform.position;

        Vector3 spawnPos = new Vector3(
            Random.Range(spawnCenter.x - spawnAreaSize.x / 2f, spawnCenter.x + spawnAreaSize.x / 2f),
            spawnCenter.y,
            Random.Range(spawnCenter.z - spawnAreaSize.z / 2f, spawnCenter.z + spawnAreaSize.z / 2f)
        );

        GameObject bison = Instantiate(bisonPrefab, spawnPos, Quaternion.identity);

        // Optional: vary size
        if (varySize)
        {
            float scale = Random.Range(minScale, maxScale);
            bison.transform.localScale = new Vector3(scale, scale, scale);
        }

        // Assign wandering behavior
        BisonWanderer wanderer = bison.GetComponent<BisonWanderer>();
        if (wanderer != null)
        {
            wanderer.centerPoint = spawnPos;
            wanderer.bounds = individualRoamBounds;
        }
    }
}
