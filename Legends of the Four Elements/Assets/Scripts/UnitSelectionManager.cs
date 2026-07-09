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

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // If we are hitting a clickable object
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, clickable) && CanSelect(hit.collider.gameObject))
            {
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
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    DeselectAll();
                }
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
            }

            // Ground move order
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, ground))
            {
                groundMarker.transform.position = hit.point;

                groundMarker.SetActive(false);
                groundMarker.SetActive(true);

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
        DeselectAll();

        selectedUnitsList.Add(unit);

        SelectUnit(unit, true);
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
