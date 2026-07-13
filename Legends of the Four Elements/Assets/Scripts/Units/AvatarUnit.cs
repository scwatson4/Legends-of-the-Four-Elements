using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The Avatar - each player's unique hero unit.
///
/// ELEMENTS: press T while selected to cycle Air -> Water -> Earth -> Fire
/// (swaps the attack VFX and the biome affinity).
///
/// ENERGY + AVATAR STATE: energy charges over time (faster in combat).
/// Spend it on three tiers of the Avatar State - while it lasts, the Avatar
/// commands ALL FOUR elements at once (every element VFX blazes, heavy
/// damage boost, incoming damage reduced):
///     G          Quick surge    5s   costs 30 energy   ~1.75x damage
///     Shift+G    Long surge    10s   costs 55 energy   ~2.25x damage
///     Ctrl+G     ULTIMATE      20s   costs 100 energy  ~3x damage
///
/// AUTONOMOUS: when fighting on its own (AI-owned, or yours but not being
/// micromanaged), the Avatar alternates elements between attacks and
/// occasionally drops into a defensive stance that halves incoming damage.
///
/// Also the ONLY unit capable of energy bending (taming wild spirits), and
/// one per faction - UnitSpawner refuses to build a second while one lives.
/// </summary>
[RequireComponent(typeof(Unit))]
public class AvatarUnit : MonoBehaviour, IDamageInterceptor
{
    [Header("Energy Bending")]
    public bool canEnergyBend = true;

    [Tooltip("Arrive in the style of the origin nation: bison descent, dragon " +
             "flight, wave ride, or erupting from the earth.")]
    public bool ceremonialArrival = true;

    [Header("Element Bending")]
    public Nation currentElement = Nation.Air;
    public KeyCode cycleElementKey = KeyCode.T;
    [Tooltip("Attack VFX per element, swapped into AttackController.flamethrowerEffect.")]
    public GameObject airEffect;
    public GameObject waterEffect;
    public GameObject earthEffect;
    public GameObject fireEffect;

    [Header("Energy")]
    public float maxEnergy = 100f;
    public float energyRegenPerSecond = 2.5f;
    [Tooltip("Extra regen per second while fighting - battle feeds the spirit.")]
    public float combatRegenBonus = 2.5f;

    [Header("Avatar State (G / Shift+G / Ctrl+G)")]
    public KeyCode avatarStateKey = KeyCode.G;
    public float quickCost = 30f, quickDuration = 5f, quickDamageMult = 1.75f;
    public float longCost = 55f, longDuration = 10f, longDamageMult = 2.25f;
    public float ultimateCost = 100f, ultimateDuration = 20f, ultimateDamageMult = 3f;
    [Tooltip("Incoming damage is multiplied by this while the state is active.")]
    [Range(0.1f, 1f)] public float stateDamageTakenMultiplier = 0.6f;
    public float stateSpeedMultiplier = 1.5f;
    [Tooltip("Optional glow VFX while the Avatar State is active.")]
    public GameObject avatarStateAura;

    [Header("Autonomous Behaviour")]
    [Tooltip("Seconds between autonomous decisions while fighting unsupervised.")]
    public float autoDecisionInterval = 4f;
    public float defensiveStanceDuration = 2.5f;
    [Range(0.1f, 1f)] public float defensiveDamageTakenMultiplier = 0.5f;

    // One living Avatar per faction.
    private static readonly Dictionary<int, AvatarUnit> aliveByFaction = new Dictionary<int, AvatarUnit>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => aliveByFaction.Clear();

    public static bool FactionHasAvatar(int factionId) =>
        aliveByFaction.TryGetValue(factionId, out AvatarUnit avatar) && avatar != null;

    public float CurrentEnergy { get; private set; }
    public float Energy01 => maxEnergy > 0f ? CurrentEnergy / maxEnergy : 0f;
    public bool AvatarStateActive { get; private set; }

    private Unit unit;
    private AttackController attackController;
    private NavMeshAgent agent;
    private int registeredFactionId = FactionManager.NoFaction;
    private float autoTimer;
    private float defensiveRemaining;

