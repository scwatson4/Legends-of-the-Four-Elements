using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Level1Music : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        audioSource.loop = true;
        audioSource.volume = 0.01f;
        audioSource.playOnAwake = true;

        if (audioSource.clip == null)
        {
            Debug.LogError("No audio clip assigned to Level1Music AudioSource");
        }
    }

    private void Start()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
            Debug.Log("Level1Music music started.");
        }
    }
}