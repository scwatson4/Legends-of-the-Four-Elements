# Multiplayer Setup — Legends of the Four Elements

The code for online co-op/versus is on this branch (Netcode for GameObjects,
server-authoritative). This document is the Editor wiring it needs. Do the
single-player parts of `FINISHING_GUIDE.md` first — multiplayer builds on the
faction system, NationDatabase and StartLocations.

**Model:** the host is the server and simulates everything (unit movement,
combat, AI). Clients render the server's world and send orders
(move / attack / build) as RPCs. This is the standard approach for a
small-scale RTS and avoids desync entirely.

---

## 1. NetworkManager (MainMenuScene)

1. Create an empty GameObject `NetworkManager` in `MainMenuScene`.
2. Add **NetworkManager** (from the Netcode package). Unity will prompt for a
   transport — pick **UnityTransport**.
3. Add the `NetworkBootstrap` component (ours) to the same object and set
   **Game Scene Name** to your gameplay scene's exact name.
4. In the NetworkManager inspector:
   - **Enable Scene Management**: ON (the host loads the level for everyone).
   - **Player Prefab**: see step 2 below.

## 2. Player prefab

1. Create an empty prefab `NetworkPlayer` (no visuals needed).
2. Add **NetworkObject** + our `RTSNetworkPlayer` component.
3. Assign it as the **Player Prefab** on the NetworkManager.

This object represents a connected human: their nation pick, faction id,
team, and credits. One spawns automatically per connection.

## 3. Make unit & building prefabs network-ready

For **every prefab that can exist in a multiplayer match** (all four nations'
bender units, spirits if you want them in MP, and all command centers):

- Units: add **NetworkObject**, **NetworkTransform**, **NetworkAnimator**
  (assign the unit's Animator), and our **`NetworkUnit`**.
- Command centers: add **NetworkObject** and our **`NetworkCommandCenter`**.
- Register all of these prefabs in the NetworkManager's **Network Prefabs
  List** (NetworkManager inspector > Prefab Lists, or a
  `NetworkPrefabsList` asset).

Notes:
- NetworkTransform defaults to server-authoritative — correct here.
- These components are harmless in single-player (they do nothing until a
  network session starts), so it's fine to put them on the shared prefabs.

## 4. Multiplayer menu panel

Add a `MultiplayerPanel` to the main menu Canvas with:

| UI element | Wire to (on the NetworkManager's `NetworkBootstrap`) |
|---|---|
| Host button | `HostGame()` |
| IP input field (TMP) | assign to **Ip Input Field** |
| Join button | `JoinGame()` |
| 4 nation buttons | `SelectNation(0..3)` |
| Co-op toggle | `SetCoop(bool)` — ON = humans allied vs AI, OFF = versus |
| Start Match button (host) | `StartMatch()` |
| Status label (TMP) | assign to **Status Label** |
| Leave button | `Disconnect()` |

Flow: host clicks **Host**, friends enter the host's IP and click **Join**,
everyone picks a nation, host clicks **Start Match** → the level loads for
all players simultaneously.

## 5. Level scene additions

In your gameplay scene (the skirmish scene from the finishing guide):

1. Ensure it has 2–4 `StartLocation` markers (one per possible player).
2. Add an empty GameObject `NetworkMatchController` with **NetworkObject** +
   our **`NetworkMatchManager`** (assign the NationDatabase).

When the scene loads in a network session, the server spawns each player's
command center and starting squad at their StartLocation. Build buttons
(`UnitSpawner.QueueRosterUnit`) automatically route through the server in
multiplayer — server checks that player's credits and spawns the unit at
their base.

## 6. Testing without two computers

The `com.unity.multiplayer.playmode` package is already in the manifest:
**Window > Multiplayer Play Mode** → enable a Virtual Player → enter Play
Mode; one instance hosts, the virtual player joins `127.0.0.1`. (Alternative:
make a standalone build and run it next to the Editor.)

## 7. Playing over the internet

Direct IP works on LAN out of the box. Over the internet the host must
port-forward UDP 7777 — fine for testing, annoying for friends. The clean
upgrade is **Unity Relay + Lobby** (free tier is generous):

1. Link the project to Unity Gaming Services (Project Settings > Services).
2. Install `com.unity.services.relay` (+ optionally `com.unity.services.lobby`).
3. In `NetworkBootstrap`, replace `SetConnectionData` with the Relay
   allocation flow (`RelayService.Instance.CreateAllocationAsync` on host →
   join code → `SetRelayServerData` on both sides) and show the 6-letter join
   code in the lobby UI instead of an IP field.

The rest of the game code doesn't change — Relay only swaps how the transport
connects.

## Known limits of this first multiplayer pass (by design, keep scope sane)

- Survival waves and AI commanders are not spawned in multiplayer matches yet
  (players fight each other; co-op vs AI needs `NetworkMatchManager` to also
  spawn an AI base — the single-player `MatchManager.SpawnBaseFor` shows
  exactly how, and its units would need `NetworkObject.Spawn()` like
  `NetworkMatchManager.SpawnBaseFor` does).
- Taming spirits is single-player only for now (gate it or replicate it via
  a ServerRpc later).
- No reconnect/late-join handling: players must be in the lobby before the
  host starts.
