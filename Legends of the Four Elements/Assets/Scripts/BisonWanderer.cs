using UnityEngine;
using System.Collections;

public class BisonWanderer : MonoBehaviour
{
    [Header("Roaming Area")]
    public Vector3 centerPoint;
    public Vector3 bounds = new Vector3(15f, 3f, 15f); // tighter vertical range

    [Header("Speed & Rotation")]
    public float moveSpeedMin = 0.7f;
    public float moveSpeedMax = 1.2f;
    public float turnSpeedMin = 0.5f;
    public float turnSpeedMax = 1.0f;

    [Header("Pause Between Targets")]
    public float waitTimeMin = 3f;
    public float waitTimeMax = 5f;

    [Header("Rotation Alignment")]
    public float rotationOffsetY = -90f;

    private Vector3 targetPosition;
    private bool waiting = false;
    private float moveSpeed;
    private float turnSpeed;

    void Start()
    {
        moveSpeed = Random.Range(moveSpeedMin, moveSpeedMax);
        turnSpeed = Random.Range(turnSpeedMin, turnSpeedMax);

        float delay = Random.Range(0f, 2f);
        StartCoroutine(DelayedStart(delay));
    }

    IEnumerator DelayedStart(float delay)
    {
        yield return new WaitForSeconds(delay);
        PickNewTarget();
    }

    void Update()
    {
        if (waiting || targetPosition == Vector3.zero) return;

        Vector3 direction = (targetPosition - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            targetRotation *= Quaternion.Euler(0f, rotationOffsetY, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
        {
            StartCoroutine(WaitAndPickNewTarget());
        }
    }

    void PickNewTarget()
    {
        float x = Random.Range(centerPoint.x - bounds.x / 2f, centerPoint.x + bounds.x / 2f);
        float y = Random.Range(centerPoint.y - bounds.y / 2f, centerPoint.y + bounds.y / 2f);
        float z = Random.Range(centerPoint.z - bounds.z / 2f, centerPoint.z + bounds.z / 2f);

        targetPosition = new Vector3(x, y, z);
    }

    IEnumerator WaitAndPickNewTarget()
    {
        waiting = true;
        float waitTime = Random.Range(waitTimeMin, waitTimeMax);
        yield return new WaitForSeconds(waitTime);
        PickNewTarget();
        waiting = false;
    }
}
