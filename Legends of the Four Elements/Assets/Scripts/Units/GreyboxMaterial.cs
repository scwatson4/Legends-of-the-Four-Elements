using UnityEngine;

/// <summary>
/// URP safety net for RUNTIME-CREATED primitives (air scooters, tornado
/// funnels, fire rings, greybox mounts, couriers, carts, perch platforms...).
///
/// GameObject.CreatePrimitive can assign the built-in "Standard" material,
/// which the Universal Render Pipeline cannot render - the object shows up
/// MAGENTA in device builds (Quest, WebGL) even when the editor looks fine.
/// Harmonize() swaps any built-in-shader material for one shared URP Lit
/// material; MaterialPropertyBlock tints (_BaseColor/_Color) keep working.
/// Call it once on anything you build out of primitives at runtime. No-op
/// when Unity already assigned a pipeline-correct material.
/// </summary>
public static class GreyboxMaterial
{
    private static Material shared;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        shared = null;
    }

    /// <summary>Fixes every renderer under `root` (inactive ones included).</summary>
    public static void Harmonize(GameObject root)
    {
        if (root == null) return;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Material material = renderer.sharedMaterial;
            if (material == null || material.shader == null ||
                material.shader.name == "Standard" ||
                material.shader.name.StartsWith("Legacy") ||
                material.shader.name == "Hidden/InternalErrorShader")
            {
                renderer.sharedMaterial = SharedLit();
            }
        }
    }

    private static Material SharedLit()
    {
        if (shared != null) return shared;

        // The URP Lit shader ships in every URP build (all scene materials
        // use it); the Standard fallback covers non-URP projects.
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        shared = new Material(shader) { name = "GreyboxShared (runtime)" };
        return shared;
    }
}
