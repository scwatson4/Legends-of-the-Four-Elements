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
    public AudioClip waterbenderAttackClip;
    public AudioClip earthbenderAttackClip;
    public AudioClip spiritAttackClip;
    public AudioClip unitDeathClip;
    public AudioClip structureDestructionClip;

    [Header("Unit Barks (random clip plays on orders - the Army Men feel)")]
    public AudioClip[] selectBarks;      // "Yes, commander?"
    public AudioClip[] moveBarks;        // "On my way!"
    public AudioClip[] attackBarks;      // "For the four nations!"
    [Tooltip("Minimum seconds between barks so spam-clicking doesn't chatter.")]
    public float barkCooldown = 1.5f;

    [Header("Game Stingers")]
    public AudioClip buildingPlacedClip;
    public AudioClip victoryClip;
    public AudioClip defeatClip;

    private AudioSource barkChannel;
    private AudioSource stingerChannel;
    private float lastBarkTime = -10f;

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

        barkChannel = gameObject.AddComponent<AudioSource>();
        barkChannel.volume = 0.3f;
        barkChannel.playOnAwake = false;

        stingerChannel = gameObject.AddComponent<AudioSource>();
        stingerChannel.volume = 0.4f;
        stingerChannel.playOnAwake = false;
    }

    // ------------------------------------------------------------------
    // Barks & stingers
    // ------------------------------------------------------------------

    public void PlaySelectBark() => PlayBark(selectBarks);
    public void PlayMoveBark() => PlayBark(moveBarks);
    public void PlayAttackBark() => PlayBark(attackBarks);

    private void PlayBark(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        if (Time.unscaledTime - lastBarkTime < barkCooldown) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        lastBarkTime = Time.unscaledTime;
        barkChannel.PlayOneShot(clip);
    }

    public void PlayBuildingPlaced()
    {
        if (buildingPlacedClip != null) stingerChannel.PlayOneShot(buildingPlacedClip);
    }

    public void PlayVictorySting()
    {
        if (victoryClip != null) stingerChannel.PlayOneShot(victoryClip);
    }

    public void PlayDefeatSting()
    {
        if (defeatClip != null) stingerChannel.PlayOneShot(defeatClip);
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
            case Unit.UnitType.Waterbender:
                clip = waterbenderAttackClip;
                break;
            case Unit.UnitType.Earthbender:
                clip = earthbenderAttackClip;
                break;
            case Unit.UnitType.Spirit:
                clip = spiritAttackClip;
                break;
            default:
                return; // villagers and unknown types make no attack sound
        }

        if (clip != null)
        {
            unitAttackChannel.clip = clip;
            unitAttackChannel.Play();
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
