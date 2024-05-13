using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SkeletonAI : MonoBehaviour
{
    public GameObject target;
    private NavMeshAgent navMeshAgent;

    public float stoppingDistance;
    public float lookSpeed;
    public float timeToAttack;
    private float nextTimeToAttack = 0f;
    public int Damage;

    public bool walk;

    public Animator anim;

    public MeshCollider hurtCone;

    public LayerMask layerMask;

    enum State
    {
        Idle,
        walkingTo,
        attacking
    }

    State state;

    private void Awake()
    {
        target = GameObject.Find("player");
        hurtCone.enabled = false;
        state = State.Idle;
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.stoppingDistance = stoppingDistance;
    }

    private void Update()
    {
        float distance = Vector3.Distance(target.transform.position, transform.position);

        if (distance <= navMeshAgent.stoppingDistance + 0.5f || Time.time < nextTimeToAttack)
        {
            navMeshAgent.destination = transform.position;
            FaceTarget();
            state = State.attacking;
            attack();
            walk = false;
        }
        else
        {
            navMeshAgent.destination = target.transform.position;
            state = State.walkingTo;
            walk = true;
        }
        anim.SetBool("Walk", walk);
    }

    void FaceTarget()
    {
        Vector3 direction = (target.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * lookSpeed);
    }

    void attack()
    {
        navMeshAgent.destination = transform.position;

        if (Time.time >= nextTimeToAttack)
        {
            nextTimeToAttack = Time.time + timeToAttack;
            anim.SetTrigger("Attack");
            Invoke("checkCollide", 0.7f);
        }
    }

    void checkCollide()
    {
        hurtCone.enabled = true;
        Invoke("disableCollide", 0.1f);
    }

    void disableCollide()
    {
        hurtCone.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (layerMask == (layerMask | (1 << other.transform.gameObject.layer)))
        {
            PlayerStatsManager playerHP = other.GetComponent<PlayerStatsManager>();

            if (playerHP != null)
            {
                playerHP.DamagePlayer(Damage);
                Debug.Log("Hit player for: " + Damage);
            }
        }
        hurtCone.enabled = false;
    }
}