    private void Start()
    {
        unit = GetComponent<Unit>();
        attackController = GetComponent<AttackController>();
        agent = GetComponent<NavMeshAgent>();

        registeredFactionId = unit.FactionId;
        aliveByFaction[registeredFactionId] = this;

        CurrentEnergy = maxEnergy * 0.3f; // arrive with a spark, not a full tank
        ApplyElement(currentElement);
        if (avatarStateAura != null) avatarStateAura.SetActive(false);

        // A dignified entrance: bison, dragon, wave, or the earth itself.
        AvatarArrival.Play(this);
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
        // Energy charges over time, faster while fighting.
        bool inCombat = attackController != null && attackController.targetToAttack != null;
        float regen = energyRegenPerSecond + (inCombat ? combatRegenBonus : 0f);
        CurrentEnergy = Mathf.Min(maxEnergy, CurrentEnergy + regen * Time.deltaTime);

        defensiveRemaining = Mathf.Max(0f, defensiveRemaining - Time.deltaTime);

        if (IsPlayerDriven())
        {
            HandlePlayerInput();
        }
        else
        {
            UpdateAutonomous(inCombat);
        }
    }

    /// <summary>Player is actively steering: selected, or embodied (VR/hero mode).</summary>
    private bool IsPlayerDriven()
    {
        if (!FactionUtility.IsLocallyControlled(gameObject)) return false;

        if (EmbodimentController.IsActive &&
            EmbodimentController.Instance.PossessedUnit == gameObject) return true;

        return UnitSelectionManager.Instance != null &&
               UnitSelectionManager.Instance.selectedUnitsList.Contains(gameObject);
    }

    private void HandlePlayerInput()
    {
        if (Input.GetKeyDown(cycleElementKey))
        {
            CycleElement();
        }

        if (Input.GetKeyDown(avatarStateKey) && !AvatarStateActive)
        {
            bool ultimate = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool longSurge = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (ultimate) TryEnterAvatarState(ultimateCost, ultimateDuration, ultimateDamageMult, "ULTIMATE");
            else if (longSurge) TryEnterAvatarState(longCost, longDuration, longDamageMult, "long surge");
            else TryEnterAvatarState(quickCost, quickDuration, quickDamageMult, "quick surge");
        }
    }

    /// <summary>Unsupervised combat: alternate elements, sometimes turtle up.</summary>
    private void UpdateAutonomous(bool inCombat)
    {
        if (!inCombat || AvatarStateActive) return;

        autoTimer -= Time.deltaTime;
        if (autoTimer > 0f) return;
        autoTimer = autoDecisionInterval;

        if (Random.value < 0.7f)
        {
            CycleElement(); // vary the offense
        }
        else
        {
            defensiveRemaining = defensiveStanceDuration; // brace behind the elements
        }
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
        if (AvatarStateActive) return; // all four stay lit during the state

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

    private void TryEnterAvatarState(float cost, float duration, float damageMult, string label)
    {
        if (CurrentEnergy < cost)
        {
            Debug.Log($"Not enough energy for the {label} ({Mathf.RoundToInt(CurrentEnergy)}/{Mathf.RoundToInt(cost)}).");
            return;
        }

        CurrentEnergy -= cost;
        StartCoroutine(AvatarStateRoutine(duration, damageMult, label));
    }

    private IEnumerator AvatarStateRoutine(float duration, float damageMult, string label)
    {
        AvatarStateActive = true;

        int baseDamage = attackController != null ? attackController.unitDamage : 0;
        float baseSpeed = agent != null ? agent.speed : 0f;

        if (attackController != null)
            attackController.unitDamage = Mathf.RoundToInt(baseDamage * damageMult);
        if (agent != null)
            agent.speed = baseSpeed * stateSpeedMultiplier;

        // ALL FOUR ELEMENTS AT ONCE.
        SetEffectActive(airEffect, true);
        SetEffectActive(waterEffect, true);
        SetEffectActive(earthEffect, true);
        SetEffectActive(fireEffect, true);
        if (avatarStateAura != null) avatarStateAura.SetActive(true);

        Debug.Log($"AVATAR STATE ({label}) - {duration}s of all four elements!");
        yield return new WaitForSeconds(duration);

        if (attackController != null) attackController.unitDamage = baseDamage;
        if (agent != null) agent.speed = baseSpeed;
        if (avatarStateAura != null) avatarStateAura.SetActive(false);

        AvatarStateActive = false;
        ApplyElement(currentElement); // back to one element lit
    }

    // ------------------------------------------------------------------
    // Damage mitigation (Avatar State shield + autonomous defensive stance)
    // ------------------------------------------------------------------

    public int ModifyIncomingDamage(int damage)
    {
        float multiplier = 1f;
        if (AvatarStateActive) multiplier *= stateDamageTakenMultiplier;
        if (defensiveRemaining > 0f) multiplier *= defensiveDamageTakenMultiplier;
        return Mathf.Max(1, Mathf.RoundToInt(damage * multiplier));
    }
}
