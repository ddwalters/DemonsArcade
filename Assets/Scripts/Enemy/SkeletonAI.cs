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

    private bool attacking;

    public int Damage;

    public bool walk;

    public Animator anim;

    public LayerMask layerMask;

    enum State
    {
        Idle,
        walkingTo,
        attacking,
        swinging,
        hitting
    }

    State state;

    private void Awake()
    {
        target = GameObject.Find("player");
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

    public void takeDamage()
    {
        if(attacking != true)
        {
            //anim.SetTrigger("Attack");
        }
    }

    void FaceTarget()
    {
        if (state != State.swinging)
        {
            Vector3 direction = (target.transform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * lookSpeed);
        }
    }

    void attack()
    {
        navMeshAgent.destination = transform.position;

        if (Time.time >= nextTimeToAttack)
        {
            nextTimeToAttack = Time.time + timeToAttack;
            anim.SetTrigger("Attack");
            StartCoroutine(startAttacking());
        }
    }

    public IEnumerator startAttacking()
    {
        state = State.swinging;
        yield return new WaitForSeconds(0.5f);
        attacking = true;
        StartCoroutine(stopAttacking());
    }

    public IEnumerator stopAttacking()
    {
        state = State.attacking;
        yield return new WaitForSeconds(0.2f);
        attacking = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (layerMask == (layerMask | (1 << other.transform.gameObject.layer)))
        {
            PlayerStatsManager playerHP = other.GetComponent<PlayerStatsManager>();

            if (playerHP != null && attacking == true)
            {
                playerHP.DamagePlayer(Damage);
                attacking = false;
            }
        }
    }
}
