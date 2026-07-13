using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The Avatar never just spawns - they ARRIVE, in the manner of their
/// origin nation:
///   Air   - descends from the sky on a flying bison
///   Fire  - swoops in on a dragon
///   Water - rides in on a rushing wave that dissolves at the shore of battle
///   Earth - erupts from the ground itself amid a tremor
///
/// Runs automatically whenever an AvatarUnit spawns (trained, campaign
/// scratch start, or a redeemed summon). Mounts come from the nation's
/// NationData.avatarMountPrefab (your bison model! a dragon later); with no
/// prefab assigned, a kingdom-tinted greybox mount is built at runtime so
/// the ceremony works from day one. Bosses skip the ceremony - they have
/// their own drama.
/// </summary>
public static class AvatarArrival
{
    private const float FlightSeconds = 3.5f;
    private const float DepartSeconds = 2.5f;
    private const float EruptSeconds = 1.8f;

    public static void Play(AvatarUnit avatarUnit)
    {
        if (avatarUnit == null || !avatarUnit.ceremonialArrival) return;
        if (avatarUnit.GetComponent<CorruptedAvatar>() != null) return; // bosses arrive their own way
        if (GameFeel.Instance == null) return;

        GameFeel.Instance.StartCoroutine(Run(avatarUnit));
    }

