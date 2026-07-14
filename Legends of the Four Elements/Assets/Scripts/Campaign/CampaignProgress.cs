using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent campaign save (PlayerPrefs + JSON): completed levels, chi
/// (the campaign's soul-energy currency), permanent upgrade levels bought
/// between missions, and which corrupted Avatars have been redeemed.
/// </summary>
[System.Serializable]
public class CampaignProgress
{
    public List<string> completedLevelIds = new List<string>();
    public int chi;

    // Parallel lists (JsonUtility can't serialize dictionaries).
    public List<string> permanentUpgradeIds = new List<string>();
    public List<int> permanentUpgradeLevels = new List<int>();

    public List<int> redeemedAvatarNations = new List<int>(); // (int)Nation

    private const string SaveKey = "LegendsCampaignProgress";

    private static CampaignProgress cached;

    public static CampaignProgress Load()
    {
        if (cached != null) return cached;

        string json = PlayerPrefs.GetString(SaveKey, "");
        cached = string.IsNullOrEmpty(json)
            ? new CampaignProgress()
            : JsonUtility.FromJson<CampaignProgress>(json) ?? new CampaignProgress();
        return cached;
    }

    public void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(this));
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        cached = new CampaignProgress();
        cached.Save();
    }

    // ------------------------------------------------------------------

    public bool IsLevelCompleted(string levelId) => completedLevelIds.Contains(levelId);

    public void CompleteLevel(string levelId, int chiReward)
    {
        if (!completedLevelIds.Contains(levelId))
        {
            completedLevelIds.Add(levelId);
            chi += chiReward;
        }
        Save();
    }

    /// <summary>A level unlocks when the previous REQUIRED level is done.
    /// Optional levels (the nation academies) are skipped by the chain, so
    /// they never gate the story - and they unlock alongside whatever
    /// required level precedes them.</summary>
    public bool IsLevelUnlocked(List<CampaignChapter> chapters, int chapterIndex, int levelIndex)
    {
        // Walk backwards through the flattened campaign to the nearest
        // non-optional predecessor; that's the gate.
        int c = chapterIndex, l = levelIndex;
        while (true)
        {
            l--;
            if (l < 0)
            {
                c--;
                if (c < 0) return true; // nothing required before this level
                l = chapters[c].levels.Count - 1;
                if (l < 0) continue;    // empty chapter
            }

            CampaignLevel previous = chapters[c].levels[l];
            if (previous.isOptional) continue;
            return IsLevelCompleted(previous.id);
        }
    }

    // ------------------------------------------------------------------

    public bool IsAvatarRedeemed(Nation nation) => redeemedAvatarNations.Contains((int)nation);

    public void RedeemAvatar(Nation nation)
    {
        if (!redeemedAvatarNations.Contains((int)nation))
        {
            redeemedAvatarNations.Add((int)nation);
        }
        Save();
    }

    public int RedeemedAvatarCount => redeemedAvatarNations.Count;

    // ------------------------------------------------------------------

    public int GetPermanentUpgradeLevel(string upgradeId)
    {
        int index = permanentUpgradeIds.IndexOf(upgradeId);
        return index >= 0 ? permanentUpgradeLevels[index] : 0;
    }

    public void SetPermanentUpgradeLevel(string upgradeId, int level)
    {
        int index = permanentUpgradeIds.IndexOf(upgradeId);
        if (index >= 0) permanentUpgradeLevels[index] = level;
        else
        {
            permanentUpgradeIds.Add(upgradeId);
            permanentUpgradeLevels.Add(level);
        }
        Save();
    }
}
