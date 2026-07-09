using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A point of interest shown on the minimap once the player has explored its
/// area: resource nodes, villages, spirit portals, enemy bases. Discovery is
/// permanent - once seen, it stays on your map even when fog returns.
/// ResourceNode, Village, SpiritPortal and CommandCenter add this themselves
/// at runtime; add it by hand to anything else you want mapped.
/// </summary>
public class MinimapPOI : MonoBehaviour
{
    public enum POIType
    {
        ResourceNode = 0,
        Village = 1,
        SpiritPortal = 2,
        CommandCenter = 3,
        Custom = 4
    }

    public POIType type = POIType.Custom;
    [Tooltip("Leave fully transparent to use the minimap's default color for the type.")]
    public Color colorOverride = new Color(0f, 0f, 0f, 0f);

    [HideInInspector] public bool discovered;

    public static readonly List<MinimapPOI> All = new List<MinimapPOI>();

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    /// <summary>Adds (or finds) a POI marker on a GameObject.</summary>
    public static MinimapPOI Ensure(GameObject go, POIType type)
    {
        MinimapPOI poi = go.GetComponent<MinimapPOI>();
        if (poi == null)
        {
            poi = go.AddComponent<MinimapPOI>();
            poi.type = type;
        }
        return poi;
    }
}
