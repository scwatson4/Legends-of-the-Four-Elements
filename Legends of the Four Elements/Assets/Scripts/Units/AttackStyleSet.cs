using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Gives a unit 3-4 attack styles it alternates between at random - each
/// swing varies in power and can carry an elemental effect (burn, slow,
/// knockback). Added automatically to every combat unit; if you leave the
/// styles list empty, a themed default set is generated from the unit's
/// type:
///   Airbender:   Gale Palm / Wind Slice / Cyclone Kick (knockback) / Sonic Boom
///   Waterbender: Water Whip / Ice Shard (slow) / Wave Crash / Frost Bind (slow)
///   Earthbender: Rock Throw / Stone Fist / Boulder Smash / Quake Kick (knockback)
///   Firebender:  Fire Jab / Flame Burst (burn) / Fire Whip (burn) / Lightning Jolt
///   Everything else: Strike / Heavy Blow / Quick Hit
/// Hand-author the list in the inspector to customize any unit.
/// </summary>
public class AttackStyleSet : MonoBehaviour
{
    public enum StyleEffect
    {
        None = 0,
        Burn = 1,
        Slow = 2,
        Knockback = 3,
        Root = 4,   // earth grips the target's feet - works anywhere
        Freeze = 5  // full ice prison - only near a large water source
    }

    [System.Serializable]
    public class Style
    {
        public string styleName = "Strike";
        public float damageMultiplier = 1f;
        public StyleEffect effect = StyleEffect.None;
        [Tooltip("Lightning attacks can be REDIRECTED by upgraded firebenders.")]
        public bool isLightning = false;

        public Style() { }

        public Style(string name, float damageMultiplier, StyleEffect effect = StyleEffect.None,
            bool isLightning = false)
        {
            styleName = name;
            this.damageMultiplier = damageMultiplier;
            this.effect = effect;
            this.isLightning = isLightning;
        }
    }

    [Tooltip("Leave empty to auto-generate a themed set from the Unit's type.")]
    public Style[] styles;

    [Header("Effect Tuning")]
    public float burnDamagePerSecond = 3f;
    public float burnDuration = 2.5f;
    public float slowMultiplier = 0.55f;
    public float slowDuration = 2.5f;
    public float knockbackDistance = 3f;
    public float rootDuration = 2f;
    public float freezeDuration = 3f;
    [Tooltip("How close a river/glacier/fish-shoal must be for freezing to work.")]
    public float waterSearchRadius = 18f;

    private void Awake()
    {
        if (styles != null && styles.Length > 0) return;

        Unit unit = GetComponent<Unit>();
        Unit.UnitType type = unit != null ? unit.unitType : Unit.UnitType.Airbender;

        switch (type)
        {
            case Unit.UnitType.Airbender:
                styles = new[]
                {
                    new Style("Gale Palm", 1f),
                    new Style("Wind Slice", 1.2f),
                    new Style("Cyclone Kick", 0.9f, StyleEffect.Knockback),
                    new Style("Sonic Boom", 1.5f)
                };
                break;
            case Unit.UnitType.Waterbender:
                styles = new[]
                {
                    new Style("Water Whip", 1f),
                    new Style("Ice Shard", 1.1f, StyleEffect.Slow),
                    new Style("Wave Crash", 1.35f),
                    new Style("Ice Prison", 0.7f, StyleEffect.Freeze) // needs real water nearby
                };
                break;
            case Unit.UnitType.Earthbender:
                styles = new[]
                {
                    new Style("Rock Throw", 1f),
                    new Style("Boulder Smash", 1.5f),
                    new Style("Earth Grip", 0.8f, StyleEffect.Root), // the ground itself holds them
                    new Style("Quake Kick", 0.9f, StyleEffect.Knockback)
                };
                break;
            case Unit.UnitType.Firebender:
                styles = new[]
                {
                    new Style("Fire Jab", 1f),
                    new Style("Flame Burst", 1.2f, StyleEffect.Burn),
                    new Style("Fire Whip", 1.1f, StyleEffect.Burn),
                    new Style("Lightning Jolt", 1.6f, StyleEffect.None, true)
                };
                break;
            case Unit.UnitType.Spirit:
                styles = new[]
                {
                    new Style("Spirit Lash", 1f),
                    new Style("Umbral Grasp", 1.15f, StyleEffect.Slow),
                    new Style("Wail", 1.4f)
                };
                break;
            default:
                styles = new[]
                {
                    new Style("Strike", 1f),
                    new Style("Heavy Blow", 1.3f),
                    new Style("Quick Hit", 0.85f)
                };
                break;
        }
    }

    public Style PickRandom()
    {
        if (styles == null || styles.Length == 0) return null;
        return styles[Random.Range(0, styles.Length)];
    }

    /// <summary>Applies the style's rider effect to a unit that just got hit.</summary>
    public void ApplyEffect(Style style, Unit target)
    {
        if (style == null || target == null) return;

        switch (style.effect)
        {
            case StyleEffect.Burn:
                BurnEffect.Apply(target, burnDamagePerSecond, burnDuration);
                break;
            case StyleEffect.Slow:
                SlowEffect.Apply(target, slowMultiplier, slowDuration);
                break;
            case StyleEffect.Knockback:
                KnockbackUtility.Push(target, transform.position, knockbackDistance);
                break;
            case StyleEffect.Root:
                // Earthbending: the ground clamps around their feet - anywhere.
                RootEffect.Apply(target, rootDuration);
                break;
            case StyleEffect.Freeze:
                // Waterbending needs actual water to freeze someone solid;
                // far from any source it's just a chilling splash (slow).
                if (WaterProximity.IsNearWater(transform.position, waterSearchRadius))
                {
                    RootEffect.Apply(target, freezeDuration);
                    SlowEffect.Apply(target, slowMultiplier, freezeDuration + 1.5f); // lingering chill
                }
                else
                {
                    SlowEffect.Apply(target, slowMultiplier, slowDuration);
                }
                break;
        }
    }
}

/// <summary>Shoves a unit away from a point along the NavMesh (wind blasts, quakes).</summary>
public static class KnockbackUtility
{
    public static void Push(Unit victim, Vector3 fromPoint, float distance)
    {
        if (victim == null) return;

        // An air shield plants the bearer like a mountain in the wind.
        ElementalShield shield = victim.GetComponent<ElementalShield>();
        if (shield != null && shield.blocksKnockback) return;

        NavMeshAgent agent = victim.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        Vector3 direction = victim.transform.position - fromPoint;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) direction = Random.insideUnitCircle.normalized;

        Vector3 pushed = victim.transform.position + direction.normalized * distance;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(pushed, out hit, distance, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
    }
}
