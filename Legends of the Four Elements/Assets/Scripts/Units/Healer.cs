using UnityEngine;

/// <summary>
/// Waterbending healer: periodically restores health to the most wounded
/// friendly unit in range (like Katara's healing). Put on the Water Tribe
/// Healer prefab; also usable for any support unit.
/// </summary>
[RequireComponent(typeof(Unit))]
public class Healer : MonoBehaviour
{
    public float healRadius = 8f;
    public int healAmount = 5;
    public float healInterval = 1f;
    [Tooltip("Optional water-glow VFX toggled while actively healing someone.")]
    public GameObject healEffect;

    private Unit self;
    private float timer;

    private void Start()
    {
        self = GetComponent<Unit>();
        if (healEffect != null) healEffect.SetActive(false);
    }

    private void Update()
    {
        if (NetworkGuard.BlockLocalSimulation) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = healInterval;

        Unit patient = FindMostWoundedAlly();
        if (healEffect != null) healEffect.SetActive(patient != null);
        if (patient != null)
        {
            patient.Heal(healAmount);
            if (TutorialSignals.IsPlayerAction(gameObject)) TutorialSignals.HealsDone++;
        }
    }

    private Unit FindMostWoundedAlly()
    {
        int myFaction = FactionUtility.GetFactionId(gameObject);
        Unit best = null;
        float lowestFraction = 0.999f; // only heal the actually-wounded

        foreach (Collider hit in Physics.OverlapSphere(transform.position, healRadius))
        {
            Unit other = hit.GetComponentInParent<Unit>();
            if (other == null || other == self) continue;
            if (FactionUtility.GetFactionId(other.gameObject) != myFaction) continue;

            if (other.HealthFraction < lowestFraction)
            {
                lowestFraction = other.HealthFraction;
                best = other;
            }
        }
        return best;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, healRadius);
    }
}
