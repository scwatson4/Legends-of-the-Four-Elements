using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grid-based fog of war for the local player. Three states per cell:
///   Hidden   - never seen: pitch dark, nothing shown
///   Explored - seen before: dimmed, terrain remembered, no live enemies
///   Visible  - currently in sight range of your units/buildings
///
/// Sight radius comes from each unit's VisionSource (flying bison see far;
/// the "sensing" upgrades raise it), falling back to defaultSightRange.
/// Enemy and neutral objects have their renderers hidden while outside your
/// vision. An overlay quad above the map darkens hidden/explored areas, and
/// MinimapController reads this grid to draw the minimap.
///
/// Add ONE to each level scene. Purely local/visual, so it works in
/// multiplayer as-is (each client fogs its own view).
/// </summary>
public class FogOfWar : MonoBehaviour
{
    public static FogOfWar Instance { get; private set; }

    public enum CellState : byte { Hidden = 0, Explored = 1, Visible = 2 }

    [Header("Area (centered on this object)")]
    public Vector2 mapSize = new Vector2(200f, 200f);
    public int gridResolution = 128;

    [Header("Vision")]
    public float defaultSightRange = 15f;
    public float updateInterval = 0.2f;
    [Tooltip("Turn off to disable fog entirely (everything visible).")]
    public bool fogEnabled = true;

    [Header("Overlay")]
    public bool showOverlay = true;
    [Tooltip("Assign a URP Unlit/Transparent material; falls back to Sprites/Default.")]
    public Material overlayMaterial;
    public float overlayHeight = 30f;
    public Color hiddenColor = new Color(0f, 0f, 0f, 0.95f);
    public Color exploredColor = new Color(0f, 0f, 0f, 0.55f);

    private CellState[] cells;
    private Texture2D fogTexture;
    private Color32[] pixels;
    private GameObject overlayQuad;
    private float tickTimer;

    private readonly Dictionary<GameObject, Renderer[]> rendererCache = new Dictionary<GameObject, Renderer[]>();
    private readonly Dictionary<GameObject, Canvas[]> canvasCache = new Dictionary<GameObject, Canvas[]>();

    // ------------------------------------------------------------------
    // Public queries (safe when no fog exists in the scene)
    // ------------------------------------------------------------------

    public static bool IsVisibleAt(Vector3 worldPosition)
    {
        if (Instance == null || !Instance.fogEnabled) return true;
        return Instance.GetState(worldPosition) == CellState.Visible;
    }

    public static bool IsExploredAt(Vector3 worldPosition)
    {
        if (Instance == null || !Instance.fogEnabled) return true;
        return Instance.GetState(worldPosition) != CellState.Hidden;
    }

    public CellState GetState(Vector3 worldPosition)
    {
        int index = CellIndex(worldPosition);
        return index >= 0 ? cells[index] : CellState.Hidden;
    }

    // ------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        cells = new CellState[gridResolution * gridResolution];
        fogTexture = new Texture2D(gridResolution, gridResolution, TextureFormat.RGBA32, false);
        fogTexture.wrapMode = TextureWrapMode.Clamp;
        pixels = new Color32[gridResolution * gridResolution];

        if (showOverlay) CreateOverlayQuad();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void CreateOverlayQuad()
    {
        overlayQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        overlayQuad.name = "FogOverlay";
        Destroy(overlayQuad.GetComponent<Collider>());
        overlayQuad.transform.SetParent(transform, false);
        overlayQuad.transform.localPosition = Vector3.up * overlayHeight;
        overlayQuad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        overlayQuad.transform.localScale = new Vector3(mapSize.x, mapSize.y, 1f);

        Material material = overlayMaterial;
        if (material == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            material = new Material(shader);
        }
        material.mainTexture = fogTexture;
        overlayQuad.GetComponent<MeshRenderer>().material = material;
        overlayQuad.GetComponent<MeshRenderer>().shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void Update()
    {
        if (!fogEnabled)
        {
            if (overlayQuad != null && overlayQuad.activeSelf) overlayQuad.SetActive(false);
            return;
        }
        if (overlayQuad != null && !overlayQuad.activeSelf) overlayQuad.SetActive(true);

        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f) return;
        tickTimer = updateInterval;

        Tick();
    }

