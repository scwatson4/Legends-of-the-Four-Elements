using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An elemental climate area on the map. Units fighting inside it are
/// stronger or weaker depending on their element:
///
///   Volcanic     - Fire +25%, Water -20%
///   Glacier      - Water +25%, Fire -20%
///   RiverLands   - Water +15%, Fire -10%
///   WindyPeaks   - Air +25%, Earth -15%
///   StoneQuarry  - Earth +25%, Air -15%
///   SpiritWilds  - Spirits +25%, everyone else -10%
///
/// A unit's element is: the Avatar's currently-bent element, the bender's own
/// discipline, or (for animals/vehicles/workers) the element of the nation
/// that owns it - a Fire Nation tank suffers on a glacier.
/// Place by hand or let MapGenerator scatter them.
/// </summary>
public class BiomeZone : MonoBehaviour
{
    public enum ClimateType
    {
        Volcanic = 0,
        Glacier = 1,
        RiverLands = 2,
        WindyPeaks = 3,
        StoneQuarry = 4,
        SpiritWilds = 5
    }

    public ClimateType climate = ClimateType.RiverLands;
    public float radius = 25f;

    private static readonly List<BiomeZone> all = new List<BiomeZone>();

    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);

    /// <summary>Combined attack multiplier for a unit standing where it is.</summary>
    public static float GetAttackMultiplier(GameObject unitGo)
    {
        if (all.Count == 0 || unitGo == null) return 1f;

        Nation element = ResolveElement(unitGo, out bool isSpirit);
        Vector3 position = unitGo.transform.position;

        float multiplier = 1f;
        foreach (BiomeZone zone in all)
        {
            if (zone == null) continue;
            if (Vector3.Distance(position, zone.transform.position) > zone.radius) continue;
            multiplier *= zone.MultiplierFor(element, isSpirit);
        }
        return multiplier;
    }

    public float MultiplierFor(Nation element, bool isSpirit)
    {
        if (climate == ClimateType.SpiritWilds)
        {
            return isSpirit ? 1.25f : 0.9f;
        }
        if (isSpirit) return 1f;

        switch (climate)
        {
            case ClimateType.Volcanic:
                if (element == Nation.Fire) return 1.25f;
                if (element == Nation.Water) return 0.8f;
                break;
            case ClimateType.Glacier:
                if (element == Nation.Water) return 1.25f;
                if (element == Nation.Fire) return 0.8f;
                break;
            case ClimateType.RiverLands:
                if (element == Nation.Water) return 1.15f;
                if (element == Nation.Fire) return 0.9f;
                break;
            case ClimateType.WindyPeaks:
                if (element == Nation.Air) return 1.25f;
                if (element == Nation.Earth) return 0.85f;
                break;
            case ClimateType.StoneQuarry:
                if (element == Nation.Earth) return 1.25f;
                if (element == Nation.Air) return 0.85f;
                break;
        }
        return 1f;
    }

    /// <summary>Which element the unit fights with (see class summary).</summary>
    public static Nation ResolveElement(GameObject unitGo, out bool isSpirit)
    {
        isSpirit = false;

        // The Avatar bends whatever element is currently active.
        AvatarUnit avatar = unitGo.GetComponent<AvatarUnit>();
        if (avatar != null) return avatar.currentElement;

        Unit unit = unitGo.GetComponent<Unit>();
        if (unit != null)
        {
            switch (unit.unitType)
            {
                case Unit.UnitType.Airbender: return Nation.Air;
                case Unit.UnitType.Waterbender: return Nation.Water;
                case Unit.UnitType.Earthbender: return Nation.Earth;
                case Unit.UnitType.Firebender: return Nation.Fire;
                case Unit.UnitType.Spirit:
                    isSpirit = true;
                    return Nation.None;
            }
        }

        // Animals, vehicles, workers: element of the owning nation.
        Faction faction = FactionManager.Get(FactionUtility.GetFactionId(unitGo));
        return faction != null ? faction.nation : Nation.None;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = climate == ClimateType.Volcanic ? Color.red
            : climate == ClimateType.Glacier ? Color.cyan
            : climate == ClimateType.RiverLands ? Color.blue
            : climate == ClimateType.WindyPeaks ? Color.yellow
            : climate == ClimateType.StoneQuarry ? Color.green
            : Color.magenta;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
