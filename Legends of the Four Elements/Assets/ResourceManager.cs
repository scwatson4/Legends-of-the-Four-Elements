using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; set; }

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

    public int credits = 300;
    public int gold = 480;
    public int wood = 350;
    public int stone = 500;
    public int food = 400;
    public int spiritEnergy = 150;

    public event Action OnResourceChanged;
    public event Action OnBuildingsChanged;

    public TextMeshProUGUI creditsUI;
    public TextMeshProUGUI goldUI;
    public TextMeshProUGUI woodUI;
    public TextMeshProUGUI stoneUI;
    public TextMeshProUGUI foodUI;
    public TextMeshProUGUI spiritEnergyUI;

    public List<BuildingType> allExistingBuildings;

    public enum ResourceType
    {
        Credits,
        Gold,
        Wood,
        Stone,
        Food,
        SpiritEnergy
    }

    private void Start()
    {
        UpdateUI();
    }

    public void UpdateBuildingChanged(BuildingType buildingType, bool isNew)
    {
        if (isNew)
        {
            allExistingBuildings.Add(buildingType);
        }
        else
        {
            allExistingBuildings.Remove(buildingType);
        }

        OnBuildingsChanged?.Invoke();
    }

    public int GetCredits()
    {
        return credits;
    }

    public int GetGold()
    {
        return gold;
    }

    public int GetWood()
    {
        return wood;
    }

    public int GetStone()
    {
        return stone;
    }

    public int GetFood()
    {
        return food;
    }

    public int GetSpiritEnergy()
    {
        return spiritEnergy;
    }

    public void IncreaseResource(ResourceType resource, int amountToIncrease)
    {
        switch (resource)
        {
            case ResourceType.Credits:
                credits += amountToIncrease;
                break;
            case ResourceType.Gold:
                gold += amountToIncrease;
                break;
            case ResourceType.Wood:
                wood += amountToIncrease;
                break;
            case ResourceType.Stone:
                stone += amountToIncrease;
                break;
            case ResourceType.Food:
                food += amountToIncrease;
                break;
            case ResourceType.SpiritEnergy:
                spiritEnergy += amountToIncrease;
                break;
        }

        OnResourceChanged?.Invoke();
    }

    public void DecreaseResource(ResourceType resource, int amountToDecrease)
    {
        switch (resource)
        {
            case ResourceType.Credits:
                credits -= amountToDecrease;
                break;
            case ResourceType.Gold:
                gold -= amountToDecrease;
                break;
            case ResourceType.Wood:
                wood -= amountToDecrease;
                break;
            case ResourceType.Stone:
                stone -= amountToDecrease;
                break;
            case ResourceType.Food:
                food -= amountToDecrease;
                break;
            case ResourceType.SpiritEnergy:
                spiritEnergy -= amountToDecrease;
                break;
        }

        OnResourceChanged?.Invoke();
    }

    internal int GetResourceAmount(ResourceType resource)
    {
        switch(resource)
        {
            case ResourceType.Credits:
                return credits;
            case ResourceType.Gold:
                return gold;
            case ResourceType.Wood:
                return wood;
            case ResourceType.Stone:
                return stone;
            case ResourceType.Food:
                return food;
            case ResourceType.SpiritEnergy:
                return spiritEnergy;
            default:
                break;
        }

        return 0;
    }

    internal void DecreaseResourcesBasedOnRequirement(ObjectData objectData)
    {
        foreach (BuildRequirement req in objectData.resourceRequirements)
        {
            Debug.Log($"Reducing {req.resource} by {req.amount}");  // Add this
            DecreaseResource(req.resource, req.amount);
        }
    }

    private void OnEnable()
    {
        OnResourceChanged += UpdateUI;
    }
    private void OnDisable()
    {
        OnResourceChanged -= UpdateUI;
    }
    private void UpdateUI()
    {
        creditsUI.text = $"{credits}";
        //goldUI.text = $"{gold}";
        //woodUI.text = $"{wood}";
        //stoneUI.text = $"{stone}";
        //foodUI.text = $"{food}";
        //spiritEnergyUI.text = $"{spiritEnergy}";
    }
}
