using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuySystem : MonoBehaviour
{
    public GameObject buildingsPanel;
    public GameObject unitsPanel;

    public Button buildingsButton;
    public Button unitsButton;

    public PlacementSystem placementSystem;

    private void Start()
    {
        buildingsButton.onClick.AddListener(BuildingsCategorySelected);
        unitsButton.onClick.AddListener(UnitsCategorySelected);

        buildingsPanel.SetActive(true);
        unitsPanel.SetActive(false);
    }

    private void BuildingsCategorySelected()
    {
        buildingsPanel.SetActive(true);
        unitsPanel.SetActive(false);
    }

    private void UnitsCategorySelected()
    {
        buildingsPanel.SetActive(false);
        unitsPanel.SetActive(true);
    }
}
