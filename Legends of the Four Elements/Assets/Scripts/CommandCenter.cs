using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandCenter : MonoBehaviour
{
    public Team team;
    private float structureHealth;
    public float maxStructureHealth = 1000f;
    public GameObject CommandCenterModel;
    public HealthTracker healthTracker;

    void Start()
    {
        structureHealth = maxStructureHealth;
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        healthTracker.UpdateSliderValue(structureHealth, maxStructureHealth);

        if (structureHealth <= 0)
        {
            SoundManager.Instance.PlayStructureDestructionSound();

            if (team == Team.Player)
            {
                if (GameManager.Instance != null)
                    StartCoroutine(TriggerGameOver());
                else
                    Debug.LogError("GameManager.Instance is null! Cannot trigger Game Over.");
            }
            else if (team == Team.Enemy)
            {
                if (GameManager.Instance != null)
                    StartCoroutine(TriggerWin());
                else
                    Debug.LogError("GameManager.Instance is null! Cannot trigger Win.");
            }
            Destroy(CommandCenterModel);
            Destroy(gameObject, 1f);
        }
    }

    public void TakeDamage(int damageToInflict)
    {
        structureHealth -= damageToInflict;
        UpdateHealthUI();
    }

    private IEnumerator TriggerGameOver()
    {
        yield return new WaitForEndOfFrame();
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerCommandCenterDestroyed();
        else
            Debug.LogError("GameManager.Instance is null during TriggerGameOver!");
    }

    private IEnumerator TriggerWin()
    {
        yield return new WaitForEndOfFrame();
        if (GameManager.Instance != null)
            GameManager.Instance.OnEnemyCommandCenterDestroyed();
        else
            Debug.LogError("GameManager.Instance is null during TriggerWin!");
    }
}