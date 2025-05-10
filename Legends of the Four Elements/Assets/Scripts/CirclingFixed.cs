using UnityEngine;

public class CirclingFixed : MonoBehaviour
{
    public float speed = 1.0f;              // Speed of rotation (radians per second)
    public float radius = 5.0f;             // Radius of the circular path
    public float rotationOffsetY = -90f;    // Y-axis offset to align model's front
    public float rotationSmoothness = 5f;   // Rotation smoothing factor

    private float angle = 0.0f;
    private Vector3 center;
    private Vector3 lastPosition;

    void Start()
    {
        center = transform.position;
        lastPosition = transform.position;
    }

    void Update()
    {
        // Circular movement calculation
        angle += speed * Time.deltaTime;
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;
        Vector3 newPosition = new Vector3(center.x + x, transform.position.y, center.z + z);

        // Rotation to face movement direction
        Vector3 direction = (newPosition - lastPosition).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            targetRotation *= Quaternion.Euler(0f, rotationOffsetY, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothness);
        }

        // Apply position update
        transform.position = newPosition;
        lastPosition = newPosition;
    }
}
