using UnityEngine;
using TMPro;

/// <summary>
/// Shows the local player's population as "23 / 45" next to the silver
/// counter. Turns red at the cap. Add to the HUD canvas and assign a label.
/// </summary>
public class PopulationHUD : MonoBehaviour
{
    public TextMeshProUGUI populationLabel;
    public float refreshInterval = 0.5f;
    public Color normalColor = Color.white;
    public Color cappedColor = new Color(1f, 0.35f, 0.3f);

    private float timer;

    private void Update()
    {
        if (populationLabel == null) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = refreshInterval;

        int factionId = FactionManager.LocalPlayerFactionId;
        int population = PopulationManager.GetPopulation(factionId);
        int cap = PopulationManager.GetCap(factionId);

        populationLabel.text = $"{population} / {cap}";
        populationLabel.color = population >= cap ? cappedColor : normalColor;
    }
}
