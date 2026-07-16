using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A bender-woven barrier on a single unit, flavored by element:
///   Air   - swirling wind: 40% damage reduction + immune to knockback
///   Water - ice casing: absorbs a flat pool of damage before shattering
///   Earth - stone shell: 60% damage reduction, but the bearer moves slower
///   Fire  - flame cloak: 30% damage reduction + scorches nearby enemies
/// Applied by ElementalShieldAbility (Q); shows a translucent element-colored
/// sphere so it reads instantly even in greybox. Reapplying refreshes it.
/// </summary>
public class ElementalShield : MonoBehaviour, IDamageInterceptor
{
    public Nation element = Nation.Air;
    public float remaining;

    [Header("Tuning (set by the caster)")]
    public float damageTakenMultiplier = 0.6f;
    public float absorbPool = 0f;          // Water: flat damage soaked
    public bool blocksKnockback = false;   // Air
    public float bearerSpeedMultiplier = 1f; // Earth: sturdy but slow
    public float fireAuraDamagePerSecond = 0f; // Fire: scorch aura
    public float fireAuraRadius = 3f;

    private GameObject visual;
    private NavMeshAgent agent;
    private float originalSpeed;
    private bool slowed;
    private float auraTick;

    public static ElementalShield Apply(Unit target, Nation element, float duration,
        float strengthMultiplier = 1f)
    {
        if (target == null) return null;

        ElementalShield shield = target.GetComponent<ElementalShield>();
        if (shield == null)
        {
            shield = target.gameObject.AddComponent<ElementalShield>();
            target.RefreshDamageInterceptors(); // make sure we're consulted
        }

        shield.Configure(element, strengthMultiplier);
        shield.remaining = Mathf.Max(shield.remaining, duration);
        return shield;
    }

    private void Configure(Nation newElement, float strength)
    {
        element = newElement;
        switch (element)
        {
            case Nation.Air:
                damageTakenMultiplier = 0.6f;
                blocksKnockback = true;
                absorbPool = 0f; fireAuraDamagePerSecond = 0f; bearerSpeedMultiplier = 1f;
                break;
            case Nation.Water:
                damageTakenMultiplier = 1f;
                absorbPool = Mathf.Max(absorbPool, 60f * strength);
                blocksKnockback = false; fireAuraDamagePerSecond = 0f; bearerSpeedMultiplier = 1f;
                break;
            case Nation.Earth:
                damageTakenMultiplier = 0.4f;
                bearerSpeedMultiplier = 0.8f;
                blocksKnockback = false; absorbPool = 0f; fireAuraDamagePerSecond = 0f;
                break;
            case Nation.Fire:
                damageTakenMultiplier = 0.7f;
                fireAuraDamagePerSecond = 4f * strength;
                blocksKnockback = false; absorbPool = 0f; bearerSpeedMultiplier = 1f;
                break;
        }

        // Shield-mastery upgrades deepen the damage reduction (capped at 90%).
        if (!Mathf.Approximately(strength, 1f) && damageTakenMultiplier < 1f)
        {
            float reduction = Mathf.Min(0.9f, (1f - damageTakenMultiplier) * strength);
            damageTakenMultiplier = 1f - reduction;
        }

        RefreshVisual();
        ApplyBearerSlow();
    }

    private void Update()
    {
        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            Destroy(this);
            return;
        }

        // Fire cloak scorches whatever presses in close.
        if (fireAuraDamagePerSecond > 0f)
        {
            auraTick -= Time.deltaTime;
            if (auraTick <= 0f)
            {
                auraTick = 0.5f;
                foreach (Collider hit in Physics.OverlapSphere(transform.position, fireAuraRadius))
                {
                    Unit other = hit.GetComponentInParent<Unit>();
                    if (other == null || !FactionUtility.AreHostile(gameObject, other.gameObject)) continue;
                    other.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(fireAuraDamagePerSecond * 0.5f)));
                }
            }
        }
    }

    public int ModifyIncomingDamage(int damage)
    {
        // Water: soak into the ice first.
        if (absorbPool > 0f)
        {
            float soaked = Mathf.Min(absorbPool, damage);
            absorbPool -= soaked;
            damage -= Mathf.RoundToInt(soaked);
            if (absorbPool <= 0f) remaining = Mathf.Min(remaining, 0.1f); // ice shatters
            if (damage <= 0) return 0;
        }
        return Mathf.Max(0, Mathf.RoundToInt(damage * damageTakenMultiplier));
    }

    // ------------------------------------------------------------------

    private void ApplyBearerSlow()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent == null) return;

        if (bearerSpeedMultiplier < 1f && !slowed)
        {
            originalSpeed = agent.speed;
            agent.speed = originalSpeed * bearerSpeedMultiplier;
            slowed = true;
        }
        else if (bearerSpeedMultiplier >= 1f && slowed)
        {
            agent.speed = originalSpeed;
            slowed = false;
        }
    }

    private void RefreshVisual()
    {
        if (visual == null)
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GreyboxMaterial.Harmonize(visual); // URP-safe material for runtime primitives
            visual.name = "ShieldVisual";
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = Vector3.up * 1f;
            visual.transform.localScale = Vector3.one * 3f;
        }

        Color color = NationInfo.ThemeColor(element);
        color.a = 0.28f;
        Renderer renderer = visual.GetComponent<Renderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        renderer.SetPropertyBlock(block);
    }

    private void OnDestroy()
    {
        if (slowed && agent != null) agent.speed = originalSpeed;
        if (visual != null) Destroy(visual);
    }
}
