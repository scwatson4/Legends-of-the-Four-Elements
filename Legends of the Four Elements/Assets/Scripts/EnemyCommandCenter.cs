using UnityEngine;

public class EnemyCommandCenter : CommandCenter
{
    [SerializeField] private GameObject enemyUnitPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float minSpawnDistance = 2f; // Minimum distance from SpawnPoint

    public void SpawnWave(int enemyCount)
    {
        if (enemyUnitPrefab == null)
        {
            Debug.LogError("EnemyUnitPrefab not assigned in EnemyCommandCenter Inspector!");
            return;
        }
        if (spawnPoint == null)
        {
            Debug.LogError("SpawnPoint not assigned in EnemyCommandCenter Inspector!");
            return;
        }

        // Check CommandCenter size for additional offset
        float commandCenterSize = 0f;
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            commandCenterSize = renderer.bounds.extents.magnitude;
            Debug.Log($"CommandCenter size: {commandCenterSize}");
        }

        // Use minSpawnDistance plus CommandCenter size to ensure spawning outside
        float spawnDistance = Mathf.Max(minSpawnDistance, commandCenterSize + 1f);

        for (int i = 0; i < enemyCount; i++)
        {
            // Circular spawn pattern outside spawnDistance
            Vector2 circleOffset = Random.insideUnitCircle.normalized * spawnDistance;
            Vector3 spawnPosition = spawnPoint.position + new Vector3(circleOffset.x, 0, circleOffset.y);
            spawnPosition.y = spawnPoint.position.y; // Keep on ground

            GameObject enemy = Instantiate(enemyUnitPrefab, spawnPosition, Quaternion.identity);
            Debug.Log($"Spawned enemy {i + 1}/{enemyCount} at {spawnPosition}");
        }
    }
}