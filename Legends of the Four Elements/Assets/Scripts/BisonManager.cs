using UnityEngine;

public class BisonManager : MonoBehaviour
{
    public GameObject bisonPrefab;
    public int bisonCount = 5;

    [Header("Spawn Settings")]
    public Vector3 spawnAreaSize = new Vector3(25f, 0f, 25f);
    public Vector3 individualRoamBounds = new Vector3(15f, 3f, 15f);

    void Start()
    {
        for (int i = 0; i < bisonCount; i++)
        {
            SpawnBison();
        }
    }

    void SpawnBison()
    {
        // Use the BisonManager's position as spawn center
        Vector3 spawnCenter = transform.position;

        Vector3 spawnPos = new Vector3(
            Random.Range(spawnCenter.x - spawnAreaSize.x / 2f, spawnCenter.x + spawnAreaSize.x / 2f),
            spawnCenter.y,
            Random.Range(spawnCenter.z - spawnAreaSize.z / 2f, spawnCenter.z + spawnAreaSize.z / 2f)
        );

        GameObject bison = Instantiate(bisonPrefab, spawnPos, Quaternion.identity);

        BisonWanderer wanderer = bison.GetComponent<BisonWanderer>();
        if (wanderer != null)
        {
            wanderer.centerPoint = spawnPos; // they roam around their own spawn point
            wanderer.bounds = individualRoamBounds;
        }
    }
}
