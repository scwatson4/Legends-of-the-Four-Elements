using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// Voice command & control via the OpenAI API. Hold V, speak an order,
/// release:
///   "Send in backup to this location"          -> reinforce
///   "Let's build a new sanctuary for bisons"   -> build (fuzzy-matched)
///   "Get me five new earthbenders"             -> train
///
/// Pipeline: Microphone -> WAV -> OpenAI transcription -> a small chat call
/// that maps the sentence onto your actual nation roster (the model is shown
/// your real unit/building names and returns JSON) -> executed through the
/// same Economy/spawner systems the mouse uses.
///
/// "This location" means: the unit you're embodying (VR/hero mode), else
/// wherever your mouse points on the ground.
///
/// Setup: add to any scene object, put your API key in
/// Assets/Resources/openai_key.txt (gitignore it!) or the OPENAI_API_KEY
/// environment variable. Test without a mic via ExecuteText("..."). See
/// docs/VR_AND_VOICE.md.
/// </summary>
public class VoiceCommander : MonoBehaviour
{
    [Header("Input")]
    public KeyCode pushToTalkKey = KeyCode.V;
    public int maxRecordSeconds = 8;
    public int sampleRate = 16000;

    [Header("OpenAI")]
    public string transcriptionModel = "gpt-4o-mini-transcribe";
    public string intentModel = "gpt-4o-mini";

    [Header("Execution")]
    public int defaultReinforcementCount = 5;

    [Header("UI (optional)")]
    public TextMeshProUGUI statusLabel;

    private string apiKey;
    private AudioClip recording;
    private string micDevice;

    private void Start()
    {
        apiKey = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            TextAsset keyFile = Resources.Load<TextAsset>("openai_key");
            if (keyFile != null) apiKey = keyFile.text.Trim();
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            SetStatus("Voice: no API key (Resources/openai_key.txt or OPENAI_API_KEY). Voice disabled.");
            enabled = false;
            return;
        }

        if (Microphone.devices.Length == 0)
        {
            SetStatus("Voice: no microphone found.");
        }
        else
        {
            micDevice = Microphone.devices[0];
            SetStatus($"Voice ready - hold {pushToTalkKey} and speak.");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(pushToTalkKey) && micDevice != null)
        {
            recording = Microphone.Start(micDevice, false, maxRecordSeconds, sampleRate);
            SetStatus("Listening...");
        }

