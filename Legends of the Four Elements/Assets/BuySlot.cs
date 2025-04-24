using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuySlot : MonoBehaviour
{
    public Sprite availableSprite;
    public Sprite unavailableSprite;

    public bool isAvailable;

    public BuySystem buySystem;

    public int databaseItemID;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(ClickedOnSlot);
        HandleResourcesChanged();
    }

    public void ClickedOnSlot()
    {
        Debug.Log("Button clicked: " + databaseItemID); // Log the ID of the building
        if (isAvailable)
        {
            Debug.Log("Building can be placed.");
            buySystem.placementSystem.StartPlacement(databaseItemID);
        }
        else
        {
            Debug.Log("Not enough resources for this building.");
        }
}

    private void UpdateAvailabilityUI()
    {
        if (isAvailable)
        {
            GetComponent<Image>().sprite = availableSprite;
            GetComponent<Button>().interactable = true;
        }
        else
        {
            GetComponent<Image>().sprite = unavailableSprite;
            GetComponent<Button>().interactable = false;
        }
    }

    private void OnEnable()
    {
        // Subscribe to the resource change event
        ResourceManager.Instance.OnResourceChanged += HandleResourcesChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe from the resource change event
        ResourceManager.Instance.OnResourceChanged -= HandleResourcesChanged;
    }

    private void HandleResourcesChanged()
    {
        ObjectData objectData = DatabaseManager.Instance.objectsDatabase.objectsData[databaseItemID];

        bool requirement = true;

        foreach (BuildRequirement req in objectData.requirements)
        {
            if (ResourceManager.Instance.GetResourceAmount(req.resource) < req.amount)
            {
                requirement = false;
                break;
            }
        }

        isAvailable = requirement;

        UpdateAvailabilityUI();
    }
}
