using UnityEngine;
using TMPro;

/// <summary>
/// UI hook for buying upgrades. Wire side-panel upgrade buttons to
/// Purchase(index) - the index into the local player's nation upgrade list
/// (NationData.upgrades), so the same buttons work for every nation.
/// </summary>
public class UpgradePurchaser : MonoBehaviour
{
    [Tooltip("Optional label refreshed with the last purchase result.")]
    public TextMeshProUGUI feedbackLabel;

    public void Purchase(int upgradeIndex)
    {
        // Multiplayer client: server validates and pays.
        if (RTSNetworkPlayer.TryRelayPurchaseUpgrade(upgradeIndex)) return;

        UpgradeData upgrade = GetLocalUpgrade(upgradeIndex);
        if (upgrade == null)
        {
            Debug.LogWarning($"UpgradePurchaser: no upgrade at index {upgradeIndex}. Fill in NationData.upgrades.");
            return;
        }

        int factionId = FactionManager.LocalPlayerFactionId;
        int level = UpgradeManager.GetLevel(factionId, upgrade);

        if (UpgradeManager.TryPurchase(factionId, upgrade))
        {
            SetFeedback($"{upgrade.displayName} → level {level + 1}");
        }
        else if (level >= upgrade.maxLevel)
        {
            SetFeedback($"{upgrade.displayName} is already mastered.");
        }
        else
        {
            SetFeedback($"Not enough silver for {upgrade.displayName}.");
        }
    }

    public static UpgradeData GetLocalUpgrade(int upgradeIndex)
    {
        NationDatabase db = NationDatabase.Load();
        if (db == null) return null;

        Faction local = FactionManager.Get(FactionManager.LocalPlayerFactionId);
        NationData data = db.Get(local != null ? local.nation : GameSetup.PlayerNation);
        if (data == null || data.upgrades == null) return null;
        if (upgradeIndex < 0 || upgradeIndex >= data.upgrades.Length) return null;
        return data.upgrades[upgradeIndex];
    }

    private void SetFeedback(string message)
    {
        Debug.Log($"[Upgrades] {message}");
        if (feedbackLabel != null) feedbackLabel.text = message;
    }
}
