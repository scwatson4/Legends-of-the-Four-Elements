using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A tear between the physical and spirit worlds. Periodically releases
/// spirits (mostly dark ones) up to a living cap - a persistent threat and a
/// bounty/taming farm worth controlling. Shows on the minimap once
/// discovered. Place by hand or via MapGenerator.
/// </summary>
public class SpiritPortal : MonoBehaviour
{
    public GameObject darkSpiritPrefab;
    public GameObject friendlySpiritPrefab;
    [Range(0f, 1f)] public float darkSpiritChance = 0.75f;

    public float spawnInterval = 45f;
    public int maxAliveSpirits = 3;
    public float spawnRadius = 5f;

    [Tooltip("Optional swirling portal VFX, purely cosmetic.")]
    public GameObject portalEffect;

    private readonly List<GameObject> alive = new List<GameObject>();
    private float timer;

    private void Start()
    {
        MinimapPOI.Ensure(gameObject, MinimapPOI.POIType.SpiritPortal);
        timer = spawnInterval * 0.5f; // first spirit comes fairly soon
    }

    private void Update()
    {
        if (NetworkGuard.BlockLocalSimulation) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = spawnInterval;

        alive.RemoveAll(s => s == null);
        if (alive.Count >= maxAliveSpirits) return;

        GameObject prefab = Random.value < darkSpiritChance ? darkSpiritPrefab : friendlySpiritPrefab;
        if (prefab == null) prefab = darkSpiritPrefab != null ? darkSpiritPrefab : friendlySpiritPrefab;
        if (prefab == null) return;

        Vector2 circle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 pos = transform.position + new Vector3(circle.x, 0f, circle.y);
        alive.Add(Instantiate(prefab, pos, Quaternion.identity));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.8f, 0.3f, 1f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
