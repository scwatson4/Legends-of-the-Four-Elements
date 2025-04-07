using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    public class WaveController : MonoBehaviour
    {
        public static WaveController instance;

        public float amplitude = 1f;
        public float lenght = 10f;
        public float speed = 1f;

        [HideInInspector]
        public List<GameObject> waveInfluencedObjects = new List<GameObject>();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(this);
            }
        }

        public float GetWaveHeight(float x)
        {
            return amplitude * Mathf.Sin(x / lenght + (Time.time * speed));
        }

        public Vector3 GetWaveNormal(float x)
        {
            return new Vector3(Mathf.Cos(x / lenght + (Time.time * speed)) * amplitude * -0.1f, 1, 0).normalized;
        }
    }
}
