using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Behaviour
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class Water : MonoBehaviour
    {
        private MeshFilter meshFilter;


        private void Awake()
        {
            meshFilter= GetComponent<MeshFilter>();
        }

        // Update is called once per frame
        void Update()
        {
            Vector3[] vertices = meshFilter.mesh.vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].y = WaveController.instance.GetWaveHeight(transform.position.x + vertices[i].x * transform.localScale.x);
            }
            meshFilter.mesh.vertices = vertices;
            meshFilter.mesh.RecalculateNormals();
        }
    }
}
