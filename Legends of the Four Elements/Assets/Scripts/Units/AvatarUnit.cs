using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The Avatar - each player's unique hero unit.
///  - Bends all four elements: press the cycle key (default T) while selected
///    to switch Air -> Water -> Earth -> Fire (swaps the attack VFX).
///  - Avatar State (default G): a short massive damage/speed surge with a
///    long cooldown.
///  - The ONLY unit capable of energy bending, and therefore the only unit
///    that can tame wild spirits (Tameable checks for this component).
///  - One per faction: UnitSpawner refuses to build a second while one lives.
/// Put this on the Avatar prefab (category = Avatar in the NationData roster).
/// </summary>
[RequireComponent(typeof(Unit))]
public class AvatarUnit : MonoBehaviour
{
    [Header("Energy Bending")]
    public bool canEnergyBend = true;

    [Header("Element Bending")]
    public Nation currentElement = Nation.Air;
    public KeyCode cycleElementKey = KeyCode.T;
    [Tooltip("Attack VFX per element, swapped into AttackController.flamethrowerEffect.")]
    public GameObject airEffect;
    public GameObject waterEffect;
    public GameObject earthEffect;
    public GameObject fireEffect;

    [Header("Avatar State")]
    public KeyCode avatarStateKey = KeyCode.G;
    public float avatarStateDuration = 10f;
    public float avatarStateCooldown = 60f;
    public float avatarStateDamageMultiplier = 2f;
    public float avatarStateSpeedMultiplier = 1.5f;
    [Tooltip("Optional glow VFX while the Avatar State is active.")]
    public GameObject avatarStateAura;

    // One living Avatar per faction.
    private static readonly Dictionary<int, AvatarUnit> aliveByFaction = new Dictionary<int, AvatarUnit>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => aliveByFaction.Clear();

    public static bool FactionHasAvatar(int factionId) =>
        aliveByFaction.TryGetValue(factionId, out AvatarUnit avatar) && avatar != null;

    private Unit unit;
    private AttackController attackController;
    private NavMeshAgent agent;
    private float cooldownRemaining;
    private bool avatarStateActive;
    private int registeredFactionId = FactionManager.NoFaction;

    private void Start()
    {
        unit = GetComponent<Unit>();
        attackController = GetComponent<AttackController>();
        agent = GetComponent<NavMeshAgent>();

        registeredFactionId = unit.FactionId;
        aliveByFaction[registeredFactionId] = this;

        ApplyElement(currentElement);
        if (avatarStateAura != null) avatarStateAura.SetActive(false);
    }

    private void OnDestroy()
    {
        if (registeredFactionId != FactionManager.NoFaction &&
            aliveByFaction.TryGetValue(registeredFactionId, out AvatarUnit avatar) && avatar == this)
        {
            aliveByFaction.Remove(registeredFactionId);
        }
    }

    private void Update()
    {
        cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);

        // Hotkeys only act on the locally selected Avatar.
        if (!IsSelectedLocally()) return;

        if (Input.GetKeyDown(cycleElementKey))
        {
            CycleElement();
        }
        if (Input.GetKeyDown(avatarStateKey) && !avatarStateActive && cooldownRemaining <= 0f)
        {
            StartCoroutine(AvatarStateRoutine());
        }
    }

    private bool IsSelectedLocally()
    {
        return UnitSelectionManager.Instance != null &&
               UnitSelectionManager.Instance.selectedUnitsList.Contains(gameObject) &&
               FactionUtility.IsLocallyControlled(gameObject);
    }

    // ------------------------------------------------------------------
    // Element bending
    // ------------------------------------------------------------------

    public void CycleElement()
    {
        Nation next;
        switch (currentElement)
        {
            case Nation.Air: next = Nation.Water; break;
            case Nation.Water: next = Nation.Earth; break;
            case Nation.Earth: next = Nation.Fire; break;
            default: next = Nation.Air; break;
        }
        ApplyElement(next);
    }

    public void ApplyElement(Nation element)
    {
        currentElement = element;

        SetEffectActive(airEffect, false);
        SetEffectActive(waterEffect, false);
        SetEffectActive(earthEffect, false);
        SetEffectActive(fireEffect, false);

        GameObject chosen = GetEffect(element);
        if (attackController != null && chosen != null)
        {
            // The attack state machine toggles flamethrowerEffect on/off,
            // so pointing it at the element's VFX is all we need.
            attackController.flamethrowerEffect = chosen;
        }

        Debug.Log($"Avatar now bending {element}.");
    }

    private GameObject GetEffect(Nation element)
    {
        switch (element)
        {
            case Nation.Air: return airEffect;
            case Nation.Water: return waterEffect;
            case Nation.Earth: return earthEffect;
            case Nation.Fire: return fireEffect;
            default: return airEffect;
        }
    }

    private static void SetEffectActive(GameObject effect, bool active)
    {
        if (effect != null) effect.SetActive(active);
    }

    // ------------------------------------------------------------------
    // Avatar State
    // ------------------------------------------------------------------

    private IEnumerator AvatarStateRoutine()
    {
        avatarStateActive = true;
        cooldownRemaining = avatarStateCooldown;

        int baseDamage = attackController != null ? attackController.unitDamage : 0;
        float baseSpeed = agent != null ? agent.speed : 0f;

        if (attackController != null)
            attackController.unitDamage = Mathf.RoundToInt(baseDamage * avatarStateDamageMultiplier);
        if (agent != null)
            agent.speed = baseSpeed * avatarStateSpeedMultiplier;
        if (avatarStateAura != null) avatarStateAura.SetActive(true);

        Debug.Log("AVATAR STATE!");
        yield return new WaitForSeconds(avatarStateDuration);

        if (attackController != null) attackController.unitDamage = baseDamage;
        if (agent != null) agent.speed = baseSpeed;
        if (avatarStateAura != null) avatarStateAura.SetActive(false);

        avatarStateActive = false;
    }
}
