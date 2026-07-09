using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// "Enter the fight": possess any of your units and play from its
/// perspective while the rest of the game keeps simulating around you.
///
///   H (with exactly one of your units selected)  -> possess it
///   WASD  -> run (NavMesh-constrained)   Mouse -> look
///   Left-click / Space -> attack what's in front of you
///   H / Esc -> return to the command camera
///
/// This is the foundation of the Quest 3 mode: on desktop the main camera
/// parents to the unit's head; in VR you parent your XR Origin to
/// HeadAnchor instead (see docs/VR_AND_VOICE.md). Works for every unit,
/// including the Avatar - possess it and press T/G for elements and the
/// Avatar State from the inside.
/// Self-bootstraps into every scene; does nothing until you press H.
/// </summary>
public class EmbodimentController : MonoBehaviour
{
    public static EmbodimentController Instance { get; private set; }

    /// <summary>True while the player is inside a unit (pauses RTS input).</summary>
    public static bool IsActive => Instance != null && Instance.possessed != null;

    [Header("Keys")]
    public KeyCode possessKey = KeyCode.H;

    [Header("Camera")]
    public Vector3 headOffset = new Vector3(0f, 1.7f, 0.2f);
    public float lookSensitivity = 2.5f;
    public float pitchLimit = 75f;

    [Header("Movement")]
    public float moveSpeedMultiplier = 1.1f;

    private GameObject possessed;
    private NavMeshAgent agent;
    private Animator animator;
    private AttackController attackController;
    private Transform headAnchor;

    private Camera cam;
    private Transform camOriginalParent;
    private Vector3 camOriginalPosition;
    private Quaternion camOriginalRotation;
    private RTSCameraController rtsCamera;

    private float yaw;
    private float pitch;

    /// <summary>Parent your XR Origin here for VR instead of the desktop camera.</summary>
    public Transform HeadAnchor => headAnchor;
    public GameObject PossessedUnit => possessed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("EmbodimentController");
        Instance = go.AddComponent<EmbodimentController>();
        DontDestroyOnLoad(go);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (possessed == null)
        {
            // Not embodied: H with exactly one unit selected dives in.
            if (Input.GetKeyDown(possessKey)) TryPossessSelected();
            return;
        }

        // The unit died under us, or the player wants out.
        if (Input.GetKeyDown(possessKey) || Input.GetKeyDown(KeyCode.Escape))
        {
            Release();
            return;
        }

        DriveMovement();
        DriveLook();
        DriveAttack();
    }

    private void LateUpdate()
    {
        // Unit destroyed while embodied - eject cleanly.
        if (possessed == null && cam != null && cam.transform.parent == null)
        {
            return;
        }
        if (Instance == this && possessed == null && headAnchor != null)
        {
            Release();
        }
    }

    // ------------------------------------------------------------------

    private void TryPossessSelected()
    {
        if (UnitSelectionManager.Instance == null) return;
        var selected = UnitSelectionManager.Instance.selectedUnitsList;
        if (selected.Count != 1 || selected[0] == null) return;

        GameObject unit = selected[0];
        if (!FactionUtility.IsLocallyControlled(unit)) return;
        if (unit.GetComponent<NavMeshAgent>() == null) return;

        Possess(unit);
    }

    public void Possess(GameObject unit)
    {
        possessed = unit;
        agent = unit.GetComponent<NavMeshAgent>();
        animator = unit.GetComponent<Animator>();
        attackController = unit.GetComponent<AttackController>();

        // Head anchor: where the camera (or XR Origin) lives.
        headAnchor = new GameObject("HeadAnchor").transform;
        headAnchor.SetParent(unit.transform, false);
        headAnchor.localPosition = headOffset;

        cam = Camera.main;
        if (cam != null)
        {
            camOriginalParent = cam.transform.parent;
            camOriginalPosition = cam.transform.position;
            camOriginalRotation = cam.transform.rotation;
            cam.transform.SetParent(headAnchor, false);
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;

            rtsCamera = cam.GetComponentInParent<RTSCameraController>();
            if (rtsCamera == null) rtsCamera = FindFirstObjectByType<RTSCameraController>();
            if (rtsCamera != null) rtsCamera.enabled = false;
        }

        if (agent != null)
        {
            agent.ResetPath();
            agent.updateRotation = false;
        }
        UnitMovement movement = unit.GetComponent<UnitMovement>();
        if (movement != null) movement.enabled = false;

        yaw = unit.transform.eulerAngles.y;
        pitch = 0f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log($"Embodied {unit.name}! WASD to move, mouse to look, click/Space to attack, H to return.");
    }

    public void Release()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (cam != null)
        {
            cam.transform.SetParent(camOriginalParent, false);
            cam.transform.position = camOriginalPosition;
            cam.transform.rotation = camOriginalRotation;
        }
        if (rtsCamera != null) rtsCamera.enabled = true;

        if (agent != null && agent.enabled)
        {
            agent.updateRotation = true;
        }

        if (headAnchor != null) Destroy(headAnchor.gameObject);
        headAnchor = null;
        possessed = null;
        agent = null;
        animator = null;
        attackController = null;
    }

    // ------------------------------------------------------------------

    private void DriveMovement()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        float forward = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
        float strafe = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);

        Vector3 input = new Vector3(strafe, 0f, forward);
        bool moving = input.sqrMagnitude > 0.01f;

        if (moving)
        {
            Vector3 direction = Quaternion.Euler(0f, yaw, 0f) * input.normalized;
            agent.Move(direction * agent.speed * moveSpeedMultiplier * Time.deltaTime);
        }

        // Body faces where you look; animator reflects your feet.
        possessed.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (animator != null) animator.SetBool("isMoving", moving);
    }

    private void DriveLook()
    {
        yaw += Input.GetAxis("Mouse X") * lookSensitivity;
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * lookSensitivity, -pitchLimit, pitchLimit);
        if (headAnchor != null) headAnchor.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void DriveAttack()
    {
        if (attackController == null) return;
        if (!Input.GetMouseButtonDown(0) && !Input.GetKeyDown(KeyCode.Space)) return;

        // Attack the nearest hostile roughly in front of us.
        Unit best = null;
        float bestScore = Mathf.Infinity;
        Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;

        foreach (Collider hit in Physics.OverlapSphere(
                     possessed.transform.position, attackController.detectionRadius))
        {
            Unit other = hit.GetComponentInParent<Unit>();
            if (other == null || other.gameObject == possessed) continue;
            if (!FactionUtility.AreHostile(possessed, other.gameObject)) continue;

            Vector3 to = other.transform.position - possessed.transform.position;
            float distance = to.magnitude;
            float angle = Vector3.Angle(forward, to);
            float score = distance + angle * 0.2f; // prefer close AND in front

            if (score < bestScore)
            {
                bestScore = score;
                best = other;
            }
        }

        if (best != null)
        {
            attackController.targetToAttack = best.transform;
        }
    }
}
