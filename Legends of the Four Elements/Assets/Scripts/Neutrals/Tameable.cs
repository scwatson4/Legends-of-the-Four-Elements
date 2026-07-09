using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Lets spirits (or any creature) be befriended/tamed. Select your units and
/// right-click the creature: your units walk to it and channel; once enough
/// time in range accumulates, it joins your faction.
///
/// Friendly spirits can be tamed at full health. For dark spirits, set
/// maxHealthFractionToTame to e.g. 0.5 so they must be weakened in battle
/// before they can be turned.
/// </summary>
[RequireComponent(typeof(Unit))]
public class Tameable : MonoBehaviour
{
    [Header("Taming Rules")]
    public float tameDuration = 6f;
    [Range(0.05f, 1f)]
    [Tooltip("1 = tameable at any health. 0.5 = must be below half health first.")]
    public float maxHealthFractionToTame = 1f;
    public float tameRadius = 4f;

    [Header("After Taming")]
    [Tooltip("Layer used by your selectable player units, so the tamed being can be selected.")]
    public string tamedLayerName = "Clickable";
    public UnityEvent onTamed;

    public bool IsTamed { get; private set; }
    public float Progress01 => Mathf.Clamp01(progress / tameDuration);

    private readonly List<GameObject> tamers = new List<GameObject>();
    private float progress;
    private Unit unit;

    private void Start()
    {
        unit = GetComponent<Unit>();
    }

    public bool IsTameableNow()
    {
        if (IsTamed || unit == null) return false;
        return unit.HealthFraction <= maxHealthFractionToTame + 0.0001f;
    }

    /// <summary>Orders a unit to walk over and start channeling.</summary>
    public void OrderTame(GameObject tamer)
    {
        if (IsTamed || tamer == null) return;
        if (!tamers.Contains(tamer)) tamers.Add(tamer);

        AttackController attack = tamer.GetComponent<AttackController>();
        if (attack != null) attack.targetToAttack = null;

        NavMeshAgent agent = tamer.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(transform.position);
        }
    }

    private void Update()
    {
        if (IsTamed) return;

        tamers.RemoveAll(t => t == null);
        if (tamers.Count == 0)
        {
            // Bond fades while nobody is channeling.
            progress = Mathf.Max(0f, progress - Time.deltaTime * 0.5f);
            return;
        }

        if (!IsTameableNow()) return;

        int channeling = 0;
        int tamingFaction = FactionManager.NoFaction;
        foreach (GameObject tamer in tamers)
        {
            if (Vector3.Distance(tamer.transform.position, transform.position) <= tameRadius)
            {
                channeling++;
                tamingFaction = FactionUtility.GetFactionId(tamer);
            }
        }

        if (channeling == 0) return;

        // Extra tamers speed things up a little (diminishing returns).
        progress += Time.deltaTime * (1f + (channeling - 1) * 0.5f);
        if (progress >= tameDuration && tamingFaction != FactionManager.NoFaction)
        {
            CompleteTame(tamingFaction);
        }
    }

    private void CompleteTame(int newFactionId)
    {
        IsTamed = true;
        tamers.Clear();

        FactionUtility.SetFaction(gameObject, newFactionId);

        // Move to the selectable layer so the new owner can click it.
        int layer = LayerMask.NameToLayer(tamedLayerName);
        if (layer >= 0) SetLayerRecursively(gameObject, layer);

        Spirit spirit = GetComponent<Spirit>();
        if (spirit != null) spirit.OnTamed(newFactionId);

        onTamed?.Invoke();
        Debug.Log($"{gameObject.name} was befriended by faction {newFactionId}!");
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, tameRadius);
    }
}
