using Unity.Netcode;

/// <summary>
/// Safe checks for "are we in a multiplayer session?" that single-player code
/// can call without caring whether networking is running. In single-player
/// everything simulates locally; in multiplayer only the server simulates.
/// </summary>
public static class NetworkGuard
{
    public static bool IsNetworked =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    public static bool IsServer =>
        !IsNetworked || NetworkManager.Singleton.IsServer;

    /// <summary>True on multiplayer clients, where damage/spawning must not
    /// run locally (the server's results replicate down instead).</summary>
    public static bool BlockLocalSimulation =>
        IsNetworked && !NetworkManager.Singleton.IsServer;
}
