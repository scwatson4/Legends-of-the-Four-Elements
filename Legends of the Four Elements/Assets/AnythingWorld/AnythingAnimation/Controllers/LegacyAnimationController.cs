using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Animation
{
    using Animation = UnityEngine.Animation;

    /// <summary>
    /// Provides base functionality for controlling legacy animations, including crossfading,
    /// playing, and stopping animations.
    /// </summary>
    public class LegacyAnimationController : MonoBehaviour
    {
        public float crossfadeTime = 0.01f;
        
        [HideInInspector] public Animation animationPlayer;
        [HideInInspector] public List<string> loadedAnimationNames = new List<string>();
        [HideInInspector] public List<float> loadedAnimationDurations = new List<float>();
        public Dictionary<string, float> animationNamesToDurations = new Dictionary<string, float>();

        // Initializes the animation controller by mapping animation names to their durations and
        // fetching the Animation component.
        private void Awake()
        {
            animationNamesToDurations.Clear();
            for (int i = 0; i < loadedAnimationNames.Count; i++)
            {
                animationNamesToDurations.Add(loadedAnimationNames[i], loadedAnimationDurations[i]);
            }
            animationPlayer = GetComponent<Animation>();
        }

        // Crossfades to the specified animation if it exists.
        public void CrossFadeAnimation(string animationName)
        {
            if (animationNamesToDurations.ContainsKey(animationName))
            {
                animationPlayer.CrossFade(animationName, crossfadeTime);
            }
        }
        
        // Plays the specified animation immediately if it exists.
        public void PlayAnimation(string animationName)
        {
            if (animationNamesToDurations.ContainsKey(animationName))
            {
                animationPlayer.Play(animationName);
            }
        }

        // Stops all playing animations.
        public void StopAnimations()
        {
            animationPlayer.Stop();
        }
    }
}