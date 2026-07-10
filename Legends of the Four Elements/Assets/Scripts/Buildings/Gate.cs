using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// An earthbent gate in a wall line: slides open when friendly units
/// approach (and no enemies are at the doorstep), and seals shut otherwise.
/// The stone only answers to earthbenders - if the owner has none left
/// alive, the gate stays sealed even for friends.
///
/// Prefab recipe: gate model + Structure + NavMeshObstacle (Carve ON) +
/// this + RequiresBenderPresence. The obstacle blocks pathing while closed;
/// opening disables it so friendlies path straight through. Optionally
/// assign the visual door part to gateModel - it sinks into the ground
/// when open, very earthbending.
/// </summary>
[RequireComponent(typeof(NavMeshObstacle))]
public class Gate : MonoBehaviour
{
    public float openRadius = 8f;
    public float scanInterval = 0.5f;

    [Tooltip("The owner must still have this bender type alive to operate the gate.")]
    public bool requiresBenderToOperate = true;
    public Unit.UnitType operatorBender = Unit.UnitType.Earthbender;

    [Tooltip("Optional: the visual door piece, sunk into the ground while open.")]
    public Transform gateModel;
    public float sinkDepth = 4f;
    public float animateSpeed = 6f;

    private NavMeshObstacle obstacle;
    private float scanTimer;
    private bool isOpen;
    private Vector3 modelClosedPosition;

    private void Start()
    {
        obstacle = GetComponent<NavMeshObstacle>();
        obstacle.carving = true;
        if (gateModel != null) modelClosedPosition = gateModel.localPosition;
    }

    private void Update()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = scanInterval;
            isOpen = ShouldBeOpen();
            obstacle.enabled = !isOpen; // no obstacle = friendlies path through
        }

        // Earthbend the door down into the ground / back up.
        if (gateModel != null)
        {
            Vector3 target = isOpen
                ? modelClosedPosition + Vector3.down * sinkDepth
                : modelClosedPosition;
            gateModel.localPosition = Vector3.MoveTowards(
                gateModel.localPosition, target, animateSpeed * Time.deltaTime);
        }
    }

    private bool ShouldBeOpen()
    {
        int myFaction = FactionUtility.GetFactionId(gameObject);

        // Without an earthbender in the army, the stone doesn't move.
        if (requiresBenderToOperate &&
            RequiresBenderPresence.CountBenders(myFaction, operatorBender) <= 0)
        {
            return false;
        }

        bool friendlyNear = false;
        foreach (Collider hit in Physics.OverlapSphere(transform.position, openRadius))
        {
            Unit unit = hit.GetComponentInParent<Unit>();
            if (unit == null) continue;

            if (FactionManager.AreHostile(myFaction, FactionUtility.GetFactionId(unit.gameObject)))
            {
                return false; // enemies at the doorstep: stay sealed
            }
            if (FactionUtility.GetFactionId(unit.gameObject) == myFaction)
            {
                friendlyNear = true;
            }
        }
        return friendlyNear;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, openRadius);
    }
}
