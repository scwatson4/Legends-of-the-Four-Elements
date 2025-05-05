using UnityEngine;

public class CommandCenter : MonoBehaviour
{
    public float maxHealth = 500f;
    private float health;
    public HealthTracker healthTracker;
    public GameObject CommandCenterModel;
    public Team team = Team.Player; // Team affiliation

    void Start()
    {
        health = maxHealth;
        UpdateHealthUI();
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        UpdateHealthUI();

        if (health <= 0)
        {
            // Play destruction animation or effect if available
            SoundManager.Instance.PlayStructureDestructionSound();
            Destroy(CommandCenterModel, 1f); // Delay for effect
            Destroy(gameObject);
        }
    }

    private void UpdateHealthUI()
    {
        if (healthTracker != null)
        {
            healthTracker.UpdateSliderValue(health, maxHealth);
        }
    }
}