using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SlimeAI : MonoBehaviour
{
    public GameObject target;
    private NavMeshAgent navMeshAgent;

    public float lookSpeed;

    private void Awake()
    {
        target = GameObject.Find("player");
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        navMeshAgent.destination = target.transform.position;

        float distance = Vector3.Distance(target.transform.position, transform.position);

        if (distance <= navMeshAgent.stoppingDistance + 0.5f)
        {
            //Attack();
            FaceTarget();
        }
    }

    void FaceTarget()
    {
        Vector3 direction = (target.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * lookSpeed);
    }
}
