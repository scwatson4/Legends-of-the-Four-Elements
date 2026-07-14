using UnityEngine;

/// <summary>
/// Lightweight global counters of PLAYER actions, incremented by the gameplay
/// systems themselves (shields, mobility, healing, status effects, upgrades,
/// building placement). The nation-academy tutorials watch these to confirm
/// the player actually performed each technique: a step records the counter
/// when it starts and completes when the counter rises.
/// Only locally-controlled units bump the counters - AI actions don't count.
/// </summary>
public static class TutorialSignals
{
    public static int ShieldsCast;        // ElementalShieldAbility (Q)
    public static int ScooterSprints;     // AirbenderMobility air scooter engaged
    public static int GliderFlights;      // AirbenderMobility staff-glider takeoff
    public static int HealsDone;          // Healer restored an ally's health
    public static int BurnsApplied;       // fire techniques set someone alight
    public static int WaterEffectsApplied;// slows/chills/freezes landed
    public static int RootsApplied;       // Earth Grip held someone fast
    public static int FreezesApplied;     // full Ice Prison (needs real water)
    public static int KnockbacksApplied;  // wind/quake shoves
    public static int LightningThrown;    // a Lightning Jolt left a firebender
    public static int UpgradesPurchased;  // any upgrade bought by the player
    public static int DefensesPlaced;     // towers / walls / gates placed
    public static int BuildingsPlaced;    // anything placed via BuildingPlacer

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        ShieldsCast = 0;
        ScooterSprints = 0;
        GliderFlights = 0;
        HealsDone = 0;
        BurnsApplied = 0;
        WaterEffectsApplied = 0;
        RootsApplied = 0;
        FreezesApplied = 0;
        KnockbacksApplied = 0;
        LightningThrown = 0;
        UpgradesPurchased = 0;
        DefensesPlaced = 0;
        BuildingsPlaced = 0;
    }

    /// <summary>True when the acting object belongs to the local player -
    /// tutorial credit is only given for the player's own actions.</summary>
    public static bool IsPlayerAction(GameObject actor)
    {
        return actor != null && FactionUtility.IsLocallyControlled(actor);
    }
}
