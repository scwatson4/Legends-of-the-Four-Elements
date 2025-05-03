using UnityEngine;

public class AutoTargetNearby : MonoBehaviour
{
    private Unit unit;
    private AttackController attackController;
    public float checkInterval = 1.5f;
    private float timer;

    void Start()
    {
        unit = GetComponent<Unit>();
        attackController = GetComponent<AttackController>();
        timer = checkInterval;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            if (attackController.targetToAttack == null)
            {
                FindAndAssignTarget();
            }

            timer = checkInterval;
        }
    }

    void FindAndAssignTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackController.detectionRadius);
        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (var hit in hits)
        {
            Unit otherUnit = hit.GetComponent<Unit>();
            if (otherUnit != null && unit.IsHostileTo(otherUnit.team))
            {
                float distance = Vector3.Distance(transform.position, otherUnit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = otherUnit.transform;
                }
            }
        }

        if (closestEnemy != null)
        {
            attackController.targetToAttack = closestEnemy;
        }
    }
}
