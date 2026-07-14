using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance { get; set; }

    public List<GameObject> allUnitsList = new List<GameObject>();
    public List<GameObject> selectedUnitsList = new List<GameObject>();

    public LayerMask clickable;
    public LayerMask ground;
    public LayerMask attackable;

    [Tooltip("Only allow selecting units owned by the local player's faction.")]
    public bool restrictSelectionToLocalPlayer = true;

    public bool attackCursorVisible;
    public GameObject groundMarker;

    private Camera cam;

    // Control groups (Ctrl+1..9 assign, 1..9 select), attack-move (F),
    // and double-click select-all-of-type.
    private readonly Dictionary<int, List<GameObject>> controlGroups =
        new Dictionary<int, List<GameObject>>();
    private bool attackMoveArmed;
    private float lastClickTime;
    private GameObject lastClickedUnit;
    private UnitSpawner selectedSpawner;
    private float lastSelectArmyTime = -10f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        // Clean up destroyed units from selectedUnitsList
        selectedUnitsList.RemoveAll(unit => unit == null);

        // While placing a building, the mouse belongs to the BuildingPlacer.
        if (BuildingPlacer.IsPlacing) return;

        // While embodying a unit, mouse/keys drive that unit instead.
        if (EmbodimentController.IsActive) return;

        // While a superweapon awaits its target, the click belongs to it.
        if (Superweapon.IsTargeting) return;

        // Sell a building: hover it and press X (refunds half its cost).
        if (Input.GetKeyDown(KeyCode.X))
        {
            RaycastHit sellHit;
            Ray sellRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(sellRay, out sellHit, Mathf.Infinity))
            {
                Structure structure = sellHit.collider.GetComponentInParent<Structure>();
                if (structure != null && FactionUtility.IsLocallyControlled(structure.gameObject))
                {
                    structure.Sell();
                }
            }
        }

        HandleControlGroups();

        // E: select the ENTIRE army (all military, workers excluded).
        // Double-tap E: also jump the camera to the army's center.
        if (Input.GetKeyDown(KeyCode.E))
        {
            bool doubleTap = Time.unscaledTime - lastSelectArmyTime < 0.4f;
            lastSelectArmyTime = Time.unscaledTime;
            SelectEntireArmy(doubleTap);
        }

        // F arms attack-move: the next left-click on ground sends the army
        // there, engaging everything hostile it meets on the way.
        if (Input.GetKeyDown(KeyCode.F) && selectedUnitsList.Count > 0)
        {
            attackMoveArmed = true;
        }
        if (attackMoveArmed && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
        {
            attackMoveArmed = false;
        }

        if (attackMoveArmed && Input.GetMouseButtonDown(0))
        {
            RaycastHit amHit;
            Ray amRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(amRay, out amHit, Mathf.Infinity, ground))
            {
                IssueAttackMove(amHit.point);
            }
            attackMoveArmed = false;
            return; // consume the click - don't also change the selection
        }

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // If we are hitting a clickable object
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, clickable) && CanSelect(hit.collider.gameObject))
            {
                selectedSpawner = null;
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    SelectMultiple(hit.collider.gameObject);
                }
                else
                {
                    SelectByClicking(hit.collider.gameObject);
                }
            }
            else // If we are NOT hitting a clickable object
            {
                // Clicking one of your production buildings selects it, so a
                // following right-click on ground sets its rally point.
                UnitSpawner spawner = null;
                if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                {
                    UnitSpawner hitSpawner = hit.collider.GetComponentInParent<UnitSpawner>();
                    if (hitSpawner != null && FactionUtility.IsLocallyControlled(hitSpawner.gameObject))
                    {
                        spawner = hitSpawner;
                    }
                }
                selectedSpawner = spawner;
                if (spawner != null)
                {
                    Debug.Log($"{spawner.gameObject.name} selected - right-click ground to set its rally point.");
                }

                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    DeselectAll();
                }
            }
        }

        // Rally point: right-click ground with a production building selected.
        if (selectedSpawner != null && selectedUnitsList.Count == 0 && Input.GetMouseButtonDown(1))
        {
            RaycastHit rallyHit;
            Ray rallyRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(rallyRay, out rallyHit, Mathf.Infinity, ground))
            {
                selectedSpawner.SetRallyPoint(rallyHit.point);
                groundMarker.transform.position = rallyHit.point;
                groundMarker.SetActive(false);
                groundMarker.SetActive(true);
            }
        }

        if (Input.GetMouseButtonDown(1) && selectedUnitsList.Count > 0)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Befriend/tame command: right-click on a tameable being.
            // Only energy benders (the Avatar) can channel it.
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                Tameable tameable = hit.collider.GetComponentInParent<Tameable>();
                if (tameable != null && tameable.IsTameableNow())
                {
                    foreach (GameObject unit in selectedUnitsList)
                    {
                        if (unit != null && tameable.CanBeTamedBy(unit)) tameable.OrderTame(unit);
                    }
                }

                // Spirit-world travel: right-click a portal to send the
                // selected units through to its linked portal.
                SpiritPortal portal = hit.collider.GetComponentInParent<SpiritPortal>();
                if (portal != null && portal.travelEnabled)
                {
                    foreach (GameObject unit in selectedUnitsList)
                    {
                        if (unit != null && FactionUtility.IsLocallyControlled(unit))
                        {
                            portal.OrderTravel(unit);
                        }
                    }
                }

                // Colossus awakening: only an Avatar can merge with the
                // sleeping giant - right-click it with your Avatar selected.
                ColossalSpirit colossus = hit.collider.GetComponentInParent<ColossalSpirit>();
                if (colossus != null && !colossus.IsAwakened)
                {
                    foreach (GameObject unit in selectedUnitsList)
                    {
                        AvatarUnit avatar = unit != null ? unit.GetComponent<AvatarUnit>() : null;
                        if (avatar != null && FactionUtility.IsLocallyControlled(unit) &&
                            colossus.OrderChannel(avatar))
                        {
                            break; // one Avatar is all it takes
                        }
                    }
                }

                // Harvest order: right-click a resource node with workers selected.
                ResourceNode node = hit.collider.GetComponentInParent<ResourceNode>();
                if (node != null && !node.IsDepleted)
                {
                    foreach (GameObject unit in selectedUnitsList)
                    {
                        ResourceCollector collector = unit != null ? unit.GetComponent<ResourceCollector>() : null;
                        if (collector != null) collector.SetTargetNode(node);
                    }
                }

                // Board order: right-click a friendly transport (sky bison,
                // war balloon) with infantry selected.
                Transport transport = hit.collider.GetComponentInParent<Transport>();
                if (transport != null &&
                    FactionUtility.IsLocallyControlled(transport.gameObject) &&
                    !selectedUnitsList.Contains(transport.gameObject))
                {
                    foreach (GameObject unit in selectedUnitsList)
                    {
                        transport.OrderBoard(unit);
                    }
                }

                // Repair order: right-click a damaged friendly building
                // with workers selected.
                Structure damagedBuilding = hit.collider.GetComponentInParent<Structure>();
                if (damagedBuilding != null && damagedBuilding.IsDamaged &&
                    FactionUtility.IsLocallyControlled(damagedBuilding.gameObject))
                {
                    foreach (GameObject unit in selectedUnitsList)
                    {
                        ResourceCollector worker = unit != null ? unit.GetComponent<ResourceCollector>() : null;
                        if (worker != null) worker.SetRepairTarget(damagedBuilding);
                    }
                }
            }

            // Ground move order
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, ground))
            {
                groundMarker.transform.position = hit.point;

                groundMarker.SetActive(false);
                groundMarker.SetActive(true);

                if (SoundManager.Instance != null) SoundManager.Instance.PlayMoveBark();

                // Clear attack targets for selected units
                foreach (GameObject unit in selectedUnitsList)
                {
                    if (unit != null && unit.GetComponent<AttackController>() != null)
                    {
                        unit.GetComponent<AttackController>().targetToAttack = null;
                    }
                }
            }
        }

        // Attack Target
        if (selectedUnitsList.Count > 0 && AtLeastOneOffensiveUnit(selectedUnitsList))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // If we are hitting an attackable object
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, attackable))
            {
                attackCursorVisible = true;

                if (Input.GetMouseButton(1))
                {
                    Transform target = hit.transform;

                    if (Input.GetMouseButtonDown(1) && SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlayAttackBark();
                    }

                    // Don't order attacks on friendlies or peaceful neutrals.
                    Tameable tameable = hit.collider.GetComponentInParent<Tameable>();
                    foreach (GameObject unit in selectedUnitsList)
                    {
                        if (unit == null || unit.GetComponent<AttackController>() == null) continue;
                        if (!FactionUtility.AreHostile(unit, target.gameObject))
                        {
                            // Peaceful being: treat the click as a befriend order instead.
                            if (tameable != null && tameable.IsTameableNow()) tameable.OrderTame(unit);
                            continue;
                        }

                        // Networked client: relay the order to the server.
                        if (!NetworkUnit.TryRelayAttack(unit, target.gameObject))
                        {
                            unit.GetComponent<AttackController>().targetToAttack = target;
                        }
                    }
                }
            }
            else
            {
                attackCursorVisible = false;
            }
        }

        CursorSelector();
    }

    /// <summary>You can only select units that belong to your own faction.</summary>
    private bool CanSelect(GameObject clicked)
    {
        if (!restrictSelectionToLocalPlayer) return true;

        Unit unit = clicked.GetComponentInParent<Unit>();
        if (unit == null) return true; // non-unit clickables (buildings etc.)

        return FactionUtility.IsLocallyControlled(unit.gameObject);
    }

    private void CursorSelector()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, clickable) && CanSelect(hit.collider.gameObject))
        {
            CursorManager.Instance.SetMarkerType(CursorManager.CursorType.Selectable);
        }
        else if (Physics.Raycast(ray, out hit, Mathf.Infinity, attackable) && selectedUnitsList.Count > 0 && AtLeastOneOffensiveUnit(selectedUnitsList))
        {
            CursorManager.Instance.SetMarkerType(CursorManager.CursorType.Attackable);
        }
        else if (Physics.Raycast(ray, out hit, Mathf.Infinity, ground) && selectedUnitsList.Count > 0)
        {
            CursorManager.Instance.SetMarkerType(CursorManager.CursorType.Walkable);
        }
        else
        {
            CursorManager.Instance.SetMarkerType(CursorManager.CursorType.None);
        }
    }

    private bool AtLeastOneOffensiveUnit(List<GameObject> selectedUnitsList)
    {
        foreach (GameObject unit in selectedUnitsList)
        {
            if (unit != null && unit.GetComponent<AttackController>() != null)
            {
                return true;
            }
        }
        return false;
    }

    private void SelectMultiple(GameObject unit)
    {
        if (!selectedUnitsList.Contains(unit))
        {
            selectedUnitsList.Add(unit);
            SelectUnit(unit, true);
        }
        else
        {
            SelectUnit(unit, false);
            selectedUnitsList.Remove(unit);
        }
    }

    public void DeselectAll()
    {
        foreach (var unit in selectedUnitsList)
        {
            if (unit != null)
            {
                SelectUnit(unit, false);
            }
        }

        groundMarker.SetActive(false);

        selectedUnitsList.Clear();
    }

    internal void DragSelect(GameObject unit)
    {
        if (selectedUnitsList.Contains(unit) == false && CanSelect(unit))
        {
            selectedUnitsList.Add(unit);
            SelectUnit(unit, true);
        }
    }

    private void SelectUnit(GameObject unit, bool isSelected)
    {
        if (unit != null)
        {
            TriggerSelectionIndicator(unit, isSelected);
            EnableUnitMovement(unit, isSelected);
        }
    }

    private void SelectByClicking(GameObject unit)
    {
        // Double-click: grab every unit of the same type you own.
        bool doubleClick = unit == lastClickedUnit &&
                           Time.unscaledTime - lastClickTime < 0.35f;
        lastClickedUnit = unit;
        lastClickTime = Time.unscaledTime;

        DeselectAll();

        if (doubleClick)
        {
            SelectAllOfSameType(unit);
            return;
        }

        selectedUnitsList.Add(unit);

        SelectUnit(unit, true);
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySelectBark();
    }

    private void SelectAllOfSameType(GameObject clicked)
    {
        Unit clickedUnit = clicked.GetComponentInParent<Unit>();
        if (clickedUnit == null)
        {
            selectedUnitsList.Add(clicked);
            SelectUnit(clicked, true);
            return;
        }

        foreach (GameObject go in allUnitsList)
        {
            if (go == null || !CanSelect(go)) continue;
            Unit unit = go.GetComponent<Unit>();
            if (unit == null || unit.unitType != clickedUnit.unitType) continue;

            selectedUnitsList.Add(go);
            SelectUnit(go, true);
        }
    }

    // ------------------------------------------------------------------
    // Control groups & attack-move
    // ------------------------------------------------------------------

    private void HandleControlGroups()
    {
        bool assign = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        for (int number = 1; number <= 9; number++)
        {
            if (!Input.GetKeyDown(KeyCode.Alpha0 + number)) continue;

            if (assign)
            {
                if (selectedUnitsList.Count == 0) continue;
                controlGroups[number] = new List<GameObject>(selectedUnitsList);
                Debug.Log($"Control group {number}: {selectedUnitsList.Count} units assigned.");
            }
            else if (controlGroups.TryGetValue(number, out List<GameObject> group))
            {
                group.RemoveAll(u => u == null);
                if (group.Count == 0) continue;

                DeselectAll();
                foreach (GameObject unit in group)
                {
                    selectedUnitsList.Add(unit);
                    SelectUnit(unit, true);
                }
            }
        }
    }

    /// <summary>Halo Wars style: grab every military unit you own (workers
    /// keep working). Also wireable to a UI button.</summary>
    public void SelectEntireArmy(bool centerCamera = false)
    {
        DeselectAll();

        Vector3 centroid = Vector3.zero;
        int count = 0;

        foreach (GameObject go in allUnitsList)
        {
            if (go == null || !FactionUtility.IsLocallyControlled(go)) continue;
            if (go.GetComponent<AttackController>() == null) continue;      // pacifists stay
            if (go.GetComponent<ResourceCollector>() != null) continue;     // workers keep working

            selectedUnitsList.Add(go);
            SelectUnit(go, true);
            centroid += go.transform.position;
            count++;
        }

        if (count == 0) return;
        Debug.Log($"Entire army selected: {count} units.");

        if (centerCamera && RTSCameraController.instance != null)
        {
            RTSCameraController.instance.JumpTo(centroid / count);
        }
    }

    private void IssueAttackMove(Vector3 destination)
    {
        groundMarker.transform.position = destination;
        groundMarker.SetActive(false);
        groundMarker.SetActive(true);

        for (int i = 0; i < selectedUnitsList.Count; i++)
        {
            GameObject unit = selectedUnitsList[i];
            if (unit == null) continue;

            AttackController attack = unit.GetComponent<AttackController>();
            if (attack != null) attack.targetToAttack = null;

            // Advance in formation, not as a single-point dogpile.
            Vector3 slot = FormationUtility.GetDestination(destination, i, selectedUnitsList.Count);

            if (NetworkUnit.TryRelayMove(unit, slot)) continue;

            UnityEngine.AI.NavMeshAgent agent = unit.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                // Deliberately NOT setting isCommandedToMove: that flag
                // suppresses target acquisition, and attack-move should
                // engage everything hostile along the way.
                agent.SetDestination(slot);
            }
        }
        Debug.Log($"Attack-move: {selectedUnitsList.Count} units advancing.");
    }

    private void EnableUnitMovement(GameObject unit, bool shouldMove)
    {
        if (unit != null)
        {
            var movement = unit.GetComponent<UnitMovement>();
            if (movement != null)
            {
                movement.enabled = shouldMove;
            }
        }
    }

    private void TriggerSelectionIndicator(GameObject unit, bool isVisible)
    {
        if (unit != null)
        {
            var indicator = unit.transform.Find("Indicator");
            if (indicator != null)
            {
                indicator.gameObject.SetActive(isVisible);
            }
        }
    }

    // Called by Unit.cs when a unit is destroyed
    public void OnUnitDestroyed(GameObject unit)
    {
        if (selectedUnitsList.Contains(unit))
        {
            SelectUnit(unit, false);
            selectedUnitsList.Remove(unit);
        }
        allUnitsList.Remove(unit);
    }
}
