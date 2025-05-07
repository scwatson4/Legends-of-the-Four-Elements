using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MainMenuMusic : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        audioSource.loop = true;
        audioSource.volume = 0.5f;
        audioSource.playOnAwake = true;

        if (audioSource.clip == null)
        {
            Debug.LogError("No audio clip assigned to MainMenuMusic AudioSource");
        }
    }

    private void Start()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
            Debug.Log("MainMenu music started.");
        }
    }
}