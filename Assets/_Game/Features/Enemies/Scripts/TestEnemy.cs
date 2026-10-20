using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TestEnemy : MonoBehaviour
{
    public GameObject target;
    private NavMeshAgent navMeshAgent;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        navMeshAgent.destination = target.transform.position;

        float distance = Vector3.Distance(target.transform.position, transform.position);

        if (distance <= navMeshAgent.stoppingDistance)
        {
            //Attack Target
        }
    }
}
