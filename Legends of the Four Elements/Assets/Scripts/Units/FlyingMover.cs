using UnityEngine;

/// <summary>
/// Smooth surface-following flight for anything airborne: glider monks,
/// bison couriers, future war balloons. The flyer cruises a fixed height
/// above WHATEVER is below it - terrain, water, buildings, trees - rising
/// and falling smoothly over small hills and rooftops. Obstacles taller
/// than its climb ceiling (mountains, great walls) are flown AROUND, not
/// through: a forward probe checks the ground ahead and steers to the
/// clearest heading.
///
/// Drive it with SetDestination(); OnArrived fires at the goal. Works with
/// no NavMeshAgent at all (couriers) or alongside a disabled one (gliding
/// units that land again).
/// </summary>
public class FlyingMover : MonoBehaviour
{
    [Header("Flight")]
    public float speed = 10f;
    public float cruiseHeight = 6f;
    public float turnDegreesPerSecond = 120f;
    [Tooltip("Seconds of smoothing on altitude changes - bigger = floatier.")]
    public float altitudeSmoothTime = 0.6f;

    [Header("Obstacle Handling")]
    [Tooltip("Rises ahead taller than this (above current ground) are flown AROUND.")]
    public float maxClimb = 12f;
    public float probeDistance = 14f;
    public LayerMask surfaceMask = ~0;

    public bool HasDestination { get; private set; }
    public System.Action OnArrived;

    private Vector3 destination;
    private float verticalVelocity;

    public void SetDestination(Vector3 target)
    {
        destination = target;
        HasDestination = true;
    }

    public void Stop()
    {
        HasDestination = false;
    }

    private void Update()
    {
        // Horizontal travel.
        if (HasDestination)
        {
            Vector3 toTarget = destination - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= Mathf.Max(2f, speed * 0.2f))
            {
                HasDestination = false;
                OnArrived?.Invoke();
            }
            else
            {
                Vector3 heading = SteerAroundTallObstacles(toTarget.normalized);
                Quaternion wanted = Quaternion.LookRotation(heading);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, wanted, turnDegreesPerSecond * Time.deltaTime);

                // Fly along our nose, so turns carve smooth arcs.
                Vector3 flatForward = transform.forward;
                flatForward.y = 0f;
                transform.position += flatForward.normalized * speed * Time.deltaTime;
            }
        }

        // Altitude: hug whatever is below, smoothly.
        float surfaceY = SampleSurfaceHeight(transform.position);
        float targetY = surfaceY + cruiseHeight;
        float newY = Mathf.SmoothDamp(transform.position.y, targetY, ref verticalVelocity, altitudeSmoothTime);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    /// <summary>Height of the world directly under a point (terrain, water
    /// plane, rooftops - whatever the raycast finds).</summary>
    public float SampleSurfaceHeight(Vector3 position)
    {
        Vector3 rayStart = new Vector3(position.x, position.y + 120f, position.z);
        RaycastHit hit;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 400f, surfaceMask))
        {
            return hit.point.y;
        }
        return 0f;
    }

    /// <summary>Small hills and buildings we float over; mountains we go around.</summary>
    private Vector3 SteerAroundTallObstacles(Vector3 heading)
    {
        float groundHere = SampleSurfaceHeight(transform.position);

        if (IsPassable(heading, groundHere)) return heading;

        // Try progressively wider detours, alternating sides.
        for (int step = 1; step <= 4; step++)
        {
            float angle = step * 35f;
            Vector3 right = Quaternion.Euler(0f, angle, 0f) * heading;
            if (IsPassable(right, groundHere)) return right;
            Vector3 left = Quaternion.Euler(0f, -angle, 0f) * heading;
            if (IsPassable(left, groundHere)) return left;
        }

        // Boxed in on all sides: climb it anyway rather than stalling.
        return heading;
    }

    private bool IsPassable(Vector3 heading, float groundHere)
    {
        Vector3 probePoint = transform.position + heading * probeDistance;
        float groundAhead = SampleSurfaceHeight(probePoint);
        return groundAhead - groundHere <= maxClimb;
    }
}
