using UnityEngine;

/// <summary>
/// A defensive tower that automatically fires at hostile units in range.
/// Nation flavor: Air wind cannon pagoda, Water ice-spike tower, Earth rock
/// launcher, Fire flame turret. Pair with a Structure component so it can be
/// destroyed.
/// </summary>
public class DefenseTower : MonoBehaviour
{
    public float range = 15f;
    public int damage = 8;
    public float fireInterval = 1.5f;

    [Tooltip("Optional muzzle VFX toggled briefly on each shot.")]
    public GameObject shotEffect;
    [Tooltip("Where the shot visually comes from (defaults to this transform).")]
    public Transform firePoint;

    private float fireTimer;
    private Unit currentTarget;

    private void Update()
    {
        if (NetworkGuard.BlockLocalSimulation) return;

        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f) return;
        fireTimer = fireInterval;

        if (currentTarget == null || !InRange(currentTarget.transform.position))
        {
            currentTarget = FindTarget();
        }
        if (currentTarget == null) return;

        Fire(currentTarget);
    }

    private void Fire(Unit target)
    {
        target.TakeDamage(damage);

        if (shotEffect != null)
        {
            shotEffect.SetActive(false);
            shotEffect.SetActive(true);
        }

        Transform origin = firePoint != null ? firePoint : transform;
        Debug.DrawLine(origin.position, target.transform.position, Color.red, 0.2f);
    }

    private bool InRange(Vector3 position)
    {
        return Vector3.Distance(transform.position, position) <= range;
    }

    private Unit FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range);
        Unit best = null;
        float bestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            Unit unit = hit.GetComponentInParent<Unit>();
            if (unit == null) continue;
            if (!FactionUtility.AreHostile(gameObject, unit.gameObject)) continue;

            float distance = Vector3.Distance(transform.position, unit.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = unit;
            }
        }
        return best;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
