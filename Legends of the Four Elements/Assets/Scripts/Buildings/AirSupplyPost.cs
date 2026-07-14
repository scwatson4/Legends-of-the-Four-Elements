using UnityEngine;

/// <summary>
/// Sky Mooring - the Air Nomads' aerial supply line. Build one far from your
/// command center and bison couriers launch from home, FLY over any terrain
/// (rivers, cliffs, enemy walls) using the flight system, and deliver silver
/// when they moor. Couriers are living units: towers and ranged fire can
/// shoot them down mid-flight, so supply lanes are contestable airspace.
///
/// Prefab recipe: mooring model + collider + Structure + this (Air roster,
/// Special ~180). Assign a bison courier prefab, or leave empty for a
/// kingdom-tinted greybox bison. The farther from home, the better the pay.
/// </summary>
[RequireComponent(typeof(Structure))]
public class AirSupplyPost : MonoBehaviour
{
    public float runInterval = 30f;
    [Tooltip("Base pay per delivery; distance adds up to +100% at 120m.")]
    public int silverPerRun = 30;
    public float minDistanceFromBase = 25f;

    [Header("Courier")]
    [Tooltip("Optional bison prefab (needs nothing special - flight is added). Empty = greybox bison.")]
    public GameObject courierPrefab;
    public int courierHealth = 70;
    public int courierBounty = 15;
    public float courierSpeed = 10f;
    public float courierAltitude = 9f;

    private float timer = 8f;
    private GameObject activeCourier;

    private void Update()
    {
        if (NetworkGuard.BlockLocalSimulation) return;
        if (activeCourier != null) return; // one bison per mooring

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = runInterval;

        CommandCenter home = FindHomeBase();
        if (home == null) return;

        float distance = Vector3.Distance(home.transform.position, transform.position);
        if (distance < minDistanceFromBase) return; // too close to matter

        LaunchCourier(home.transform.position, distance);
    }

    private CommandCenter FindHomeBase()
    {
        int myFaction = FactionUtility.GetFactionId(gameObject);
        foreach (CommandCenter cc in FindObjectsByType<CommandCenter>(FindObjectsSortMode.None))
        {
            if (FactionUtility.GetFactionId(cc.gameObject) == myFaction) return cc;
        }
        return null;
    }

    private void LaunchCourier(Vector3 homePosition, float distance)
    {
        int factionId = FactionUtility.GetFactionId(gameObject);
        int pay = Mathf.RoundToInt(silverPerRun * (1f + Mathf.Clamp01(distance / 120f)));

        GameObject courier;
        if (courierPrefab != null)
        {
            courier = Instantiate(courierPrefab, homePosition + Vector3.up * courierAltitude,
                Quaternion.identity);
        }
        else
        {
            // Greybox bison: broad, gentle, and about to be shot at.
            courier = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            courier.name = "Bison Courier";
            courier.transform.position = homePosition + Vector3.up * courierAltitude;
            courier.transform.localScale = new Vector3(2.2f, 1.1f, 3.2f);
            courier.AddComponent<NationColorizer>();
        }

        Unit unit = courier.GetComponent<Unit>();
        if (unit == null) unit = courier.AddComponent<Unit>();
        unit.maxUnitHealth = courierHealth;
        unit.killBounty = courierBounty;
        unit.category = UnitCategory.Animal;
        unit.populationCost = 0; // supply crews don't crowd the army

        FactionUtility.SetFaction(courier, factionId);

        FlyingMover flight = courier.GetComponent<FlyingMover>();
        if (flight == null) flight = courier.AddComponent<FlyingMover>();
        flight.speed = courierSpeed;
        flight.cruiseHeight = courierAltitude;
        flight.OnArrived = () =>
        {
            if (courier == null) return;
            Economy.Award(factionId, pay);
            Debug.Log($"Bison courier moors: +{pay} silver.");
            Destroy(courier);
        };
        flight.SetDestination(transform.position);

        activeCourier = courier;
        Debug.Log($"A bison courier lifts off, bound for {gameObject.name} ({pay} silver aboard).");
    }
}
