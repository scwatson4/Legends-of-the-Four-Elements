using UnityEngine;

/// <summary>
/// Remembers a building's original stats so Building-category upgrades can
/// be re-applied cleanly (never compounding). Covers Structure health,
/// DefenseTower damage / fire rate / range, and VisionSource sight.
/// Added automatically by UpgradeManager; no need to put it on prefabs.
/// </summary>
public class StructureBaseStats : MonoBehaviour
{
    private float baseMaxHealth;
    private int baseTowerDamage;
    private float baseFireInterval;
    private float baseRange;
    private float baseSight;
    private bool captured;

    private void Capture()
    {
        if (captured) return;

        Structure structure = GetComponent<Structure>();
        DefenseTower tower = GetComponent<DefenseTower>();
        VisionSource vision = GetComponent<VisionSource>();

        baseMaxHealth = structure != null ? structure.maxHealth : 0f;
        baseTowerDamage = tower != null ? tower.damage : 0;
        baseFireInterval = tower != null ? tower.fireInterval : 0f;
        baseRange = tower != null ? tower.range : 0f;
        baseSight = vision != null ? vision.sightRange : 0f;
        captured = true;
    }

    public void ApplyMultipliers(float damageMult, float healthMult, float rateMult, float rangeMult)
    {
        Capture();

        Structure structure = GetComponent<Structure>();
        if (structure != null && baseMaxHealth > 0f)
        {
            structure.SetMaxHealth(baseMaxHealth * healthMult);
        }

        DefenseTower tower = GetComponent<DefenseTower>();
        if (tower != null)
        {
            if (baseTowerDamage > 0)
            {
                tower.damage = Mathf.RoundToInt(baseTowerDamage * damageMult);
            }
            if (baseFireInterval > 0f)
            {
                tower.fireInterval = baseFireInterval / Mathf.Max(0.1f, rateMult); // faster firing
            }
            if (baseRange > 0f)
            {
                tower.range = baseRange * rangeMult;
            }
        }

        VisionSource vision = GetComponent<VisionSource>();
        if (vision != null && baseSight > 0f)
        {
            vision.sightRange = baseSight * rangeMult;
        }
    }
}
