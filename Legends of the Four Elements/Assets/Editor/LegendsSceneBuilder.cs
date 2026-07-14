using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds a COMPLETE, PLAYABLE skirmish scene from scratch - the thing that
/// normally takes an hour of hand-wiring in the editor. Run:
///
///     Legends ► Build Playable Skirmish Scene
///
/// It assembles the ground, camera rig, lighting, EventSystem, the match
/// rig (MatchManager + selection + placement + fog + sound), a self-building
/// GameHUD (train/build/upgrade panel, no manual wiring), two StartLocations,
/// and a scatter of resource nodes / a village / spirit portals / biome
/// zones pulled from the greybox prefabs. Saves to
/// Assets/Scenes/Skirmish_Generated.unity and adds it to Build Settings.
///
/// PREREQUISITE: run **Legends ► Bootstrap ALL** first so the NationDatabase
/// and greybox prefabs exist. The scene still builds without them (you'll
/// just get an empty map and a console note).
///
/// After building: bake the NavMesh (Window ► AI ► Navigation, or a
/// NavMeshSurface) and press Play. The scene is also playable directly -
/// SkirmishAutoConfig sets up a free-for-all when launched outside the menu.
/// </summary>
public static class LegendsSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Skirmish_Generated.unity";
    private const string GreyboxFolder = "Assets/Prefabs/Greybox";

    [MenuItem("Legends/Build Playable Skirmish Scene", priority = 40)]
    public static void BuildScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildGround();
        BuildLighting();
        BuildCameraRig();
        BuildMatchRig();
        BuildStartLocations();
        ScatterMapObjects();

        EnsureFolder("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);

        Debug.Log("[LegendsSceneBuilder] Built " + ScenePath +
                  ". Bake the NavMesh, then press Play. (Run Legends ► Bootstrap ALL first if you haven't.)");
        EditorUtility.DisplayDialog("Legends",
            "Built a playable skirmish scene at:\n" + ScenePath +
            "\n\nNext: bake the NavMesh (Window ► AI ► Navigation), then press Play.\n\n" +
            "If units/buildings are missing, run Legends ► Bootstrap ALL and rebuild.",
            "Got it");
    }

    // ------------------------------------------------------------------

    private static void BuildGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(24f, 1f, 24f); // 240 x 240
        SetLayer(ground, "Ground");
        Tint(ground, new Color(0.34f, 0.4f, 0.3f));

        // A couple of greybox hills + a steep mountain so slope rules and Air
        // perches are testable out of the box.
        MakeMound("Hill_A", new Vector3(40f, 0f, 30f), new Vector3(18f, 5f, 18f), new Color(0.4f, 0.44f, 0.32f));
        MakeMound("Hill_B", new Vector3(-35f, 0f, -20f), new Vector3(22f, 4f, 16f), new Color(0.4f, 0.44f, 0.32f));
        MakeMound("Mountain", new Vector3(0f, 0f, 55f), new Vector3(26f, 30f, 26f), new Color(0.5f, 0.5f, 0.52f));
    }

    private static void MakeMound(string name, Vector3 pos, Vector3 scale, Color color)
    {
        // A squashed sphere reads as a hill and gives a real sloped surface
        // (steep enough on the "Mountain" to be unwalkable + Air-perch-able).
        GameObject mound = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mound.name = name;
        mound.transform.position = new Vector3(pos.x, scale.y * -0.35f, pos.z);
        mound.transform.localScale = scale;
        SetLayer(mound, "Ground");
        Tint(mound, color);
    }

    private static void BuildLighting()
    {
        GameObject lightGo = new GameObject("Directional Light");
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.86f);
        light.intensity = 1.1f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientLight = new Color(0.5f, 0.52f, 0.55f);
    }

    private static void BuildCameraRig()
    {
        GameObject rig = new GameObject("CameraRig");
        rig.transform.position = new Vector3(-40f, 0f, -40f);
        RTSCameraController controller = rig.AddComponent<RTSCameraController>();

        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(rig.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 45f, -35f);
        camGo.transform.localRotation = Quaternion.Euler(50f, 0f, 0f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        camGo.AddComponent<AudioListener>();

        // Wire the private serialized fields the controller needs.
        SerializedObject so = new SerializedObject(controller);
        SetProp(so, "cameraTransform", camGo.transform);
        SetBool(so, "moveWithKeyboard", true);
        SetBool(so, "moveWithMouseDrag", true);
        SetBool(so, "moveWithEdgeScrolling", false);
        SetBool(so, "playIntro", false);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildMatchRig()
    {
        GameObject rig = new GameObject("MatchController");

        SkirmishAutoConfig autoConfig = rig.AddComponent<SkirmishAutoConfig>();
        autoConfig.playerNation = Nation.Air;
        autoConfig.aiOpponents = 1;

        MatchManager match = rig.AddComponent<MatchManager>();
        match.nationDatabase = LoadDatabase();
        match.spawnBasesAtStartLocations = true;
        match.startingUnitsPerFaction = 3;

        // Selection with the standard layer masks.
        UnitSelectionManager selection = rig.AddComponent<UnitSelectionManager>();
        selection.clickable = LayerOnly("Clickable");
        selection.ground = LayerOnly("Ground");
        selection.attackable = LayerOnly("Attackable");

        // Building placement onto the ground layer.
        BuildingPlacer placer = rig.AddComponent<BuildingPlacer>();
        placer.groundMask = LayerOnly("Ground");

        // Fog of war sized to the ground.
        FogOfWar fog = rig.AddComponent<FogOfWar>();
        fog.mapSize = new Vector2(240f, 240f);

        rig.AddComponent<SoundManager>();
        // (PopulationManager is a static system - no component needed.)

        // The self-building control panel (also creates PlayerResources,
        // PopulationHUD, UpgradePurchaser and an EventSystem).
        GameObject hudGo = new GameObject("GameHUD");
        hudGo.AddComponent<GameHUD>();
    }

    private static void BuildStartLocations()
    {
        MakeStartLocation("StartLocation_Player", 0, new Vector3(-70f, 0f, -70f));
        MakeStartLocation("StartLocation_AI", 1, new Vector3(70f, 0f, 70f));
    }

    private static void MakeStartLocation(string name, int index, Vector3 position)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;
        go.AddComponent<StartLocation>().index = index;
    }

    private static void ScatterMapObjects()
    {
        // Resource nodes near each base + contested middle.
        PlacePrefab("SpiritGroveNode", new Vector3(-55f, 0f, -55f));
        PlacePrefab("CrystalDepositNode", new Vector3(-60f, 0f, -40f));
        PlacePrefab("CoalSeamNode", new Vector3(55f, 0f, 55f));
        PlacePrefab("FishShoalNode", new Vector3(0f, 0f, -15f));
        PlacePrefab("CrystalDepositNode", new Vector3(15f, 0f, 5f));

        // A neutral village to befriend, and two linked portals to test travel.
        PlacePrefab("Village", new Vector3(-10f, 0f, 25f));
        PlacePrefab("SpiritPortal", new Vector3(-30f, 0f, 40f));
        PlacePrefab("SpiritPortal", new Vector3(35f, 0f, -30f));

        // Biome zones so climate buffs/freezes are live.
        MakeBiome("Volcanic", BiomeZone.ClimateType.Volcanic, new Vector3(60f, 0f, -50f), new Color(0.7f, 0.3f, 0.2f));
        MakeBiome("Glacier", BiomeZone.ClimateType.Glacier, new Vector3(-60f, 0f, 60f), new Color(0.6f, 0.8f, 0.95f));
        MakeBiome("RiverLands", BiomeZone.ClimateType.RiverLands, new Vector3(0f, 0f, -15f), new Color(0.4f, 0.6f, 0.85f));
        MakeBiome("WindyPeaks", BiomeZone.ClimateType.WindyPeaks, new Vector3(0f, 0f, 55f), new Color(0.8f, 0.85f, 0.6f));
    }

    private static void MakeBiome(string name, BiomeZone.ClimateType climate, Vector3 position, Color color)
    {
        GameObject go = new GameObject("Biome_" + name);
        go.transform.position = position;
        BiomeZone zone = go.AddComponent<BiomeZone>();
        zone.climate = climate;

        // A faint translucent disc so the zone is visible in-editor.
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "ZoneMarker";
        Object.DestroyImmediate(disc.GetComponent<Collider>());
        disc.transform.SetParent(go.transform, false);
        disc.transform.localScale = new Vector3(30f, 0.05f, 30f);
        Tint(disc, new Color(color.r, color.g, color.b, 0.35f));
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static GameObject PlacePrefab(string prefabName, Vector3 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{GreyboxFolder}/{prefabName}.prefab");
        if (prefab == null)
        {
            Debug.LogWarning($"[LegendsSceneBuilder] Missing greybox prefab '{prefabName}' - run Legends ► Bootstrap ALL. Skipping.");
            return null;
        }
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = position;
        return instance;
    }

    private static NationDatabase LoadDatabase()
    {
        NationDatabase db = AssetDatabase.LoadAssetAtPath<NationDatabase>("Assets/Resources/NationDatabase.asset");
        if (db == null)
        {
            Debug.LogWarning("[LegendsSceneBuilder] No NationDatabase - run Legends ► Bootstrap ALL. Bases won't spawn.");
        }
        return db;
    }

    private static LayerMask LayerOnly(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        return layer >= 0 ? (1 << layer) : 0;
    }

    private static void SetLayer(GameObject go, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0) go.layer = layer;
    }

    private static void Tint(GameObject go, Color color)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;

        EnsureFolder("Assets/Nations/Materials");
        string key = ColorUtility.ToHtmlStringRGBA(color);
        string path = $"Assets/Nations/Materials/scene_{key}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
        }
        renderer.sharedMaterial = material;
    }

    private static void SetProp(SerializedObject so, string prop, Object value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p != null) p.objectReferenceValue = value;
    }

    private static void SetBool(SerializedObject so, string prop, bool value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p != null) p.boolValue = value;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    private static void AddToBuildSettings(string scenePath)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes)
        {
            if (s.path == scenePath) return; // already present
        }
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
