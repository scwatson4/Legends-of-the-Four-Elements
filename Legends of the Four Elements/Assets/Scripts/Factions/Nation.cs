using UnityEngine;

/// <summary>
/// The four playable elemental nations. Values are serialized in scenes and
/// prefabs, so never reorder them - only append.
/// </summary>
public enum Nation
{
    Air = 0,
    Water = 1,
    Earth = 2,
    Fire = 3,
    None = 4
}

public static class NationInfo
{
    public static string DisplayName(Nation nation)
    {
        switch (nation)
        {
            case Nation.Air: return "Air Nomads";
            case Nation.Water: return "Water Tribe";
            case Nation.Earth: return "Earth Kingdom";
            case Nation.Fire: return "Fire Nation";
            default: return "Unaligned";
        }
    }

    public static Color ThemeColor(Nation nation)
    {
        switch (nation)
        {
            case Nation.Air: return new Color(1f, 0.85f, 0.4f);   // saffron yellow
            case Nation.Water: return new Color(0.3f, 0.55f, 1f);  // ocean blue
            case Nation.Earth: return new Color(0.35f, 0.7f, 0.3f); // kingdom green
            case Nation.Fire: return new Color(0.9f, 0.25f, 0.15f); // flame red
            default: return Color.gray;
        }
    }
}
