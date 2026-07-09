using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;

/// <summary>
/// Main-menu multiplayer panel logic: host a match, join by IP, pick your
/// nation, choose co-op vs versus, and (host only) launch the level for
/// everyone. Wire the UI per docs/MULTIPLAYER_SETUP.md.
/// </summary>
public class NetworkBootstrap : MonoBehaviour
{
    [Header("Match")]
    public string gameSceneName = "Level1";
    public ushort port = 7777;

    [Header("UI (optional)")]
    public TMP_InputField ipInputField;
    public TextMeshProUGUI statusLabel;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    // ---- UI hooks -----------------------------------------------------

    public void HostGame()
    {
        UnityTransport transport = GetTransport();
        if (transport == null) return;

        transport.SetConnectionData("0.0.0.0", port);
        NetworkManager.Singleton.StartHost();
        SetStatus($"Hosting on port {port}. Waiting for players...");
    }

    public void JoinGame()
    {
        string ip = ipInputField != null ? ipInputField.text.Trim() : "127.0.0.1";
        if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";

        UnityTransport transport = GetTransport();
        if (transport == null) return;

        transport.SetConnectionData(ip, port);
        NetworkManager.Singleton.StartClient();
        SetStatus($"Connecting to {ip}:{port}...");
    }

    /// <summary>Wire nation buttons in the multiplayer panel: 0=Air..3=Fire.</summary>
    public void SelectNation(int nationIndex)
    {
        Nation nation = (Nation)Mathf.Clamp(nationIndex, 0, 3);
        GameSetup.PlayerNation = nation;

        if (RTSNetworkPlayer.Local != null)
        {
            RTSNetworkPlayer.Local.SetNation(nation);
        }
        SetStatus($"Playing as {NationInfo.DisplayName(nation)}");
    }

    /// <summary>Wire a co-op toggle. On = all humans allied vs AI; off = versus.</summary>
    public void SetCoop(bool coop)
    {
        GameSetup.MultiplayerCoop = coop;
    }

    /// <summary>Host only: loads the level for every connected player.</summary>
    public void StartMatch()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost)
        {
            SetStatus("Only the host can start the match.");
            return;
        }

        // Lock in team assignments now that everyone has picked.
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            RTSNetworkPlayer player = client.PlayerObject != null
                ? client.PlayerObject.GetComponent<RTSNetworkPlayer>()
                : null;
            if (player != null)
            {
                player.AssignTeam(GameSetup.MultiplayerCoop ? 0 : (int)client.ClientId);
            }
        }

        GameSetup.Mode = GameMode.Skirmish;
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName,
            UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void Disconnect()
    {
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
        SetStatus("Disconnected.");
    }

    // ---- internals ----------------------------------------------------

    private UnityTransport GetTransport()
    {
        if (NetworkManager.Singleton == null)
        {
            SetStatus("No NetworkManager in the scene. See docs/MULTIPLAYER_SETUP.md.");
            return null;
        }
        return NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
    }

    private void OnClientConnected(ulong clientId)
    {
        SetStatus($"Player {clientId} connected. " +
                  $"{NetworkManager.Singleton.ConnectedClientsList.Count} player(s) in lobby.");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        SetStatus($"Player {clientId} disconnected.");
    }

    private void SetStatus(string message)
    {
        Debug.Log($"[Multiplayer] {message}");
        if (statusLabel != null) statusLabel.text = message;
    }
}