        if (Input.GetKeyUp(pushToTalkKey) && recording != null)
        {
            int position = Microphone.GetPosition(micDevice);
            Microphone.End(micDevice);

            if (position > sampleRate / 4) // ignore sub-quarter-second blips
            {
                byte[] wav = EncodeWav(recording, position);
                StartCoroutine(ProcessSpeech(wav));
            }
            else
            {
                SetStatus("Voice: too short, try again.");
            }
            recording = null;
        }
    }

    // ------------------------------------------------------------------
    // OpenAI pipeline
    // ------------------------------------------------------------------

    private IEnumerator ProcessSpeech(byte[] wavData)
    {
        SetStatus("Transcribing...");

        List<IMultipartFormSection> form = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection("file", wavData, "command.wav", "audio/wav"),
            new MultipartFormDataSection("model", transcriptionModel)
        };

        string transcript = null;
        using (UnityWebRequest request = UnityWebRequest.Post(
                   "https://api.openai.com/v1/audio/transcriptions", form))
        {
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                SetStatus($"Voice: transcription failed ({request.responseCode}).");
                yield break;
            }
            TranscriptionResponse response =
                JsonUtility.FromJson<TranscriptionResponse>(request.downloadHandler.text);
            transcript = response != null ? response.text : null;
        }

        if (string.IsNullOrEmpty(transcript))
        {
            SetStatus("Voice: heard nothing.");
            yield break;
        }
        SetStatus($"\"{transcript}\"");
        yield return InterpretAndExecute(transcript);
    }

    /// <summary>Text entry point - lets you test commands without a mic/VR.</summary>
    public void ExecuteText(string transcript)
    {
        StartCoroutine(InterpretAndExecute(transcript));
    }

    private IEnumerator InterpretAndExecute(string transcript)
    {
        NationData data = GetLocalNationData();
        if (data == null)
        {
            SetStatus("Voice: no NationData for your nation.");
            yield break;
        }

        ChatRequest chat = new ChatRequest
        {
            model = intentModel,
            response_format = new ResponseFormat { type = "json_object" },
            messages = new[]
            {
                new ChatMessage { role = "system", content = BuildSystemPrompt(data) },
                new ChatMessage { role = "user", content = transcript }
            }
        };

        string intentJson = null;
        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(chat));
        using (UnityWebRequest request = new UnityWebRequest(
                   "https://api.openai.com/v1/chat/completions", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                SetStatus($"Voice: intent call failed ({request.responseCode}).");
                yield break;
            }

            ChatResponse response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
            if (response != null && response.choices != null && response.choices.Length > 0)
            {
                intentJson = response.choices[0].message.content;
            }
        }

        if (string.IsNullOrEmpty(intentJson))
        {
            SetStatus("Voice: couldn't understand the order.");
            yield break;
        }

        CommandIntent intent = JsonUtility.FromJson<CommandIntent>(intentJson);
        Execute(intent, data);
    }

    private string BuildSystemPrompt(NationData data)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("You convert a battlefield voice order into JSON for an RTS game. " +
                      "Reply with ONLY a JSON object: " +
                      "{\"action\":\"reinforce|train|build|unknown\",\"unit_index\":-1,\"building_index\":-1,\"count\":0}. " +
                      "For 'train', set unit_index to the roster unit meant and count to how many (default 1). " +
                      "For 'build', set building_index to the closest matching building (-2 means command center). " +
                      "For 'reinforce'/'send backup', set count to how many units (default 0 = commander's choice).");

        sb.AppendLine("Units (index: name):");
        if (data.units != null)
        {
            for (int i = 0; i < data.units.Length; i++)
            {
                if (data.units[i] != null) sb.AppendLine($"{i}: {data.units[i].unitName}");
            }
        }
        sb.AppendLine("Buildings (index: name):");
        if (data.buildings != null)
        {
            for (int i = 0; i < data.buildings.Length; i++)
            {
                if (data.buildings[i] != null) sb.AppendLine($"{i}: {data.buildings[i].buildingName}");
            }
        }
        sb.AppendLine("-2: Command Center");
        return sb.ToString();
    }

    // ------------------------------------------------------------------
    // Execution
    // ------------------------------------------------------------------

    private void Execute(CommandIntent intent, NationData data)
    {
        if (intent == null || intent.action == "unknown")
        {
            SetStatus("Voice: order unclear - try again.");
            return;
        }

        switch (intent.action)
        {
            case "reinforce":
                Reinforce(intent.count > 0 ? intent.count : defaultReinforcementCount);
                break;
            case "train":
                Train(Mathf.Max(0, intent.unit_index), Mathf.Max(1, intent.count));
                break;
            case "build":
                Build(intent.building_index, data);
                break;
            default:
                SetStatus($"Voice: unsupported action '{intent.action}'.");
                break;
        }
    }

    private Vector3 GetTargetLocation()
    {
        // Embodied (VR/hero mode): backup comes to YOU.
        if (EmbodimentController.IsActive && EmbodimentController.Instance.PossessedUnit != null)
        {
            return EmbodimentController.Instance.PossessedUnit.transform.position;
        }

        // Commander mode: wherever the mouse points.
        if (Camera.main != null)
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition),
                    out hit, Mathf.Infinity))
            {
                return hit.point;
            }
        }

        // Fallback: home base.
        foreach (CommandCenter cc in FindObjectsByType<CommandCenter>(FindObjectsSortMode.None))
        {
            if (FactionUtility.IsLocallyControlled(cc.gameObject)) return cc.transform.position;
        }
        return Vector3.zero;
    }

    private void Reinforce(int count)
    {
        Vector3 target = GetTargetLocation();
        int sent = 0;

        // Grab your combat units farthest from the fight first (they're the idle ones).
        List<GameObject> candidates = new List<GameObject>();
        if (UnitSelectionManager.Instance != null)
        {
            foreach (GameObject go in UnitSelectionManager.Instance.allUnitsList)
            {
                if (go == null || !FactionUtility.IsLocallyControlled(go)) continue;
                if (EmbodimentController.IsActive &&
                    go == EmbodimentController.Instance.PossessedUnit) continue;

                Unit unit = go.GetComponent<Unit>();
                if (unit == null || unit.category == UnitCategory.Worker) continue;
                if (go.GetComponent<AttackController>() == null) continue;
                candidates.Add(go);
            }
        }
        candidates.Sort((a, b) =>
            Vector3.Distance(b.transform.position, target)
                .CompareTo(Vector3.Distance(a.transform.position, target)));

        foreach (GameObject go in candidates)
        {
            if (sent >= count) break;
            NavMeshAgent agent = go.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) continue;

            AttackController attack = go.GetComponent<AttackController>();
            if (attack != null) attack.targetToAttack = null;
            agent.SetDestination(FormationUtility.GetDestination(target, sent, count));
            sent++;
        }

        SetStatus(sent > 0 ? $"Backup inbound: {sent} unit(s)!" : "No units available for backup.");
        if (sent > 0 && SoundManager.Instance != null) SoundManager.Instance.PlayMoveBark();
    }

    private void Train(int unitIndex, int count)
    {
        UnitSpawner spawner = null;
        foreach (UnitSpawner candidate in FindObjectsByType<UnitSpawner>(FindObjectsSortMode.None))
        {
            if (!FactionUtility.IsLocallyControlled(candidate.gameObject)) continue;
            spawner = candidate;
            if (candidate.GetComponentInParent<CommandCenter>() != null) break; // prefer the CC
        }

        if (spawner == null)
        {
            SetStatus("No production building available to train units.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            spawner.QueueRosterUnit(unitIndex);
        }
        SetStatus($"Training {count} unit(s).");
    }

    private void Build(int buildingIndex, NationData data)
    {
        NationData.BuildingEntry entry = buildingIndex == -2
            ? BuildingPlacer.MakeCommandCenterEntry(data)
            : data.GetBuilding(buildingIndex);
        if (entry == null || entry.prefab == null)
        {
            SetStatus("Voice: no matching building in your nation's roster.");
            return;
        }

        // Earthbent structures need earthbenders alive in the army.
        if (!RequiresBenderPresence.SatisfiedFor(entry.prefab, FactionManager.LocalPlayerFactionId))
        {
            SetStatus($"The {entry.buildingName} needs benders in your army to raise it.");
            return;
        }

        // Find a clear, walkable spot near the target location.
        Vector3 center = GetTargetLocation();
        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector2 circle = Random.insideUnitCircle * (6f + attempt * 2f);
            Vector3 candidate = center + new Vector3(circle.x, 0f, circle.y);

            NavMeshHit navHit;
            if (!NavMesh.SamplePosition(candidate, out navHit, 4f, NavMesh.AllAreas)) continue;
            candidate = navHit.position;

            bool blocked = false;
            foreach (Collider hit in Physics.OverlapSphere(candidate + Vector3.up, 4f))
            {
                if (hit.GetComponentInParent<Unit>() != null ||
                    hit.GetComponentInParent<Structure>() != null ||
                    hit.GetComponentInParent<CommandCenter>() != null)
                {
                    blocked = true;
                    break;
                }
            }
            if (blocked) continue;

            if (!Economy.TrySpend(FactionManager.LocalPlayerFactionId, entry.cost))
            {
                SetStatus($"Not enough silver for the {entry.buildingName}.");
                return;
            }

            GameObject building = Instantiate(entry.prefab, candidate, Quaternion.identity);
            FactionUtility.SetFaction(building, FactionManager.LocalPlayerFactionId);
            if (SoundManager.Instance != null) SoundManager.Instance.PlayBuildingPlaced();
            SetStatus($"Constructing {entry.buildingName}!");
            return;
        }

        SetStatus("Voice: no clear ground for that building here.");
    }

    private NationData GetLocalNationData()
    {
        NationDatabase db = NationDatabase.Load();
        if (db == null) return null;
        Faction local = FactionManager.Get(FactionManager.LocalPlayerFactionId);
        return db.Get(local != null ? local.nation : GameSetup.PlayerNation);
    }

    private void SetStatus(string message)
    {
        Debug.Log($"[Voice] {message}");
        if (statusLabel != null) statusLabel.text = message;
    }

    // ------------------------------------------------------------------
    // WAV encoding (16-bit PCM)
    // ------------------------------------------------------------------

    private static byte[] EncodeWav(AudioClip clip, int sampleCount)
    {
        float[] samples = new float[sampleCount * clip.channels];
        clip.GetData(samples, 0);

        int byteCount = samples.Length * 2;
        byte[] wav = new byte[44 + byteCount];

        WriteAscii(wav, 0, "RIFF");
        WriteInt(wav, 4, 36 + byteCount);
        WriteAscii(wav, 8, "WAVEfmt ");
        WriteInt(wav, 16, 16);
        WriteShort(wav, 20, 1); // PCM
        WriteShort(wav, 22, (short)clip.channels);
        WriteInt(wav, 24, clip.frequency);
        WriteInt(wav, 28, clip.frequency * clip.channels * 2);
        WriteShort(wav, 32, (short)(clip.channels * 2));
        WriteShort(wav, 34, 16);
        WriteAscii(wav, 36, "data");
        WriteInt(wav, 40, byteCount);

        int offset = 44;
        foreach (float sample in samples)
        {
            short value = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
            wav[offset++] = (byte)(value & 0xff);
            wav[offset++] = (byte)((value >> 8) & 0xff);
        }
        return wav;
    }

    private static void WriteAscii(byte[] buffer, int offset, string text)
    {
        for (int i = 0; i < text.Length; i++) buffer[offset + i] = (byte)text[i];
    }

    private static void WriteInt(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value & 0xff);
        buffer[offset + 1] = (byte)((value >> 8) & 0xff);
        buffer[offset + 2] = (byte)((value >> 16) & 0xff);
        buffer[offset + 3] = (byte)((value >> 24) & 0xff);
    }

    private static void WriteShort(byte[] buffer, int offset, short value)
    {
        buffer[offset] = (byte)(value & 0xff);
        buffer[offset + 1] = (byte)((value >> 8) & 0xff);
    }

    // ------------------------------------------------------------------
    // API payload shapes
    // ------------------------------------------------------------------

    [System.Serializable] private class TranscriptionResponse { public string text; }

    [System.Serializable]
    private class ChatRequest
    {
        public string model;
        public ChatMessage[] messages;
        public ResponseFormat response_format;
    }

    [System.Serializable] private class ChatMessage { public string role; public string content; }
    [System.Serializable] private class ResponseFormat { public string type; }
    [System.Serializable] private class ChatResponse { public Choice[] choices; }
    [System.Serializable] private class Choice { public ChatMessage message; }

    [System.Serializable]
    private class CommandIntent
    {
        public string action = "unknown";
        public int unit_index = -1;
        public int building_index = -1;
        public int count = 0;
    }
}
