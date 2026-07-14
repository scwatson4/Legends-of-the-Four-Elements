using System.Collections.Generic;
using UnityEngine;

/// <summary>What the player must do to win a campaign level.</summary>
public enum CampaignObjective
{
    DestroyEnemyBase = 0,  // classic: raze every rival command center
    DefeatBoss = 1,        // boss arena: kill the boss (no enemy base)
    Survive = 2,           // hold out for surviveSeconds against the assault
    Escort = 3             // deliver the caravan alive to the golden beacon
}

/// <summary>Which boss, if any, appears in a level.</summary>
public enum CampaignBoss
{
    None = 0,
    CorruptedAvatarAir = 1,
    CorruptedAvatarWater = 2,
    CorruptedAvatarEarth = 3,
    CorruptedAvatarFire = 4,
    DarkSpirit = 5
}

[System.Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea] public string text;

    public DialogueLine() { }

    public DialogueLine(string speaker, string text)
    {
        this.speaker = speaker;
        this.text = text;
    }
}

/// <summary>
/// One campaign level. The built-in campaign (DefaultCampaign.cs) creates
/// these in code - complete with story dialogue - so the whole campaign runs
/// without authoring assets. Field meanings:
///  - sceneName: empty = the campaign menu's default level scene
///  - mapSeed: fed to MapGenerator scenes so every level's terrain differs
///  - enemyNations: AI opponents (empty for pure boss arenas)
/// </summary>
[System.Serializable]
public class CampaignLevel
{
    public string id;             // e.g. "c2l4" - stable, used by the save file
    public string title;
    [TextArea] public string blurb;

    public string sceneName = "";
    public int mapSeed = 1;

    public CampaignObjective objective = CampaignObjective.DestroyEnemyBase;
    public CampaignBoss boss = CampaignBoss.None;
    public float surviveSeconds = 300f;
    public List<Nation> enemyNations = new List<Nation>();

    [Tooltip("Boot Camp: attaches the interactive TutorialManager to this level.")]
    public bool isTutorial = false;

    [Header("Chapter Interlude (auto-filled on each chapter's first level)")]
    public string interludeTitle = "";
    [TextArea] public string interludeText = "";

    public int chiReward = 100;

    [Header("Scratch Start (Avatar + builder, build your own base)")]
    [Tooltip("Starting silver: enough to place the command center and get going.")]
    public int startingSilver = 600;

    [Tooltip("How much of your base stands when the mission opens: " +
             "0 = nothing (low silver too - befriend a local village for their alliance gift!), " +
             "0.5 = command center, 0.75 = CC + housing + tower + extra worker, " +
             "1 = all that plus a production building.")]
    [Range(0f, 1f)] public float startingBaseLevel = 0.5f;

    [Header("Enemy Waves")]
    public float firstWaveDelay = 90f;
    public float waveInterval = 70f;
    public int waveBaseSize = 3;
    [Tooltip("Extra units added to each successive wave.")]
    public int waveGrowth = 1;

    public List<DialogueLine> dialogue = new List<DialogueLine>();
}

[System.Serializable]
public class CampaignChapter
{
    public string title;
    [TextArea] public string description;
    public List<CampaignLevel> levels = new List<CampaignLevel>();
}
