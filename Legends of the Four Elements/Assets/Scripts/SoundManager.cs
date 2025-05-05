using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; set; }

    private AudioSource infantryAttackChannel;
    private AudioSource unitDeathChannel;
    private AudioSource structureDestructionChannel;

    public AudioClip infantryAttackClip;
    public AudioClip unitDeathClip;
    public AudioClip structureDestructionClip;

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

        // Initialize AudioSource components
        infantryAttackChannel = gameObject.AddComponent<AudioSource>();
        infantryAttackChannel.volume = 0.1f;
        infantryAttackChannel.playOnAwake = false;

        unitDeathChannel = gameObject.AddComponent<AudioSource>();
        unitDeathChannel.volume = 0.1f;
        unitDeathChannel.playOnAwake = false;

        structureDestructionChannel = gameObject.AddComponent<AudioSource>();
        structureDestructionChannel.volume = 0.2f;
        structureDestructionChannel.playOnAwake = false;
    }

    public void PlayInfantryAttackSound()
    {
        if (infantryAttackChannel.isPlaying == false && infantryAttackClip != null)
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

    public void PlayUnitDeathSound()
    {
        if (unitDeathChannel.isPlaying == false && unitDeathClip != null)
        {
            unitDeathChannel.clip = unitDeathClip;
            unitDeathChannel.Play();
        }
    }

    public void PlayStructureDestructionSound()
    {
        if (structureDestructionChannel.isPlaying == false && structureDestructionClip != null)
        {
            structureDestructionChannel.clip = structureDestructionClip;
            structureDestructionChannel.Play();
        }
    }
}