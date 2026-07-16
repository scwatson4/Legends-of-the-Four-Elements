using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Trade routes: while you control a village, it periodically sends a trade
/// cart rolling to your command center; each cart that ARRIVES pays silver.
/// Carts are real (greybox) units - slow, unarmed, killable - so enemies can
/// raid your trade lanes for bounty, and escorting them matters. Works for
/// the AI's villages too. Self-bootstraps; zero wiring.
/// </summary>
public class TradeRouteDirector : MonoBehaviour
{
    public float dispatchInterval = 25f;
    public int silverPerDelivery = 30;
    public int cartHealth = 60;
    public int cartRaidBounty = 20;
    public float cartSpeed = 3f;

    private float timer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<TradeRouteDirector>() != null) return;
        GameObject go = new GameObject("TradeRouteDirector");
        go.AddComponent<TradeRouteDirector>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.GameIsOver) return;
        if (NetworkGuard.BlockLocalSimulation) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = dispatchInterval;

        foreach (Village village in FindObjectsByType<Village>(FindObjectsSortMode.None))
        {
            int factionId = village.ControllingFactionId();
            if (factionId == FactionManager.NoFaction) continue;

            CommandCenter home = FindCommandCenter(factionId);
            if (home == null) continue;

            SpawnCart(village.transform.position, home.transform.position, factionId);
        }
    }

    private static CommandCenter FindCommandCenter(int factionId)
    {
        foreach (CommandCenter cc in FindObjectsByType<CommandCenter>(FindObjectsSortMode.None))
        {
            if (FactionUtility.GetFactionId(cc.gameObject) == factionId) return cc;
        }
        return null;
    }

    private void SpawnCart(Vector3 from, Vector3 to, int factionId)
    {
        // Greybox cart assembled at runtime - swap for a real cart prefab by
        // replacing this block later if you like.
        GameObject cart = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GreyboxMaterial.Harmonize(cart); // URP-safe material for runtime primitives
        cart.name = "Trade Cart";
        cart.transform.position = from + Vector3.up * 0.5f;
        cart.transform.localScale = new Vector3(1.2f, 1f, 1.8f);

        NavMeshAgent agent = cart.AddComponent<NavMeshAgent>();
        agent.speed = cartSpeed;
        agent.radius = 0.7f;

        Unit unit = cart.AddComponent<Unit>();
        unit.maxUnitHealth = cartHealth;
        unit.killBounty = cartRaidBounty;
        unit.category = UnitCategory.Worker;
        unit.populationCost = 0; // trade doesn't crowd the army

        FactionUtility.SetFaction(cart, factionId);
        if (cart.GetComponent<NationColorizer>() == null) cart.AddComponent<NationColorizer>();

        TradeCart route = cart.AddComponent<TradeCart>();
        route.destination = to;
        route.payout = silverPerDelivery;
    }
}

/// <summary>One cart's journey: reach the destination, pay out, vanish.</summary>
public class TradeCart : MonoBehaviour
{
    public Vector3 destination;
    public int payout = 30;
    public float arriveRadius = 6f;

    private NavMeshAgent agent;
    private float repathTimer;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = 2f;
            agent.SetDestination(destination);
        }

        if (Vector3.Distance(transform.position, destination) <= arriveRadius)
        {
            Economy.Award(FactionUtility.GetFactionId(gameObject), payout);
            Debug.Log($"Trade cart delivered: +{payout} silver.");
            Destroy(gameObject);
        }
    }
}