    private static IEnumerator Run(AvatarUnit avatarUnit)
    {
        yield return null; // let every Start() (NavMesh snap, registration) finish

        if (avatarUnit == null) yield break;
        GameObject avatar = avatarUnit.gameObject;
        Vector3 destination = avatar.transform.position;
        Nation origin = avatarUnit.currentElement;

        // Suspend the Avatar: present in the world's books, absent from the field.
        SetSuspended(avatar, true);

        switch (origin)
        {
            case Nation.Earth:
                yield return Erupt(avatar, destination);
                break;
            default:
                yield return RideIn(avatar, destination, origin);
                break;
        }

        if (avatar != null)
        {
            SetSuspended(avatar, false);
            NavMeshAgent agent = avatar.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled) agent.Warp(destination);
            GameFeel.Shake(0.25f, 0.5f);
            Debug.Log($"The {NationInfo.DisplayName(origin)} Avatar has arrived!");
        }
    }

    // ------------------------------------------------------------------
    // Earth: the ground delivers its own
    // ------------------------------------------------------------------

    private static IEnumerator Erupt(GameObject avatar, Vector3 destination)
    {
        if (avatar == null) yield break;

        // Rise from beneath the battlefield, renderers on, everything else off.
        foreach (Renderer renderer in avatar.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
        }

        Vector3 buried = destination + Vector3.down * 4f;
        avatar.transform.position = buried;
        GameFeel.Shake(0.45f, EruptSeconds);

        for (float t = 0f; t < EruptSeconds; t += Time.deltaTime)
        {
            if (avatar == null) yield break;
            float eased = Mathf.SmoothStep(0f, 1f, t / EruptSeconds);
            avatar.transform.position = Vector3.Lerp(buried, destination, eased);
            yield return null;
        }
        if (avatar != null) avatar.transform.position = destination;
    }

    // ------------------------------------------------------------------
    // Air / Fire / Water: a mount carries them in
    // ------------------------------------------------------------------

    private static IEnumerator RideIn(GameObject avatar, Vector3 destination, Nation origin)
    {
        bool flying = origin != Nation.Water;
        GameObject mount = CreateMount(origin);

        // Approach from a dramatic angle.
        Vector2 direction = Random.insideUnitCircle.normalized;
        Vector3 offset = new Vector3(direction.x, 0f, direction.y) * 70f;
        Vector3 start = destination + offset + (flying ? Vector3.up * 40f : Vector3.zero);
        Vector3 departEnd = destination - offset + (flying ? Vector3.up * 50f : Vector3.zero);

        mount.transform.position = start;

        for (float t = 0f; t < FlightSeconds; t += Time.deltaTime)
        {
            if (mount == null) break;
            float eased = Mathf.SmoothStep(0f, 1f, t / FlightSeconds);
            Vector3 pos = Vector3.Lerp(start, destination, eased);
            if (flying) pos.y = Mathf.Lerp(start.y, destination.y + 0.5f, eased * eased); // swooping dive
            mount.transform.position = pos;
            mount.transform.rotation = Quaternion.LookRotation((destination - start).normalized);
            yield return null;
        }

        // Touchdown: the Avatar steps off (handled by caller); the mount leaves.
        if (mount != null)
        {
            if (flying)
            {
                GameFeel.Instance.StartCoroutine(FlyAway(mount, destination, departEnd));
            }
            else
            {
                GameFeel.Instance.StartCoroutine(DissolveWave(mount));
            }
        }
    }

    private static IEnumerator FlyAway(GameObject mount, Vector3 from, Vector3 to)
    {
        for (float t = 0f; t < DepartSeconds; t += Time.deltaTime)
        {
            if (mount == null) yield break;
            float eased = t / DepartSeconds;
            mount.transform.position = Vector3.Lerp(from, to, eased * eased); // accelerating climb
            mount.transform.rotation = Quaternion.LookRotation((to - from).normalized);
            yield return null;
        }
        if (mount != null) Object.Destroy(mount);
    }

    private static IEnumerator DissolveWave(GameObject mount)
    {
        Vector3 scale = mount.transform.localScale;
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            if (mount == null) yield break;
            mount.transform.localScale = scale * (1f - t); // the wave sinks back into the earth
            yield return null;
        }
        if (mount != null) Object.Destroy(mount);
    }

    // ------------------------------------------------------------------

    private static GameObject CreateMount(Nation origin)
    {
        // Real mount prefab if the nation has one (your bison model!).
        NationDatabase db = NationDatabase.Load();
        NationData data = db != null ? db.Get(origin) : null;
        if (data != null && data.avatarMountPrefab != null)
        {
            GameObject prefabMount = Object.Instantiate(data.avatarMountPrefab);
            StripGameplay(prefabMount);
            return prefabMount;
        }

        // Greybox mount: readable silhouettes until real models arrive.
        GameObject mount = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        mount.name = origin + "AvatarMount";
        Object.Destroy(mount.GetComponent<Collider>());

        Color color;
        switch (origin)
        {
            case Nation.Air: // sky bison: broad and gentle
                mount.transform.localScale = new Vector3(3f, 1.4f, 4.5f);
                color = new Color(0.95f, 0.92f, 0.8f);
                break;
            case Nation.Fire: // dragon: long and serpentine
                mount.transform.localScale = new Vector3(1.2f, 1.2f, 7f);
                color = new Color(0.85f, 0.2f, 0.1f);
                break;
            default: // water: the crest of a wave
                mount.transform.localScale = new Vector3(6f, 1.2f, 3f);
                color = new Color(0.3f, 0.55f, 1f, 0.7f);
                break;
        }

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        Renderer renderer = mount.GetComponent<Renderer>();
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        renderer.SetPropertyBlock(block);
        return mount;
    }

    /// <summary>A ceremonial mount is scenery: no brains, no collisions.</summary>
    private static void StripGameplay(GameObject mount)
    {
        foreach (Behaviour behaviour in mount.GetComponentsInChildren<Behaviour>(true))
        {
            behaviour.enabled = false;
        }
        foreach (Collider collider in mount.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }
    }

    private static void SetSuspended(GameObject avatar, bool suspended)
    {
        foreach (Renderer renderer in avatar.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = !suspended;
        }
        foreach (Collider collider in avatar.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = !suspended;
        }
        NavMeshAgent agent = avatar.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = !suspended;
        Canvas[] canvases = avatar.GetComponentsInChildren<Canvas>(true);
        foreach (Canvas canvas in canvases) canvas.enabled = !suspended;
    }
}
