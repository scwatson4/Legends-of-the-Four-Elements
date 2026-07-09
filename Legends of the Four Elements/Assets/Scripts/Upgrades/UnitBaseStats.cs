using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Remembers a unit's original stats so upgrade multipliers can be
/// re-applied cleanly (never compounding). Added automatically by
/// UpgradeManager; no need to put it on prefabs.
/// </summary>
public class UnitBaseStats : MonoBehaviour
{
    private float baseMaxHealth;
    private int baseDamage;
    private float baseSpeed;
    private float baseSight;
    private bool captured;

    private void Capture()
    {
        if (captured) return;

        Unit unit = GetComponent<Unit>();
        AttackController attack = GetComponent<AttackController>();
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        VisionSource vision = GetComponent<VisionSource>();

        baseMaxHealth = unit != null ? unit.maxUnitHealth : 0f;
        baseDamage = attack != null ? attack.unitDamage : 0;
        baseSpeed = agent != null ? agent.speed : 0f;
        baseSight = vision != null ? vision.sightRange
            : FogOfWar.Instance != null ? FogOfWar.Instance.defaultSightRange
            : 15f;
        captured = true;
    }

    public void ApplyMultipliers(float damageMult, float healthMult, float speedMult, float sightMult = 1f)
    {
        Capture();

        Unit unit = GetComponent<Unit>();
        if (unit != null && baseMaxHealth > 0f)
        {
            unit.SetMaxHealth(baseMaxHealth * healthMult);
        }

        AttackController attack = GetComponent<AttackController>();
        if (attack != null && baseDamage > 0)
        {
            attack.unitDamage = Mathf.RoundToInt(baseDamage * damageMult);
        }

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null && baseSpeed > 0f)
        {
            agent.speed = baseSpeed * speedMult;
        }

        // Sensing upgrades: extend fog-of-war sight, adding the component
        // when the unit relied on the default range before.
        if (!Mathf.Approximately(sightMult, 1f))
        {
            VisionSource vision = GetComponent<VisionSource>();
            if (vision == null) vision = gameObject.AddComponent<VisionSource>();
            vision.sightRange = baseSight * sightMult;
        }
    }
}
