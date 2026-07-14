using System.Collections.Generic;
using UnityEngine;

public enum GameMode
{
    /// <summary>The original Level1 experience: defend against Fire Nation waves.</summary>
    Survival = 0,

    /// <summary>Free-for-all / teams vs AI commanders, Halo Wars style.</summary>
    Skirmish = 1,

    /// <summary>Story campaign level (CampaignManager drives objectives/bosses).</summary>
    Campaign = 2
}

public enum Difficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2
}

/// <summary>
/// Match configuration chosen in the main menu, carried across scene loads.
/// Defaults reproduce the original game (Air player vs Fire waves) so the
/// level still works when launched directly from the editor.
/// </summary>
public static class GameSetup
{
    [System.Serializable]
    public class AIOpponent
    {
        public Nation nation = Nation.Fire;
        public int teamGroup = 1;

        public AIOpponent() { }

        public AIOpponent(Nation nation, int teamGroup)
        {
            this.nation = nation;
            this.teamGroup = teamGroup;
        }
    }

    public static Nation PlayerNation = Nation.Air;
    public static GameMode Mode = GameMode.Survival;
    public static readonly List<AIOpponent> AIOpponents = new List<AIOpponent>();

    /// <summary>Set by the multiplayer lobby; co-op puts all humans on team 0.</summary>
    public static bool MultiplayerCoop = false;

    /// <summary>Scene chosen in the map selector. Empty = the controller's default.</summary>
    public static string MapSceneName = "";

    /// <summary>Seed used by MapGenerator scenes. 0 = generator default.</summary>
    public static int MapSeed = 0;

    public static Difficulty Difficulty = Difficulty.Normal;

    /// <summary>Silly mode: everyone renders as shiny single-color plastic
    /// toys (Army Men RTS homage). Toggle in the menu or with F9 in-game.</summary>
    public static bool PlasticSoldiersMode = false;

    /// <summary>Scales AI income and enemy wave sizes.</summary>
    public static float EnemyStrengthMultiplier =>
        Difficulty == Difficulty.Easy ? 0.7f :
        Difficulty == Difficulty.Hard ? 1.4f : 1f;

    /// <summary>True once the menu (or a scene autoconfig) has set up a match.
    /// A generated scene played directly can use this to apply its own
    /// defaults only when nothing else already has.</summary>
    public static bool Configured { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        PlayerNation = Nation.Air;
        Mode = GameMode.Survival;
        AIOpponents.Clear();
        MultiplayerCoop = false;
        MapSceneName = "";
        MapSeed = 0;
        Difficulty = Difficulty.Normal;
        PlasticSoldiersMode = false;
        Configured = false;
    }

    public static void ConfigureSurvival(Nation playerNation)
    {
        PlayerNation = playerNation;
        Mode = GameMode.Survival;
        AIOpponents.Clear();
        AIOpponents.Add(new AIOpponent(Nation.Fire, 1));
        Configured = true;
    }

    public static void ConfigureSkirmish(Nation playerNation, int aiOpponentCount)
    {
        PlayerNation = playerNation;
        Mode = GameMode.Skirmish;
        AIOpponents.Clear();
        Configured = true;

        // Fill AI seats with the nations the player did not pick, each on
        // its own team (free-for-all).
        int teamGroup = 1;
        foreach (Nation nation in new[] { Nation.Fire, Nation.Earth, Nation.Water, Nation.Air })
        {
            if (AIOpponents.Count >= aiOpponentCount) break;
            if (nation == playerNation) continue;
            AIOpponents.Add(new AIOpponent(nation, teamGroup++));
        }
    }
}
