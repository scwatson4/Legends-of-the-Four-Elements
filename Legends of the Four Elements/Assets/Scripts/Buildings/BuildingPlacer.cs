using UnityEngine;

/// <summary>
/// Ghost-preview building placement, Army Men RTS style. Wire UI build
/// buttons to BeginPlacement(index) - the index into the local player's
/// nation building roster (NationData.buildings). A translucent ghost follows
/// the mouse: green = valid, red = blocked. Left-click places (and pays),
/// right-click / Escape cancels, R rotates.
/// </summary>
public class BuildingPlacer : MonoBehaviour
{
    public static BuildingPlacer Instance { get; private set; }

    /// <summary>True while a ghost is being placed (selection input pauses).</summary>
    public static bool IsPlacing => Instance != null && Instance.ghost != null;

    [Header("Masks")]
    public LayerMask groundMask;
    [Tooltip("Things a building cannot overlap: units, other buildings.")]
    public LayerMask obstructionMask = ~0;

    [Header("Placement")]
    public float clearanceRadius = 4f;
    public KeyCode rotateKey = KeyCode.R;
    public Color validTint = new Color(0.3f, 1f, 0.3f, 0.6f);
    public Color invalidTint = new Color(1f, 0.3f, 0.3f, 0.6f);

    private GameObject ghost;
    private NationData.BuildingEntry pendingEntry;
    private int pendingIndex = -1;
    private Camera cam;
    private bool placementValid;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        cam = Camera.main;
    }

    /// <summary>Special index for command centers (campaign scratch starts, expansions).</summary>
    public const int CommandCenterIndex = -1;

    /// <summary>Place a new command center - wire a "Found Base" button to this,
    /// or press B in campaign levels.</summary>
    public void BeginCommandCenterPlacement()
    {
        BeginPlacement(CommandCenterIndex);
    }

    /// <summary>Wire building buttons to this (index into NationData.buildings;
    /// -1 = the nation's command center).</summary>
    public void BeginPlacement(int buildingIndex)
    {
        CancelPlacement();

        NationData data = GetLocalNationData();
        NationData.BuildingEntry entry = null;
        if (data != null)
        {
            entry = buildingIndex == CommandCenterIndex
                ? MakeCommandCenterEntry(data)
                : data.GetBuilding(buildingIndex);
        }
        if (entry == null || entry.prefab == null)
        {
            Debug.LogWarning($"BuildingPlacer: no building at roster index {buildingIndex}. Fill in NationData.");
            return;
        }

        if (Economy.GetBalance(FactionManager.LocalPlayerFactionId) < entry.cost)
        {
            if (PlayerResources.Instance != null) PlayerResources.Instance.SpendCredits(entry.cost); // triggers the "not enough" popup
            return;
        }

        pendingEntry = entry;
        pendingIndex = buildingIndex;
        ghost = Instantiate(entry.prefab);
        MakeGhostly(ghost);
    }

    public void CancelPlacement()
    {
        if (ghost != null) Destroy(ghost);
        ghost = null;
        pendingEntry = null;
        pendingIndex = -1;
    }

    private void Update()
    {
        if (ghost == null) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
            return;
        }

        if (Input.GetKeyDown(rotateKey))
        {
            ghost.transform.Rotate(0f, 45f, 0f);
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, Mathf.Infinity, groundMask)) return;

        ghost.transform.position = hit.point;
        placementValid = ValidatePlacement(hit.point);
        TintGhost(placementValid ? validTint : invalidTint);

        if (placementValid && Input.GetMouseButtonDown(0))
        {
            Place(hit.point, ghost.transform.rotation);
        }
    }

    private bool ValidatePlacement(Vector3 position)
    {
        // No units/buildings in the footprint (the ghost's own colliders are disabled).
        Vector3 checkCenter = position + Vector3.up * 1f;
        foreach (Collider hit in Physics.OverlapSphere(checkCenter, clearanceRadius, obstructionMask))
        {
            if (hit.transform.root == ghost.transform.root) continue;
            if (hit.GetComponentInParent<Unit>() != null ||
                hit.GetComponentInParent<Structure>() != null ||
                hit.GetComponentInParent<CommandCenter>() != null)
            {
                return false;
            }
        }

        // Economy buildings that need a node must be placed next to one.
        IncomeBuilding income = pendingEntry.prefab.GetComponent<IncomeBuilding>();
        if (income != null && income.requiresNearbyNode)
        {
            if (!IncomeBuilding.HasMatchingNodeNear(position, income.requiredNodeType, income.nodeSearchRadius))
            {
                return false;
            }
        }

        return true;
    }

    private void Place(Vector3 position, Quaternion rotation)
    {
        NationData.BuildingEntry entry = pendingEntry;
        int index = pendingIndex;
        CancelPlacement();

        // Multiplayer: the server validates, pays and spawns.
        if (RTSNetworkPlayer.TryRelayPlaceBuilding(index, position, rotation)) return;

        if (!Economy.TrySpend(FactionManager.LocalPlayerFactionId, entry.cost)) return;

        GameObject building = Instantiate(entry.prefab, position, rotation);
        FactionUtility.SetFaction(building, FactionManager.LocalPlayerFactionId);
    }

    public static NationData.BuildingEntry MakeCommandCenterEntry(NationData data)
    {
        if (data == null || data.commandCenterPrefab == null) return null;
        return new NationData.BuildingEntry
        {
            buildingName = data.displayName + " Command Center",
            prefab = data.commandCenterPrefab,
            cost = data.commandCenterCost,
            category = BuildingCategory.Special
        };
    }

    private NationData GetLocalNationData()
    {
        NationDatabase db = NationDatabase.Load();
        if (db == null) return null;
        Faction local = FactionManager.Get(FactionManager.LocalPlayerFactionId);
        return db.Get(local != null ? local.nation : GameSetup.PlayerNation);
    }

    // ------------------------------------------------------------------
    // Ghost visuals
    // ------------------------------------------------------------------

    private void MakeGhostly(GameObject go)
    {
        foreach (MonoBehaviour behaviour in go.GetComponentsInChildren<MonoBehaviour>())
        {
            behaviour.enabled = false;
        }
        foreach (Collider collider in go.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }
        TintGhost(validTint);
    }

    private void TintGhost(Color color)
    {
        if (ghost == null) return;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        foreach (Renderer renderer in ghost.GetComponentsInChildren<Renderer>())
        {
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color); // URP lit
            block.SetColor("_Color", color);     // legacy/other shaders
            renderer.SetPropertyBlock(block);
        }
    }
}
