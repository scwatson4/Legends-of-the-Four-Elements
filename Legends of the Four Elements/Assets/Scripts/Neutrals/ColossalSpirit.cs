using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A colossal ancient spirit sleeping on the battlefield - and only an
/// AVATAR can wake it, by merging with it (Korra against Unavaatu, Aang and
/// the ocean spirit). Right-click it with your Avatar selected: the Avatar
/// walks over and channels; when the merge completes, the Avatar's body is
/// drawn inside and the colossus rises under YOUR banner as a devastating
/// giant for a limited time. When the time ends (or the colossus falls),
/// the Avatar steps back out and the spirit returns to its slumber.
///
/// Prefab recipe: giant model + collider + NavMeshAgent (large radius) +
/// Unit (huge HP, type Spirit, category Avatar-sized populationCost 0 - it
/// costs no supply, it is the map's prize) + AttackController (big damage,
/// trigger collider) + this. Dormant it is invulnerable and untargetable.
/// One per large map (MapGenerator can place it).
/// </summary>
[RequireComponent(typeof(Unit))]
public class ColossalSpirit : MonoBehaviour, IDamageInterceptor
{
    [Header("Awakening")]
    public float channelSeconds = 10f;
    public float channelRadius = 8f;
    [Tooltip("How long the merged colossus fights before the Avatar is released.")]
    public float awakenedSeconds = 60f;
    public float cooldownSeconds = 120f;

    [Header("After Awakening")]
    public string awakenedLayerName = "Clickable";

    public bool IsAwakened { get; private set; }

    private Unit unit;
    private AvatarUnit channelingAvatar;
    private float channelProgress;
    private float awakenedRemaining;
    private float cooldownRemaining;
    private GameObject mergedAvatar;
    private int originalLayer;
    private EnemyAI brain;

    private void Start()
    {
        unit = GetComponent<Unit>();
        originalLayer = gameObject.layer;
        FactionUtility.SetFaction(gameObject, FactionManager.NoFaction);

        MinimapPOI poi = MinimapPOI.Ensure(gameObject, MinimapPOI.POIType.Custom);
        poi.colorOverride = new Color(0.4f, 1f, 0.9f); // ancient teal
    }

    /// <summary>Dormant = untouchable; awakened = a fair (huge) target.</summary>
    public int ModifyIncomingDamage(int damage) => IsAwakened ? damage : 0;

    /// <summary>Right-click order from the selection manager (Avatars only).</summary>
    public bool OrderChannel(AvatarUnit avatar)
    {
        if (avatar == null || IsAwakened || cooldownRemaining > 0f) return false;

        channelingAvatar = avatar;

        NavMeshAgent agent = avatar.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(transform.position);
        }
        Debug.Log($"{avatar.name} approaches the sleeping colossus...");
        return true;
    }

    private void Update()
    {
        cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);

        if (IsAwakened)
        {
            awakenedRemaining -= Time.deltaTime;
            if (awakenedRemaining <= 0f || unit.CurrentHealth <= 0f)
            {
                ReleaseAvatar();
            }
            return;
        }

        // Channeling: the Avatar must stand in the spirit's shadow.
        if (channelingAvatar == null)
        {
            channelProgress = Mathf.Max(0f, channelProgress - Time.deltaTime);
            return;
        }

        float distance = Vector3.Distance(channelingAvatar.transform.position, transform.position);
        if (distance <= channelRadius)
        {
            channelProgress += Time.deltaTime;
            if (channelProgress >= channelSeconds)
            {
                Awaken(channelingAvatar);
            }
        }
    }

    // ------------------------------------------------------------------

    private void Awaken(AvatarUnit avatar)
    {
        IsAwakened = true;
        channelProgress = 0f;
        channelingAvatar = null;

        int factionId = FactionUtility.GetFactionId(avatar.gameObject);

        // The Avatar's body is drawn inside the spirit.
        mergedAvatar = avatar.gameObject;
        if (UnitSelectionManager.Instance != null)
        {
            UnitSelectionManager.Instance.OnUnitDestroyed(mergedAvatar); // deselect/delist while merged
        }
        mergedAvatar.SetActive(false);

        // The colossus rises under the Avatar's banner, at full vigor.
        FactionUtility.SetFaction(gameObject, factionId);
        unit.Heal(Mathf.CeilToInt(unit.maxUnitHealth));
        awakenedRemaining = awakenedSeconds;

        int layer = LayerMask.NameToLayer(awakenedLayerName);
        if (layer >= 0 && FactionUtility.IsLocallyControlled(gameObject))
        {
            gameObject.layer = layer; // selectable/orderable by its awakener
        }

        UnitMovement movement = GetComponent<UnitMovement>();
        if (movement == null) movement = gameObject.AddComponent<UnitMovement>();
        movement.enabled = false; // selection enables it

        if (FactionManager.IsAIControlled(factionId))
        {
            if (brain == null) brain = gameObject.AddComponent<EnemyAI>();
            brain.enabled = true;
            brain.chaseCommandCenters = true;
        }

        GameFeel.Shake(0.8f, 1.5f);
        if (SoundManager.Instance != null) SoundManager.Instance.StartColossusDirge();
        Debug.Log($"THE COLOSSUS AWAKENS - the Avatar walks as a giant for {awakenedSeconds:0}s!");
    }

    private void ReleaseAvatar()
    {
        IsAwakened = false;
        cooldownRemaining = cooldownSeconds;
        if (SoundManager.Instance != null) SoundManager.Instance.StopColossusDirge();

        // The Avatar steps out of the fading giant.
        if (mergedAvatar != null)
        {
            Vector3 exit = transform.position + transform.forward * 4f;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(exit, out hit, 10f, NavMesh.AllAreas)) exit = hit.position;

            mergedAvatar.transform.position = exit;
            mergedAvatar.SetActive(true);
            NavMeshAgent avatarAgent = mergedAvatar.GetComponent<NavMeshAgent>();
            if (avatarAgent != null && avatarAgent.enabled) avatarAgent.Warp(exit);

            if (UnitSelectionManager.Instance != null &&
                !UnitSelectionManager.Instance.allUnitsList.Contains(mergedAvatar))
            {
                UnitSelectionManager.Instance.allUnitsList.Add(mergedAvatar);
            }
            mergedAvatar = null;
        }

        // Back to slumber: neutral, untouchable, unmoving.
        if (UnitSelectionManager.Instance != null)
        {
            UnitSelectionManager.Instance.OnUnitDestroyed(gameObject); // deselect if selected
        }
        FactionUtility.SetFaction(gameObject, FactionManager.NoFaction);
        gameObject.layer = originalLayer;

        AttackController attack = GetComponent<AttackController>();
        if (attack != null) attack.targetToAttack = null;
        if (brain != null) brain.enabled = false;

        UnitMovement movement = GetComponent<UnitMovement>();
        if (movement != null) movement.enabled = false;
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.ResetPath();

        unit.Heal(Mathf.CeilToInt(unit.maxUnitHealth)); // the slumber restores it
        Debug.Log("The colossus returns to its slumber.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, channelRadius);
    }
}
