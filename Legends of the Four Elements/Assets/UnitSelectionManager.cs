using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance { get; set; }

    public List<GameObject> allUnitsList = new List<GameObject>();
    public List<GameObject> selectedUnitsList = new List<GameObject>();

    public LayerMask clickable;
    public LayerMask ground;
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
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            //If we are hitting a clickable object
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

            //If we are hitting a clickable object
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, ground))
            {
                groundMarker.transform.position = hit.point;

                groundMarker.SetActive(false);
                groundMarker.SetActive(true);
            }
        }
    }

    private void SelectMultiple(GameObject unit)
    {
        if (!selectedUnitsList.Contains(unit))
        {
            selectedUnitsList.Add(unit);
            TriggerSelectionIndicator(unit, true);
            EnableUnitMovement(unit, true);
        }
        else
        {
            EnableUnitMovement(unit, false);
            TriggerSelectionIndicator(unit, false);
            selectedUnitsList.Remove(unit);
        }
    }

    private void DeselectAll()
    {
        foreach (var unit in selectedUnitsList)
        {
            EnableUnitMovement(unit, false);
            TriggerSelectionIndicator(unit, false);
        }

        groundMarker.SetActive(false);

        selectedUnitsList.Clear();
    }

    private void SelectByClicking(GameObject unit)
    {
        DeselectAll();

        selectedUnitsList.Add(unit);

        TriggerSelectionIndicator(unit, true);

        EnableUnitMovement(unit, true);
    }

    private void EnableUnitMovement(GameObject unit, bool shouldMove)
    {
        unit.GetComponent<UnitMovement>().enabled = shouldMove;
    }

    private void TriggerSelectionIndicator(GameObject unit, bool isVisible)
    {
        unit.transform.GetChild(0).gameObject.SetActive(isVisible);
    }
}
