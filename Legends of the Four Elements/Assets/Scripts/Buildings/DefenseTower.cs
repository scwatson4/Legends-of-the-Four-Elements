using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A defensive tower that automatically fires at hostile units in range,
/// with a bending style per element - and a bigger special attack every
/// Nth shot:
///
///   Fire  - shots ignite (burn damage over time); special: LIGHTNING strike
///           for triple damage.
///   Air   - shots blast targets backwards; special: TORNADO that damages and
///           scatters everything around the target.
///   Water - ice crystals slow the target; special: a WAVE that hits and
///           slows every enemy near the target.
///   Earth - boulders always splash nearby enemies; special: a massive
///           boulder for double damage and full splash.
///   None  - plain shots (the original behaviour).
///
/// Pair with a Structure component so it can be destroyed. Set the element
/// per nation's tower prefab (see docs/ROSTERS.md).
/// </summary>
public class DefenseTower : MonoBehaviour
{
    public enum TowerElement
    {
        None = 0,
        Fire = 1,
        Air = 2,
        Water = 3,
        Earth = 4
    }

    public TowerElement element = TowerElement.None;
    public float range = 15f;
    public int damage = 8;
    public float fireInterval = 1.5f;

    [Header("Elemental Special")]
    [Tooltip("Every Nth shot is the big one (lightning/tornado/wave/heavy boulder). 0 = never.")]
    public int specialEveryNShots = 4;
    public float splashRadius = 5f;

    [Header("Effect Tuning")]
    public float burnDamagePerSecond = 3f;
    public float burnDuration = 3f;
    public float slowMultiplier = 0.5f;
    public float slowDuration = 3f;
    public float knockbackDistance = 5f;
    [Range(0f, 1f)] public float splashDamageFraction = 0.5f;

    [Header("Visuals")]
    [Tooltip("Muzzle VFX toggled briefly on each shot.")]
    public GameObject shotEffect;
    [Tooltip("Bigger VFX toggled on each special shot (lightning bolt, tornado...).")]
    public GameObject specialShotEffect;
    [Tooltip("Where the shot visually comes from (defaults to this transform).")]
    public Transform firePoint;

    private float fireTimer;
    private Unit currentTarget;
    private int shotCounter;

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
        shotCounter++;
        bool special = element != TowerElement.None && specialEveryNShots > 0 &&
                       shotCounter % specialEveryNShots == 0;

        switch (element)
        {
            case TowerElement.Fire:
                if (special)
                {
                    // Lightning: pure, devastating, instant - unless the
                    // target knows Iroh's redirection technique.
                    if (!LightningRedirect.TryRedirect(target, damage * 3, gameObject))
                    {
                        target.TakeDamage(damage * 3);
                    }
                }
                else
                {
                    target.TakeDamage(damage);
                    BurnEffect.Apply(target, burnDamagePerSecond, burnDuration);
                }
                break;

            case TowerElement.Air:
                target.TakeDamage(damage);
                Knockback(target, transform.position);
                if (special)
                {
                    // Tornado: damage and scatter everything around the target.
                    foreach (Unit victim in HostilesAround(target.transform.position))
                    {
                        if (victim != target)
                        {
                            victim.TakeDamage(Mathf.RoundToInt(damage * splashDamageFraction));
                        }
                        Knockback(victim, target.transform.position);
                    }
                }
                break;

            case TowerElement.Water:
                target.TakeDamage(damage);
                SlowEffect.Apply(target, slowMultiplier, slowDuration);
                if (special)
                {
                    // Wave: soak and slow the whole pack.
                    foreach (Unit victim in HostilesAround(target.transform.position))
                    {
                        if (victim != target) victim.TakeDamage(damage);
                        SlowEffect.Apply(victim, slowMultiplier, slowDuration);
                    }
                }
                break;

            case TowerElement.Earth:
                // Boulders always splash; the special one is a monster rock.
                int boulderDamage = special ? damage * 2 : damage;
                target.TakeDamage(boulderDamage);
                foreach (Unit victim in HostilesAround(target.transform.position))
                {
                    if (victim == target) continue;
                    float fraction = special ? 1f : splashDamageFraction;
                    victim.TakeDamage(Mathf.RoundToInt(boulderDamage * fraction));
                }
                break;

            default:
                target.TakeDamage(damage);
                break;
        }

        PulseEffect(special && specialShotEffect != null ? specialShotEffect : shotEffect);

        Transform origin = firePoint != null ? firePoint : transform;
        Debug.DrawLine(origin.position, target.transform.position,
            special ? Color.magenta : Color.red, 0.2f);
    }

    /// <summary>Shove a unit away from a point (wind blasts, tornado throws).</summary>
    private void Knockback(Unit victim, Vector3 fromPoint)
    {
        if (victim == null) return;

        // Air shields anchor their bearer against wind.
        ElementalShield shield = victim.GetComponent<ElementalShield>();
        if (shield != null && shield.blocksKnockback) return;

        NavMeshAgent agent = victim.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        Vector3 direction = (victim.transform.position - fromPoint).normalized;
        if (direction.sqrMagnitude < 0.01f) direction = Random.insideUnitSphere;
        direction.y = 0f;

        Vector3 pushed = victim.transform.position + direction.normalized * knockbackDistance;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(pushed, out hit, knockbackDistance, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
    }

    private System.Collections.Generic.List<Unit> HostilesAround(Vector3 center)
    {
        System.Collections.Generic.List<Unit> result = new System.Collections.Generic.List<Unit>();
        foreach (Collider hit in Physics.OverlapSphere(center, splashRadius))
        {
            Unit unit = hit.GetComponentInParent<Unit>();
            if (unit == null || result.Contains(unit)) continue;
            if (!FactionUtility.AreHostile(gameObject, unit.gameObject)) continue;
            result.Add(unit);
        }
        return result;
    }

    private void PulseEffect(GameObject effect)
    {
        if (effect == null) return;
        effect.SetActive(false);
        effect.SetActive(true);
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, splashRadius);
    }
}
