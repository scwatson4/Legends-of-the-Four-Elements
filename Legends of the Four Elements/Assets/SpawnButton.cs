using UnityEngine;
using UnityEngine.UI;

public class SpawnButton : MonoBehaviour
{
    public CommandCenterSpawner commandCenter;
    public GameObject prefab;
    public int cost = 100;
    public float buildTime = 3f;

    public void OnClickSpawn()
    {
        var unit = new CommandCenterSpawner.UnitToBuild
        {
            prefab = prefab,
            cost = cost,
            buildTime = buildTime
        };

        commandCenter.QueueUnit(unit);
    }
}
