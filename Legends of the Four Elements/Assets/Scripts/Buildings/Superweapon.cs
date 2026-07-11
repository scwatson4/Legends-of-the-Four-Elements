using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// C&amp;C-style superweapon, one flavor per element. Charges over minutes;
/// when ready, press P (or wire a UI button to Arm) and left-click a target:
///
///   CometBarrage (Fire)  - waves of falling fire: heavy damage + burn to
///                          everything in the area, buildings included.
///   FlashFreeze  (Water) - freezes every enemy in the area nearly solid
///                          for 8 seconds, with light damage.
///   GreatStorm   (Air)   - a hurricane that damages and hurls every enemy
///                          away from the epicenter.
///   StoneRampart (Earth) - earthbends a ring of stone walls out of the
///                          ground around the point (assign the wall
///                          prefab); without one it quakes: damage + scatter.
///
/// Put on each nation's Special building (Spirit Shrine / War Sanctum...).
/// AI-owned superweapons auto-fire at the nearest enemy base when charged.
/// </summary>
public class Superweapon : MonoBehaviour
{
    public enum PowerType
    {
        CometBarrage = 0, // Fire
        FlashFreeze = 1,  // Water
        GreatStorm = 2,   // Air
        StoneRampart = 3  // Earth
    }

    public PowerType power = PowerType.CometBarrage;
    public float cooldownSeconds = 240f;
    public float radius = 15f;
    public int damage = 40;
    public KeyCode armKey = KeyCode.P;

    [Header("Stone Rampart")]
    [Tooltip("Wall prefab raised in a ring (Earth). Empty = quake instead.")]
    public GameObject wallPrefab;
    public int rampartSegments = 8;

    [Header("Visuals (optional)")]
    [Tooltip("VFX instantiated at the target point when it fires.")]
    public GameObject impactVfx;

    private float chargeRemaining;
    private static Superweapon targeting;

    /// <summary>True while a superweapon waits for its target click (pauses RTS input).</summary>
    public static bool IsTargeting => targeting != null;

    public bool IsCharged => chargeRemaining <= 0f;
    public float Charge01 => 1f - Mathf.Clamp01(chargeRemaining / Mathf.Max(1f, cooldownSeconds));

    private void Start()
    {
        chargeRemaining = cooldownSeconds; // charges from zero, C&C style
    }

    private void OnDestroy()
    {
        if (targeting == this) targeting = null;
    }

    private void Update()
    {
        if (NetworkGuard.BlockLocalSimulation) return;

        if (chargeRemaining > 0f)
        {
            chargeRemaining -= Time.deltaTime;
            if (IsCharged)
            {
                Debug.Log($"{gameObject.name}: {power} is CHARGED.");
            }
            return;
        }

        int ownerFaction = FactionUtility.GetFactionId(gameObject);

        // AI: unleash on the nearest enemy base as soon as it's ready.
        if (FactionManager.IsAIControlled(ownerFaction))
        {
            Vector3? target = FindNearestHostileBase(ownerFaction);
            if (target.HasValue) Fire(target.Value);
            return;
        }

        // Player: arm with the key, then click the target.
        if (ownerFaction != FactionManager.LocalPlayerFactionId) return;

        if (targeting == null && Input.GetKeyDown(armKey))
        {
            targeting = this;
            Debug.Log($"{power} armed - left-click the target area (right-click/Esc cancels).");
        }

        if (targeting != this) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            targeting = null;
            return;
        }

