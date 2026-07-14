using UnityEngine;

/// <summary>
/// Lets a generated skirmish scene be PLAYED DIRECTLY (Play button) without
/// going through the main menu. If nothing has configured a match yet
/// (GameSetup.Configured is false), it sets up a quick free-for-all so the
/// scene is instantly playable. When you DO launch from the menu, the menu
/// configures GameSetup first and this does nothing.
///
/// Runs before MatchManager (which reads GameSetup in Awake) via a very low
/// execution order.
/// </summary>
[DefaultExecutionOrder(-10000)]
public class SkirmishAutoConfig : MonoBehaviour
{
    public Nation playerNation = Nation.Air;
    [Range(1, 3)] public int aiOpponents = 1;

    private void Awake()
    {
        if (GameSetup.Configured) return; // the menu already set things up
        GameSetup.ConfigureSkirmish(playerNation, aiOpponents);
        Debug.Log($"[SkirmishAutoConfig] Direct play: {playerNation} vs {aiOpponents} AI.");
    }
}
