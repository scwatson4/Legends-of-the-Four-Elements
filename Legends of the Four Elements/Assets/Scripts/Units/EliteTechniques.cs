using UnityEngine;

/// <summary>
/// Lightning redirection - the Iroh technique. Granted to firebenders by an
/// upgrade (UpgradeData.grantsLightningRedirect): when a lightning attack
/// (a firebender's Lightning Jolt or a Flame Turret's lightning special)
/// strikes them, they have a chance to catch it and hurl it BACK at the
/// source. Applied by UpgradeManager; don't add by hand.
/// </summary>
public class LightningRedirect : MonoBehaviour
{
    [Range(0f, 0.9f)] public float chance = 0.25f;

    /// <summary>Rolls redirection. True = the damage went back to the source
    /// instead; the caller should NOT damage the target.</summary>
    public static bool TryRedirect(Unit target, int damage, GameObject source)
    {
        if (target == null || source == null) return false;

        LightningRedirect redirect = target.GetComponent<LightningRedirect>();
        if (redirect == null || Random.value > redirect.chance) return false;

        // The lightning flows through them and back out.
        Unit sourceUnit = source.GetComponentInParent<Unit>();
        if (sourceUnit != null)
        {
            sourceUnit.TakeDamage(damage);
        }
        else
        {
            Structure sourceStructure = source.GetComponentInParent<Structure>();
            if (sourceStructure != null) sourceStructure.TakeDamage(damage);
            else
            {
                CommandCenter sourceCc = source.GetComponentInParent<CommandCenter>();
                if (sourceCc != null) sourceCc.TakeDamage(damage);
            }
        }

        Debug.DrawLine(target.transform.position, source.transform.position, Color.cyan, 0.5f);
        Debug.Log($"{target.name} REDIRECTS the lightning back at {source.name}!");
        return true;
    }
}

/// <summary>
/// Metalbending - the absolute top-tier earthbender technique (Toph's gift).
/// Granted by an upgrade (UpgradeData.grantsMetalBending): earthbenders tear
/// into machines and fortifications - bonus damage against Vehicles and
/// buildings. Applied by UpgradeManager; don't add by hand.
/// </summary>
public class MetalBending : MonoBehaviour
{
    public float vehicleDamageMultiplier = 1.6f;
    public float structureDamageMultiplier = 1.3f;
}
