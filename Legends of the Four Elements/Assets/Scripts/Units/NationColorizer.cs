using UnityEngine;

/// <summary>
/// Greybox helper: tints this object's renderers to its OWNER's kingdom
/// color at runtime - Fire red, Water blue, Earth green, Air saffron-yellow
/// (the same NationInfo.ThemeColor palette the UI uses). Neutrals go gray,
/// dark spirits purple. Because it reads ownership, one placeholder prefab
/// reads correctly for every faction, and a tamed spirit recolors the
/// moment it joins you.
///
/// Add to placeholder prefabs (the Model child or the root - it tints all
/// child renderers). Uses MaterialPropertyBlocks, so no material assets are
/// touched. When real art arrives, remove the component or lower
/// tintStrength for a subtle team-color accent instead.
/// </summary>
public class NationColorizer : MonoBehaviour
{
    [Range(0f, 1f)]
    [Tooltip("1 = full flat kingdom color (greybox). ~0.35 = subtle team accent on real art.")]
    public float tintStrength = 1f;

    public Color neutralColor = new Color(0.6f, 0.6f, 0.6f);
    public Color darkSpiritColor = new Color(0.45f, 0.15f, 0.6f);

    private void Start()
    {
        Refresh();
    }

    /// <summary>Re-tint after ownership changes (taming, scripted capture).</summary>
    public void Refresh()
    {
        Color color = ResolveColor();
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            renderer.GetPropertyBlock(block);
            Color blended = Color.Lerp(Color.white, color, tintStrength);
            block.SetColor("_BaseColor", blended); // URP Lit/Unlit
            block.SetColor("_Color", blended);     // legacy shaders
            renderer.SetPropertyBlock(block);
        }
    }

    private Color ResolveColor()
    {
        int factionId = FactionUtility.GetFactionId(gameObject);

        if (factionId == FactionManager.HostileSpiritsFaction) return darkSpiritColor;
        if (factionId == FactionManager.NoFaction) return neutralColor;

        Faction faction = FactionManager.Get(factionId);
        return faction != null ? NationInfo.ThemeColor(faction.nation) : neutralColor;
    }
}
