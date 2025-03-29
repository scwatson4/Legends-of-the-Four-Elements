using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UnitSelectionManager.Instance.allUnitsList.Add(gameObject);
    }

    private void OnDestroy()
    {
        UnitSelectionManager.Instance.allUnitsList.Remove(gameObject);
    }
}
