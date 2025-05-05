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

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // If we are hitting a clickable object
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, clickable))
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

            // If we are hitting a clickable object
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
                        Debug.Log($"{unit.name} cleared attack target for ground movement");
                    }
                }
            }
        }

        // Attack Target
        if (selectedUnitsList.Count > 0 && AtLeastOneOffensiveUnit(selectedUnitsList))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // If we are hitting a clickable object
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, attackable))
            {
                Debug.Log("Enemy Hovered with mouse");

                attackCursorVisible = true;

                if (Input.GetMouseButton(1))
                {
                    Transform target = hit.transform;
                    foreach (GameObject unit in selectedUnitsList)
                    {
                        if (unit != null && unit.GetComponent<AttackController>() != null)
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

    private void CursorSelector()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, clickable))
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
        if (selectedUnitsList.Contains(unit) == false)
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