    private void Tick()
    {
        // Visible decays to Explored; Explored is remembered forever.
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == CellState.Visible) cells[i] = CellState.Explored;
        }

        // Stamp vision from everything the local player owns.
        foreach (GameObject go in EnumerateLocalObjects())
        {
            float sight = defaultSightRange;
            VisionSource vision = go.GetComponent<VisionSource>();
            if (vision != null) sight = vision.sightRange;

            RevealCircle(go.transform.position, sight);
        }

        UpdateHiddenObjects();
        UpdateTexture();
    }

    private IEnumerable<GameObject> EnumerateLocalObjects()
    {
        if (UnitSelectionManager.Instance != null)
        {
            foreach (GameObject go in UnitSelectionManager.Instance.allUnitsList)
            {
                if (go != null && FactionUtility.IsLocallyControlled(go)) yield return go;
            }
        }

        foreach (CommandCenter cc in FindObjectsByType<CommandCenter>(FindObjectsSortMode.None))
        {
            if (FactionUtility.IsLocallyControlled(cc.gameObject)) yield return cc.gameObject;
        }
        foreach (Structure structure in FindObjectsByType<Structure>(FindObjectsSortMode.None))
        {
            if (FactionUtility.IsLocallyControlled(structure.gameObject)) yield return structure.gameObject;
        }
    }

    public void RevealCircle(Vector3 worldPosition, float radius)
    {
        float cellSize = mapSize.x / gridResolution;
        int cellRadius = Mathf.CeilToInt(radius / cellSize);

        int centerX, centerY;
        if (!WorldToCell(worldPosition, out centerX, out centerY)) return;

        int radiusSquared = cellRadius * cellRadius;
        for (int y = -cellRadius; y <= cellRadius; y++)
        {
            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                if (x * x + y * y > radiusSquared) continue;
                int cx = centerX + x;
                int cy = centerY + y;
                if (cx < 0 || cy < 0 || cx >= gridResolution || cy >= gridResolution) continue;
                cells[cy * gridResolution + cx] = CellState.Visible;
            }
        }
    }

    // ------------------------------------------------------------------
    // Hiding enemies/neutrals outside vision
    // ------------------------------------------------------------------

    private void UpdateHiddenObjects()
    {
        if (UnitSelectionManager.Instance != null)
        {
            foreach (GameObject go in UnitSelectionManager.Instance.allUnitsList)
            {
                if (go == null || FactionUtility.IsLocallyControlled(go)) continue;
                SetObjectVisible(go, IsVisibleAt(go.transform.position));
            }
        }

        // Buildings stay visible once explored (you remember where bases are).
        foreach (Structure structure in FindObjectsByType<Structure>(FindObjectsSortMode.None))
        {
            if (FactionUtility.IsLocallyControlled(structure.gameObject)) continue;
            SetObjectVisible(structure.gameObject, IsExploredAt(structure.transform.position));
        }
        foreach (CommandCenter cc in FindObjectsByType<CommandCenter>(FindObjectsSortMode.None))
        {
            if (FactionUtility.IsLocallyControlled(cc.gameObject)) continue;
            SetObjectVisible(cc.gameObject, IsExploredAt(cc.transform.position));
        }
    }

    private void SetObjectVisible(GameObject go, bool visible)
    {
        Renderer[] renderers;
        if (!rendererCache.TryGetValue(go, out renderers))
        {
            renderers = go.GetComponentsInChildren<Renderer>(true);
            rendererCache[go] = renderers;
        }
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null && renderer.enabled != visible) renderer.enabled = visible;
        }

        Canvas[] canvases;
        if (!canvasCache.TryGetValue(go, out canvases))
        {
            canvases = go.GetComponentsInChildren<Canvas>(true);
            canvasCache[go] = canvases;
        }
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && canvas.enabled != visible) canvas.enabled = visible;
        }
    }

    // ------------------------------------------------------------------
    // Texture / coordinates
    // ------------------------------------------------------------------

    private void UpdateTexture()
    {
        Color32 visible = new Color32(0, 0, 0, 0);
        Color32 explored = exploredColor;
        Color32 hidden = hiddenColor;

        for (int i = 0; i < cells.Length; i++)
        {
            pixels[i] = cells[i] == CellState.Visible ? visible
                : cells[i] == CellState.Explored ? explored
                : hidden;
        }
        fogTexture.SetPixels32(pixels);
        fogTexture.Apply(false);
    }

    public bool WorldToCell(Vector3 worldPosition, out int x, out int y)
    {
        Vector3 local = worldPosition - transform.position;
        float u = local.x / mapSize.x + 0.5f;
        float v = local.z / mapSize.y + 0.5f;
        x = Mathf.FloorToInt(u * gridResolution);
        y = Mathf.FloorToInt(v * gridResolution);
        return x >= 0 && y >= 0 && x < gridResolution && y < gridResolution;
    }

    private int CellIndex(Vector3 worldPosition)
    {
        int x, y;
        if (!WorldToCell(worldPosition, out x, out y)) return -1;
        return y * gridResolution + x;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(transform.position, new Vector3(mapSize.x, 1f, mapSize.y));
    }
}
