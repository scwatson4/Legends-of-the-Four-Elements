using UnityEngine;

/// <summary>
/// Mountainside construction rules. Most nations need flat ground; the Air
/// Nomads build temples, sanctuaries and moorings ON the mountains
/// themselves - out of reach of any army that cannot fly (steep rock is off
/// the NavMesh, so ground units simply cannot path there; only flyers,
/// ranged attackers and enemy air power threaten a perch).
/// BuildingPlacer consults this class while the ghost is dragged around.
/// </summary>
public static class MountainPlacement
{
    /// <summary>Ground steeper than this is unbuildable for normal buildings.</summary>
    public const float MaxFlatSlopeDegrees = 22f;

    public static float SlopeAngle(Vector3 surfaceNormal)
    {
        return Vector3.Angle(surfaceNormal, Vector3.up);
    }

    /// <summary>True when the surveyed point is steep mountainside.</summary>
    public static bool IsMountainside(Vector3 surfaceNormal)
    {
        return SlopeAngle(surfaceNormal) > MaxFlatSlopeDegrees;
    }

    /// <summary>Can this roster entry be raised on a mountainside? Air Nomad
    /// buildings always can (their whole architecture assumes it); any other
    /// nation's building can opt in via BuildingEntry.mountainSite.</summary>
    public static bool EntryAllowsMountains(NationData.BuildingEntry entry, NationData nation)
    {
        if (entry != null && entry.mountainSite) return true;
        return nation != null && nation.nation == Nation.Air;
    }
}

/// <summary>
/// Marks (and dresses) a building that was raised on a mountainside. Air
/// Nomad architecture CHANGES when it perches: if the prefab carries a
/// disabled child named "MountainModel", it is swapped in for the flat-ground
/// "Model" child (the Greybox Protocol's art-swap point). Prefabs without
/// one get a generated greybox perch instead - an anchored platform with
/// cliff struts - so the design reads differently at a glance either way.
/// </summary>
public class MountainPerch : MonoBehaviour
{
    /// <summary>The slope normal where the building was anchored.</summary>
    public Vector3 surfaceNormal = Vector3.up;

    public static bool IsPerched(GameObject building)
    {
        return building != null && building.GetComponentInChildren<MountainPerch>() != null;
    }

    /// <summary>Call right after instantiating a building on a mountainside.</summary>
    public static void Apply(GameObject building, Vector3 normal)
    {
        if (building == null || building.GetComponent<MountainPerch>() != null) return;

        MountainPerch perch = building.AddComponent<MountainPerch>();
        perch.surfaceNormal = normal;

        // Art swap: enable the dedicated mountainside design if the prefab
        // ships one (a child named "MountainModel", disabled by default).
        Transform mountainModel = FindChild(building.transform, "MountainModel");
        if (mountainModel != null)
        {
            mountainModel.gameObject.SetActive(true);
            Transform groundModel = FindChild(building.transform, "Model");
            if (groundModel != null) groundModel.gameObject.SetActive(false);
            return;
        }

        // Greybox fallback: a landing platform + struts anchored into the rock.
        perch.BuildGreyboxPerch(normal);
    }

    private void BuildGreyboxPerch(Vector3 normal)
    {
        float footprint = 4f;
        Collider collider = GetComponentInChildren<Collider>();
        if (collider != null)
        {
            Vector3 size = collider.bounds.size;
            footprint = Mathf.Max(3f, Mathf.Max(size.x, size.z) * 1.2f);
        }

        // The platform the building stands on.
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        GreyboxMaterial.Harmonize(platform); // URP-safe material for runtime primitives
        platform.name = "PerchPlatform";
        Object.Destroy(platform.GetComponent<Collider>());
        platform.transform.SetParent(transform, false);
        platform.transform.localPosition = new Vector3(0f, -0.15f, 0f);
        platform.transform.localScale = new Vector3(footprint, 0.15f, footprint);
        Tint(platform, new Color(0.55f, 0.5f, 0.42f)); // weathered timber

        // Struts angling down-slope into the mountain face.
        Vector3 downSlope = Vector3.ProjectOnPlane(Vector3.down, normal).normalized;
        if (downSlope.sqrMagnitude < 0.01f) downSlope = Vector3.down;
        for (int i = 0; i < 3; i++)
        {
            float angle = (i - 1) * 40f;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * downSlope;

            GameObject strut = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            GreyboxMaterial.Harmonize(strut); // URP-safe material for runtime primitives
            strut.name = "PerchStrut";
            Object.Destroy(strut.GetComponent<Collider>());
            strut.transform.SetParent(transform, false);
            strut.transform.localPosition =
                direction * (footprint * 0.35f) + new Vector3(0f, -1.6f, 0f);
            strut.transform.localRotation = Quaternion.FromToRotation(Vector3.up, -direction + Vector3.up);
            strut.transform.localScale = new Vector3(0.25f, 1.8f, 0.25f);
            Tint(strut, new Color(0.45f, 0.4f, 0.34f));
        }
    }

    private static void Tint(GameObject go, Color color)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        renderer.SetPropertyBlock(block);
    }

    private static Transform FindChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name) return child;
        }
        return null;
    }
}
