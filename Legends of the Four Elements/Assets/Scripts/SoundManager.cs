using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; set; }

    private AudioSource infantryAttackChannel;
    public AudioClip infantryAttackClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        infantryAttackChannel = gameObject.AddComponent<AudioSource>();
        infantryAttackChannel.volume = 0.5f;
        infantryAttackChannel.playOnAwake = false;
    }

    public void PlayInfantryAttackSound()
    {
        if(infantryAttackChannel.isPlaying == false)
        {
            infantryAttackChannel.clip = infantryAttackClip;
            infantryAttackChannel.Play();
        }
    }
    public void StopInfantryAttackSound()
    {
        if (infantryAttackChannel.isPlaying)
        {
            infantryAttackChannel.Stop();
        }
    }
}
