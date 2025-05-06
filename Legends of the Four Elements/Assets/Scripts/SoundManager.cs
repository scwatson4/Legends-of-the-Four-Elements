using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; set; }

    private AudioSource unitAttackChannel;
    private AudioSource unitDeathChannel;
    private AudioSource structureDestructionChannel;

    public AudioClip firebenderAttackClip;
    public AudioClip airbenderAttackClip;
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
        unitAttackChannel = gameObject.AddComponent<AudioSource>();
        unitAttackChannel.volume = 0.15f;
        unitAttackChannel.playOnAwake = false;

        unitDeathChannel = gameObject.AddComponent<AudioSource>();
        unitDeathChannel.volume = 0.1f;
        unitDeathChannel.playOnAwake = false;

        structureDestructionChannel = gameObject.AddComponent<AudioSource>();
        structureDestructionChannel.volume = 0.2f;
        structureDestructionChannel.playOnAwake = false;
    }

    public void PlayAttackSound(Unit.UnitType unitType)
    {
        if (unitAttackChannel.isPlaying) return; // To avoid overlapping sounds

        AudioClip clip = null;
        switch (unitType)
        {
            case Unit.UnitType.Firebender:
                clip = firebenderAttackClip;
                break;
            case Unit.UnitType.Airbender:
                clip = airbenderAttackClip;
                break;
            default:
                Debug.LogWarning($"No attack sound defined for unit type: {unitType}");
                return;
        }

        if (clip != null)
        {
            unitAttackChannel.clip = clip;
            unitAttackChannel.Play();
            Debug.Log($"Playing attack sound for {unitType}");
        }
        else
        {
            Debug.LogWarning($"Attack sound clip missing for unit type: {unitType}");
        }
    }

    public void StopAttackSound()
    {
        if (unitAttackChannel.isPlaying)
        {
            unitAttackChannel.Stop();
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