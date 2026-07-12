using UnityEngine;

/// <summary>
/// Benders shield their allies: select benders and press Q - each one weaves
/// its element's barrier (see ElementalShield) around itself and the nearest
/// allied units. Per-bender cooldown. AI-controlled benders (and the enemy's)
/// cast automatically when a fight starts around them.
/// Auto-added to every bender unit; the Avatar shields in whatever element
/// it is currently bending.
/// </summary>
[RequireComponent(typeof(Unit))]
public class ElementalShieldAbility : MonoBehaviour
{
    public KeyCode castKey = KeyCode.Q;
    public float shieldDuration = 8f;
    public float cooldownSeconds = 25f;
    public float castRadius = 8f;
    [Tooltip("Allies shielded per cast, nearest first (the caster included).")]
    public int maxTargets = 4;

    private Unit unit;
    private float cooldownRemaining;

    private void Start()
    {
        unit = GetComponent<Unit>();
    }

    private void Update()
    {
        cooldownRemaining -= Time.deltaTime;
        if (cooldownRemaining > 0f) return;

        if (FactionManager.IsAIControlled(unit.FactionId))
        {
            AutoCast();
            return;
        }

        // Player: Q with this bender selected.
        if (!FactionUtility.IsLocallyControlled(gameObject)) return;
        if (Input.GetKeyDown(castKey) &&
            UnitSelectionManager.Instance != null &&
            UnitSelectionManager.Instance.selectedUnitsList.Contains(gameObject))
        {
            Cast();
        }
    }

    /// <summary>AI benders raise shields when combat reaches them.</summary>
    private void AutoCast()
    {
        AttackController attack = GetComponent<AttackController>();
        if (attack != null && attack.targetToAttack != null)
        {
            Cast();
        }
    }

    public void Cast()
    {
        Nation element = ResolveElement();
        int shielded = 0;
        int myFaction = unit.FactionId;

        // Shield-mastery upgrades: bought once, they empower EVERY bender of
        // the upgrade's unit type - stronger, longer, more often.
        float strengthMult, durationMult, cooldownMult;
        UpgradeManager.GetShieldMultipliers(unit, out strengthMult, out durationMult, out cooldownMult);
        float duration = shieldDuration * durationMult;

        // Nearest allies first, caster included.
        System.Collections.Generic.List<Unit> allies = new System.Collections.Generic.List<Unit> { unit };
        foreach (Collider hit in Physics.OverlapSphere(transform.position, castRadius))
        {
            Unit other = hit.GetComponentInParent<Unit>();
            if (other == null || other == unit || allies.Contains(other)) continue;
            if (FactionUtility.GetFactionId(other.gameObject) != myFaction) continue;
            allies.Add(other);
        }
        allies.Sort((a, b) =>
            Vector3.Distance(a.transform.position, transform.position)
                .CompareTo(Vector3.Distance(b.transform.position, transform.position)));

        foreach (Unit ally in allies)
        {
            if (shielded >= maxTargets) break;
            ElementalShield.Apply(ally, element, duration, strengthMult);
            shielded++;
        }

        cooldownRemaining = cooldownSeconds * cooldownMult;
        Debug.Log($"{gameObject.name} raises a {element} shield over {shielded} unit(s)!");
    }

    private Nation ResolveElement()
    {
        // The Avatar shields in whatever element it is currently bending.
        AvatarUnit avatar = GetComponent<AvatarUnit>();
        if (avatar != null) return avatar.currentElement;

        switch (unit.unitType)
        {
            case Unit.UnitType.Waterbender: return Nation.Water;
            case Unit.UnitType.Earthbender: return Nation.Earth;
            case Unit.UnitType.Firebender: return Nation.Fire;
            default: return Nation.Air;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, castRadius);
    }
}
