using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Circling : MonoBehaviour
{
    public float speed = 1.0f;      // Speed of rotation (radians per second)
    public float radius = 5.0f;     // Radius of the circular path

    private float angle = 0.0f;     // Current angle in radians
    private Vector3 center;         // Center point of the circle
    private Vector3 lastPosition;   // For calculating movement direction

    void Start()
    {
        center = transform.position;
        lastPosition = transform.position;
    }

    void Update()
    {
        angle += speed * Time.deltaTime;

        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;

        Vector3 newPosition = new Vector3(center.x + x, transform.position.y, center.z + z);

        // Calculate direction and rotate to face it
        Vector3 direction = (newPosition - lastPosition).normalized;
        if (direction != Vector3.zero)
        {
            transform.forward = direction;
        }

        // Apply new position
        transform.position = newPosition;
        lastPosition = newPosition;
    }
}