        if (Input.GetMouseButtonDown(0) && Camera.main != null)
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity))
            {
                targeting = null;
                Fire(hit.point);
            }
        }
    }

    // ------------------------------------------------------------------

    public void Arm()
    {
        if (IsCharged && targeting == null) targeting = this;
    }

    private void Fire(Vector3 point)
    {
        chargeRemaining = cooldownSeconds;

        if (impactVfx != null)
        {
            Destroy(Instantiate(impactVfx, point, Quaternion.identity), 8f);
        }

        switch (power)
        {
            case PowerType.CometBarrage: StartCoroutine(CometBarrage(point)); break;
            case PowerType.FlashFreeze: FlashFreezeAt(point); break;
            case PowerType.GreatStorm: GreatStormAt(point); break;
            case PowerType.StoneRampart: StoneRampartAt(point); break;
        }

        Debug.Log($"{power} unleashed at {point}!");
    }

    private IEnumerator CometBarrage(Vector3 point)
    {
        // Three waves of falling fire over ~3 seconds.
        for (int wave = 0; wave < 3; wave++)
        {
            foreach (Unit unit in HostileUnitsIn(point))
            {
                unit.TakeDamage(damage);
                BurnEffect.Apply(unit, 5f, 4f);
            }
            foreach (Structure structure in HostileStructuresIn(point))
            {
                structure.TakeDamage(damage / 2);
            }
            foreach (CommandCenter cc in HostileCommandCentersIn(point))
            {
                cc.TakeDamage(damage / 2);
            }
            yield return new WaitForSeconds(1.2f);
        }
    }

    private void FlashFreezeAt(Vector3 point)
    {
        foreach (Unit unit in HostileUnitsIn(point))
        {
            unit.TakeDamage(damage / 3);
            SlowEffect.Apply(unit, 0.08f, 8f); // frozen nearly solid
        }
    }

    private void GreatStormAt(Vector3 point)
    {
        foreach (Unit unit in HostileUnitsIn(point))
        {
            unit.TakeDamage(damage / 2);
            KnockbackUtility.Push(unit, point, 12f);
        }
    }

    private void StoneRampartAt(Vector3 point)
    {
        int ownerFaction = FactionUtility.GetFactionId(gameObject);

        if (wallPrefab == null)
        {
            // Quake fallback: damage and scatter.
            foreach (Unit unit in HostileUnitsIn(point))
            {
                unit.TakeDamage(damage);
                KnockbackUtility.Push(unit, point, 6f);
            }
            return;
        }

        float ringRadius = radius * 0.7f;
        for (int i = 0; i < rampartSegments; i++)
        {
            float angle = (360f / rampartSegments) * i * Mathf.Deg2Rad;
            Vector3 pos = point + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * ringRadius;
            Quaternion facing = Quaternion.LookRotation(pos - point);

            GameObject wall = Instantiate(wallPrefab, pos, facing);
            FactionUtility.SetFaction(wall, ownerFaction);
        }
    }

    // ------------------------------------------------------------------

    private List<Unit> HostileUnitsIn(Vector3 point)
    {
        List<Unit> result = new List<Unit>();
        foreach (Collider hit in Physics.OverlapSphere(point, radius))
        {
            Unit unit = hit.GetComponentInParent<Unit>();
            if (unit == null || result.Contains(unit)) continue;
            if (FactionUtility.AreHostile(gameObject, unit.gameObject)) result.Add(unit);
        }
        return result;
    }

    private List<Structure> HostileStructuresIn(Vector3 point)
    {
        List<Structure> result = new List<Structure>();
        foreach (Collider hit in Physics.OverlapSphere(point, radius))
        {
            Structure structure = hit.GetComponentInParent<Structure>();
            if (structure == null || result.Contains(structure)) continue;
            if (FactionUtility.AreHostile(gameObject, structure.gameObject)) result.Add(structure);
        }
        return result;
    }

    private List<CommandCenter> HostileCommandCentersIn(Vector3 point)
    {
        List<CommandCenter> result = new List<CommandCenter>();
        foreach (Collider hit in Physics.OverlapSphere(point, radius))
        {
            CommandCenter cc = hit.GetComponentInParent<CommandCenter>();
            if (cc == null || result.Contains(cc)) continue;
            if (FactionUtility.AreHostile(gameObject, cc.gameObject)) result.Add(cc);
        }
        return result;
    }

    private Vector3? FindNearestHostileBase(int ownerFaction)
    {
        CommandCenter best = null;
        float bestDistance = Mathf.Infinity;
        foreach (CommandCenter cc in FindObjectsByType<CommandCenter>(FindObjectsSortMode.None))
        {
            if (!FactionManager.AreHostile(ownerFaction, FactionUtility.GetFactionId(cc.gameObject))) continue;
            float distance = Vector3.Distance(transform.position, cc.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = cc;
            }
        }
        return best != null ? best.transform.position : (Vector3?)null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
