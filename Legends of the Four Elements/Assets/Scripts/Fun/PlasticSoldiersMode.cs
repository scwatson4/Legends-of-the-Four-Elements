using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PLASTIC SOLDIERS - the Army Men RTS homage mode. Every unit and building
/// renders as glossy, single-color toy plastic in its kingdom's color:
/// little green earthbenders, little red firebenders, purple dark spirit
/// toys. Pure visuals; gameplay untouched.
///
/// Toggle with F9 in-game, or from the menu (GameSetup.PlasticSoldiersMode /
/// NationSelectController.SetPlasticMode). Toggling off restores every
/// original material. Self-bootstraps; zero wiring.
/// </summary>
public class PlasticSoldiersMode : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.F9;
    public float sweepInterval = 1f;

    [Range(0f, 1f)] public float smoothness = 0.92f;
    [Range(0f, 1f)] public float metallic = 0.05f;

    private readonly Dictionary<Renderer, Material[]> originals = new Dictionary<Renderer, Material[]>();
    private readonly Dictionary<int, Material> plasticByColor = new Dictionary<int, Material>();
    private float sweepTimer;
    private bool applied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<PlasticSoldiersMode>() != null) return;
        GameObject go = new GameObject("PlasticSoldiersMode");
        go.AddComponent<PlasticSoldiersMode>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            GameSetup.PlasticSoldiersMode = !GameSetup.PlasticSoldiersMode;
            Debug.Log(GameSetup.PlasticSoldiersMode
                ? "PLASTIC SOLDIERS MODE: everyone to the toybox!"
                : "Plastic Soldiers mode off - back to flesh and stone.");
        }

        if (GameSetup.PlasticSoldiersMode)
        {
            applied = true;
            sweepTimer -= Time.unscaledDeltaTime;
            if (sweepTimer <= 0f)
            {
                sweepTimer = sweepInterval;
                Sweep();
            }
        }
        else if (applied)
        {
            applied = false;
            RestoreAll();
        }
    }

    // ------------------------------------------------------------------

    private void Sweep()
    {
        // Units (includes spirits and villagers - they're all toys now).
        if (UnitSelectionManager.Instance != null)
        {
            foreach (GameObject go in UnitSelectionManager.Instance.allUnitsList)
            {
                if (go != null) Plasticize(go);
            }
        }

        // Buildings and bases join the toybox too.
        foreach (Structure structure in FindObjectsByType<Structure>(FindObjectsSortMode.None))
        {
            Plasticize(structure.gameObject);
        }
        foreach (CommandCenter cc in FindObjectsByType<CommandCenter>(FindObjectsSortMode.None))
        {
            Plasticize(cc.gameObject);
        }
    }

    private void Plasticize(GameObject go)
    {
        Material plastic = GetPlasticMaterial(ResolveColor(go));

        foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
        {
            // Leave VFX alone - flames and water still look like magic.
            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
            {
                continue;
            }
            if (originals.ContainsKey(renderer)) continue; // already a toy

            originals[renderer] = renderer.sharedMaterials;

            Material[] replacement = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < replacement.Length; i++) replacement[i] = plastic;
            renderer.sharedMaterials = replacement;
        }
    }

    private void RestoreAll()
    {
        foreach (KeyValuePair<Renderer, Material[]> pair in originals)
        {
            if (pair.Key != null) pair.Key.sharedMaterials = pair.Value;
        }
        originals.Clear();
    }

    private static Color ResolveColor(GameObject go)
    {
        int factionId = FactionUtility.GetFactionId(go);

        if (factionId == FactionManager.HostileSpiritsFaction) return new Color(0.5f, 0.2f, 0.65f);
        if (factionId == FactionManager.NoFaction) return new Color(0.75f, 0.75f, 0.7f);

        Faction faction = FactionManager.Get(factionId);
        return faction != null ? NationInfo.ThemeColor(faction.nation) : Color.gray;
    }

    private Material GetPlasticMaterial(Color color)
    {
        // Quantize so each faction shares one material (cheap batching).
        int key = ((int)(color.r * 255) << 16) | ((int)(color.g * 255) << 8) | (int)(color.b * 255);
        if (plasticByColor.TryGetValue(key, out Material cached) && cached != null) return cached;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Sprites/Default"); // fallback, less shiny

        Material material = new Material(shader) { name = "Plastic_" + key };
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);

        plasticByColor[key] = material;
        return material;
    }
}
