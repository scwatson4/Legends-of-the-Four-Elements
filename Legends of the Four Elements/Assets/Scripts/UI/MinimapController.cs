using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws a live minimap into a RawImage from the FogOfWar grid:
///   - hidden areas are dark, explored areas dim, visible areas clear
///   - your units are green dots, visible enemies red
///   - discovered points of interest stay mapped forever: resource nodes
///     (yellow), villages (white), spirit portals (purple), enemy bases (by
///     nation color)
/// Add to the HUD canvas and assign a RawImage in the corner of the screen.
/// Works without a FogOfWar in the scene too (everything just shows).
/// </summary>
public class MinimapController : MonoBehaviour
{
    [Header("Target")]
    public RawImage minimapImage;
    public int textureSize = 256;
    public float refreshInterval = 0.4f;

    [Header("Fallback area (used only when no FogOfWar exists)")]
    public Vector2 mapSize = new Vector2(200f, 200f);
    public Vector3 mapCenter = Vector3.zero;

    [Header("Colors")]
    public Color hiddenColor = new Color(0.02f, 0.02f, 0.04f, 1f);
    public Color exploredColor = new Color(0.16f, 0.18f, 0.16f, 1f);
    public Color visibleColor = new Color(0.30f, 0.34f, 0.26f, 1f);
    public Color friendlyColor = new Color(0.2f, 1f, 0.2f, 1f);
    public Color enemyColor = new Color(1f, 0.2f, 0.2f, 1f);
    public Color resourceColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color villageColor = Color.white;
    public Color spiritPortalColor = new Color(0.8f, 0.3f, 1f, 1f);

    private Texture2D texture;
    private Color32[] pixels;
    private float timer;

    private void Start()
    {
        texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        pixels = new Color32[textureSize * textureSize];

        if (minimapImage != null)
        {
            minimapImage.texture = texture;

            // Click (or drag) the minimap to jump the camera there.
            MinimapClickRelay relay = minimapImage.gameObject.GetComponent<MinimapClickRelay>();
            if (relay == null) relay = minimapImage.gameObject.AddComponent<MinimapClickRelay>();
            relay.controller = this;
        }
        else Debug.LogWarning("MinimapController: assign a RawImage.");
    }

    /// <summary>Camera jump from a pointer position over the minimap image.</summary>
    public void JumpCameraTo(Vector2 screenPoint, Camera eventCamera)
    {
        if (minimapImage == null || RTSCameraController.instance == null) return;

        Vector2 local;
        RectTransform rect = minimapImage.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, eventCamera, out local))
        {
            return;
        }

        // Local point -> 0..1 UV -> world.
        Rect r = rect.rect;
        float u = Mathf.Clamp01((local.x - r.xMin) / r.width);
        float v = Mathf.Clamp01((local.y - r.yMin) / r.height);

        Vector3 world = AreaCenter + new Vector3((u - 0.5f) * AreaSize.x, 0f, (v - 0.5f) * AreaSize.y);
        RTSCameraController.instance.JumpTo(world);
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = refreshInterval;
        Redraw();
    }

    private void Redraw()
    {
        FogOfWar fog = FogOfWar.Instance;

        // --- background from fog states -------------------------------
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Color32 color;
                if (fog != null && fog.fogEnabled)
                {
                    Vector3 world = PixelToWorld(x, y);
                    FogOfWar.CellState state = fog.GetState(world);
                    color = state == FogOfWar.CellState.Visible ? (Color32)visibleColor
                        : state == FogOfWar.CellState.Explored ? (Color32)exploredColor
                        : (Color32)hiddenColor;
                }
                else
                {
                    color = visibleColor;
                }
                pixels[y * textureSize + x] = color;
            }
        }

        // --- discovered points of interest -----------------------------
        foreach (MinimapPOI poi in MinimapPOI.All)
        {
            if (poi == null) continue;

            if (!poi.discovered && FogOfWar.IsExploredAt(poi.transform.position))
            {
                poi.discovered = true; // permanent map knowledge
            }
            if (!poi.discovered) continue;

            DrawDot(poi.transform.position, ColorFor(poi), 2);
        }

        // --- units ------------------------------------------------------
        if (UnitSelectionManager.Instance != null)
        {
            foreach (GameObject go in UnitSelectionManager.Instance.allUnitsList)
            {
                if (go == null) continue;

                bool mine = FactionUtility.IsLocallyControlled(go);
                if (mine)
                {
                    DrawDot(go.transform.position, friendlyColor, 2);
                }
                else if (FogOfWar.IsVisibleAt(go.transform.position))
                {
                    DrawDot(go.transform.position, enemyColor, 2);
                }
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false);
    }

    private Color ColorFor(MinimapPOI poi)
    {
        if (poi.colorOverride.a > 0.01f) return poi.colorOverride;

        switch (poi.type)
        {
            case MinimapPOI.POIType.ResourceNode: return resourceColor;
            case MinimapPOI.POIType.Village: return villageColor;
            case MinimapPOI.POIType.SpiritPortal: return spiritPortalColor;
            case MinimapPOI.POIType.CommandCenter:
                Faction faction = FactionManager.Get(FactionUtility.GetFactionId(poi.gameObject));
                return faction != null ? NationInfo.ThemeColor(faction.nation) : enemyColor;
            default: return Color.cyan;
        }
    }

    // ------------------------------------------------------------------

    private void DrawDot(Vector3 worldPosition, Color color, int radius)
    {
        int px, py;
        if (!WorldToPixel(worldPosition, out px, out py)) return;

        Color32 c = color;
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                int tx = px + x;
                int ty = py + y;
                if (tx < 0 || ty < 0 || tx >= textureSize || ty >= textureSize) continue;
                pixels[ty * textureSize + tx] = c;
            }
        }
    }

    private Vector2 AreaSize => FogOfWar.Instance != null ? FogOfWar.Instance.mapSize : mapSize;
    private Vector3 AreaCenter => FogOfWar.Instance != null ? FogOfWar.Instance.transform.position : mapCenter;

    private bool WorldToPixel(Vector3 world, out int x, out int y)
    {
        Vector3 local = world - AreaCenter;
        float u = local.x / AreaSize.x + 0.5f;
        float v = local.z / AreaSize.y + 0.5f;
        x = Mathf.FloorToInt(u * textureSize);
        y = Mathf.FloorToInt(v * textureSize);
        return x >= 0 && y >= 0 && x < textureSize && y < textureSize;
    }

    private Vector3 PixelToWorld(int x, int y)
    {
        float u = (x + 0.5f) / textureSize - 0.5f;
        float v = (y + 0.5f) / textureSize - 0.5f;
        return AreaCenter + new Vector3(u * AreaSize.x, 0f, v * AreaSize.y);
    }
}

/// <summary>Forwards clicks/drags on the minimap RawImage to the controller
/// (added automatically by MinimapController).</summary>
public class MinimapClickRelay : MonoBehaviour,
    UnityEngine.EventSystems.IPointerDownHandler,
    UnityEngine.EventSystems.IDragHandler
{
    [HideInInspector] public MinimapController controller;

    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (controller != null) controller.JumpCameraTo(eventData.position, eventData.pressEventCamera);
    }

    public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (controller != null) controller.JumpCameraTo(eventData.position, eventData.pressEventCamera);
    }
}
