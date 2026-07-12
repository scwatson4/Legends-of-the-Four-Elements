using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A chapter boss: a past Avatar dragged back through the rupture and
/// corrupted by the First Shadow. Massive stats, and it shifts its bent
/// element as it takes damage (phases at 75/50/25% health), getting angrier
/// each time. Belongs to the HostileSpirits faction, so it fights everyone.
/// On death, CampaignManager redeems it into the player's arsenal.
/// Added at runtime by CampaignManager to the nation's Avatar prefab.
/// </summary>
[RequireComponent(typeof(Unit))]
public class CorruptedAvatar : MonoBehaviour
{
    public Nation avatarNation = Nation.Air;
    public string bossName = "Corrupted Avatar";

    [Header("Boss Scaling (applied once at spawn)")]
    public float healthMultiplier = 8f;
    public float damageMultiplier = 2.5f;
    public float sizeMultiplier = 1.6f;
    public float phaseDamageBonus = 0.5f; // extra damage each phase

    private Unit unit;
    private AttackController attackController;
    private AvatarUnit avatarUnit;
    private int phase; // 0..3
    private int baseDamage;
    private bool reported;

    public void Configure(Nation nation, string name)
    {
        avatarNation = nation;
        bossName = name;
    }

    private void Start()
    {
        unit = GetComponent<Unit>();
        attackController = GetComponent<AttackController>();
        avatarUnit = GetComponent<AvatarUnit>();

        // The shadow's champion fights for no nation.
        FactionUtility.SetFaction(gameObject, FactionManager.HostileSpiritsFaction);
        unit.category = UnitCategory.Avatar;
        unit.killBounty = 200;

        unit.SetMaxHealth(unit.maxUnitHealth * healthMultiplier);
        transform.localScale *= sizeMultiplier;

        if (attackController != null)
        {
            baseDamage = Mathf.RoundToInt(attackController.unitDamage * damageMultiplier);
            attackController.unitDamage = baseDamage;
        }

        // Bosses hunt relentlessly.
        EnemyAI brain = GetComponent<EnemyAI>();
        if (brain == null) brain = gameObject.AddComponent<EnemyAI>();
        brain.enabled = true;
        brain.chaseCommandCenters = true;

        if (avatarUnit != null)
        {
            avatarUnit.canEnergyBend = false; // the shadow cannot redeem, only take
            avatarUnit.ApplyElement(avatarNation);
        }

        unit.HealthChanged += OnHealthChanged;
        MinimapPOI poi = MinimapPOI.Ensure(gameObject, MinimapPOI.POIType.Custom);
        poi.colorOverride = Color.red;
    }

    private void OnHealthChanged(float current, float max)
    {
        if (current <= 0f)
        {
            ReportDefeat();
            return;
        }

        // Phase up at 75/50/25%: shift element, hit harder.
        float fraction = max > 0f ? current / max : 0f;
        int wantedPhase = fraction > 0.75f ? 0 : fraction > 0.5f ? 1 : fraction > 0.25f ? 2 : 3;
        while (phase < wantedPhase)
        {
            phase++;
            if (avatarUnit != null) avatarUnit.CycleElement();
            if (attackController != null)
            {
                attackController.unitDamage = Mathf.RoundToInt(baseDamage * (1f + phase * phaseDamageBonus));
            }
            GameFeel.Shake(0.35f + phase * 0.1f, 0.7f);
            Debug.Log($"{bossName} enters phase {phase + 1}!");
        }
    }

    private void OnDestroy()
    {
        if (unit != null) unit.HealthChanged -= OnHealthChanged;
        if (unit != null && unit.CurrentHealth <= 0f) ReportDefeat();
    }

    private void ReportDefeat()
    {
        if (reported) return;
        reported = true;

        if (CampaignManager.Instance != null)
        {
            CampaignManager.Instance.OnCorruptedAvatarDefeated(this);
        }
    }
}
