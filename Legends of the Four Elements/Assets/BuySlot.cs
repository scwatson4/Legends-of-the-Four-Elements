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
        // Subscribe to the resource change event
        ResourceManager.Instance.OnResourceChanged += HandleResourcesChanged;
        GetComponent<Button>().onClick.AddListener(ClickedOnSlot);
        HandleResourcesChanged();

        ResourceManager.Instance.OnBuildingsChanged += HandleBuildingsChanged;
        HandleBuildingsChanged();

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

    //Might delete later
    private void OnEnable()
    {
    }

    //Might delete later
    private void OnDisable()
    {
        // Unsubscribe from the resource change event
        ResourceManager.Instance.OnResourceChanged -= HandleResourcesChanged;

        // Unsubscribe from the building change event
        //ResourceManager.Instance.OnBuildingsChanged -= HandleBuildingsChanged;
    }

    private void HandleResourcesChanged()
    {
        ObjectData objectData = DatabaseManager.Instance.objectsDatabase.objectsData[databaseItemID];

        bool requirement = true;

        foreach (BuildRequirement req in objectData.resourceRequirements)
        {
            if (ResourceManager.Instance.GetResourceAmount(req.resource) < req.amount)
            {
                requirement = false;
                Debug.Log($"isAvailable for {databaseItemID}: {isAvailable}");
                Debug.Log($"Requirement is equal to {requirement}");
                break;
            }
        }

        isAvailable = requirement;

        UpdateAvailabilityUI();
    }

    private void HandleBuildingsChanged()
    {
        ObjectData objectData = DatabaseManager.Instance.objectsDatabase.objectsData[databaseItemID];

        Debug.Log($"Building {databaseItemID} dependencies met: {gameObject.activeSelf}");


        foreach (BuildingType dependency in objectData.buildDependency)
        {
            //If the building has no dependencies
            if(dependency == BuildingType.None)
            {
                gameObject.SetActive(true);
                return;
            }

            if(ResourceManager.Instance.allExistingBuildings.Contains(dependency) == false)
            {
                gameObject.SetActive(false);
                return;
            }
        }

        // If all requirements are met
        gameObject.SetActive(true);
    }    
}
