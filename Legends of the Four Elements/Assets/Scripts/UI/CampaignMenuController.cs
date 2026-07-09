using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The campaign screen in the main menu: lists all five chapters and 25
/// levels (locked/unlocked/completed), shows chi and redeemed Avatars, sells
/// permanent upgrades, and launches levels through CampaignManager.
///
/// Wiring (see docs/CAMPAIGN.md): a ScrollView whose Content is
/// `listParent`, one Button prefab with a TextMeshProUGUI child as
/// `levelButtonPrefab`, labels, and optional nation/upgrade buttons.
/// The level list populates itself - no per-level wiring.
/// </summary>
public class CampaignMenuController : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Scene used by levels that don't specify their own (a MapGenerator scene works great).")]
    public string defaultLevelSceneName = "Level1";

    [Header("Level List")]
    public Transform listParent;
    public Button levelButtonPrefab;

    [Header("Labels (optional)")]
    public TextMeshProUGUI chiLabel;
    public TextMeshProUGUI redeemedLabel;
    public TextMeshProUGUI selectedInfoLabel;

    [Header("Player")]
    public Nation playerNation = Nation.Air;

    private List<CampaignChapter> chapters;
    private CampaignProgress progress;
    private CampaignLevel selectedLevel;

    private void OnEnable()
    {
        chapters = DefaultCampaign.Build();
        progress = CampaignProgress.Load();
        RebuildList();
        RefreshLabels();
    }

    // ------------------------------------------------------------------
    // UI hooks
    // ------------------------------------------------------------------

    /// <summary>Optional nation buttons: 0=Air..3=Fire.</summary>
    public void SelectNation(int nationIndex)
    {
        playerNation = (Nation)Mathf.Clamp(nationIndex, 0, 3);
        RefreshLabels();
    }

    /// <summary>Wire the Play/Start button to this.</summary>
    public void PlaySelectedLevel()
    {
        if (selectedLevel == null)
        {
            Debug.Log("[Campaign] Pick a level first.");
            return;
        }
        CampaignManager.LaunchLevel(selectedLevel, playerNation, defaultLevelSceneName);
    }

    /// <summary>Chi shop: buy the next level of the player's nation upgrade
    /// at `upgradeIndex` (index into NationData.upgrades). Cost = silver cost
    /// of that level, paid in chi. Permanent across the whole campaign.</summary>
    public void BuyPermanentUpgrade(int upgradeIndex)
    {
        NationDatabase db = NationDatabase.Load();
        NationData data = db != null ? db.Get(playerNation) : null;
        if (data == null || data.upgrades == null ||
            upgradeIndex < 0 || upgradeIndex >= data.upgrades.Length) return;

        UpgradeData upgrade = data.upgrades[upgradeIndex];
        int currentLevel = progress.GetPermanentUpgradeLevel(upgrade.upgradeId);
        if (currentLevel >= upgrade.maxLevel)
        {
            SetInfo($"{upgrade.displayName} is already mastered.");
            return;
        }

        int cost = upgrade.CostForLevel(currentLevel + 1);
        if (progress.chi < cost)
        {
            SetInfo($"Not enough chi for {upgrade.displayName} ({cost} needed, {progress.chi} held).");
            return;
        }

        progress.chi -= cost;
        progress.SetPermanentUpgradeLevel(upgrade.upgradeId, currentLevel + 1);
        SetInfo($"{upgrade.displayName} → permanent level {currentLevel + 1}!");
        RefreshLabels();
    }

    /// <summary>Wire to a (confirm-protected!) reset button.</summary>
    public void ResetCampaign()
    {
        CampaignProgress.ResetAll();
        progress = CampaignProgress.Load();
        RebuildList();
        RefreshLabels();
        SetInfo("Campaign progress reset.");
    }

    // ------------------------------------------------------------------

    private void RebuildList()
    {
        if (listParent == null || levelButtonPrefab == null)
        {
            Debug.LogWarning("CampaignMenuController: assign List Parent and Level Button Prefab " +
                             "to show the level list (see docs/CAMPAIGN.md).");
            return;
        }

        foreach (Transform child in listParent)
        {
            Destroy(child.gameObject);
        }

        for (int c = 0; c < chapters.Count; c++)
        {
            CampaignChapter chapter = chapters[c];

            Button header = Instantiate(levelButtonPrefab, listParent);
            header.interactable = false;
            SetButtonText(header, chapter.title);

            for (int l = 0; l < chapter.levels.Count; l++)
            {
                CampaignLevel level = chapter.levels[l];
                bool unlocked = progress.IsLevelUnlocked(chapters, c, l);
                bool completed = progress.IsLevelCompleted(level.id);

                Button button = Instantiate(levelButtonPrefab, listParent);
                button.interactable = unlocked;
                string status = completed ? " ✓" : unlocked ? "" : " (locked)";
                SetButtonText(button, $"  {c + 1}-{l + 1}  {level.title}{status}");

                CampaignLevel captured = level;
                button.onClick.AddListener(() => SelectLevel(captured));
            }
        }
    }

    private void SelectLevel(CampaignLevel level)
    {
        selectedLevel = level;
        SetInfo($"{level.title}\n{level.blurb}\nObjective: {ObjectiveText(level)}  •  Reward: {level.chiReward} chi");
    }

    private static string ObjectiveText(CampaignLevel level)
    {
        switch (level.objective)
        {
            case CampaignObjective.DefeatBoss: return "defeat the boss";
            case CampaignObjective.Survive: return $"survive {Mathf.RoundToInt(level.surviveSeconds)}s";
            default: return "destroy the enemy base";
        }
    }

    private void RefreshLabels()
    {
        if (chiLabel != null) chiLabel.text = $"Chi: {progress.chi}";

        if (redeemedLabel != null)
        {
            List<string> names = new List<string>();
            foreach (int nationInt in progress.redeemedAvatarNations)
            {
                names.Add(NationInfo.DisplayName((Nation)nationInt));
            }
            redeemedLabel.text = names.Count == 0
                ? "Redeemed Avatars: none yet"
                : "Redeemed Avatars: " + string.Join(", ", names);
        }
    }

    private void SetInfo(string message)
    {
        Debug.Log($"[Campaign] {message}");
        if (selectedInfoLabel != null) selectedInfoLabel.text = message;
    }

    private static void SetButtonText(Button button, string text)
    {
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.text = text;
    }
}
