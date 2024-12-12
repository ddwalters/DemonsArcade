using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SkeletonAI : MonoBehaviour
{
    [Header("Attributes")]
    [SerializeField] float Damage;
    [SerializeField] bool hasShield;
    [SerializeField] bool canSpawnWithArmor;
    [SerializeField] bool advancedCombat;
    [Header("Settings")]
    [SerializeField] public float stoppingDistance;
    [SerializeField] public float lookSpeed;
    [SerializeField] public float timeToAttack;
    [Space]
    [Header("Components")]
    [SerializeField] EnemyDamageDealer damageDealer;
    public GameObject target;
    public Animator anim;
    private NavMeshAgent navMeshAgent;
    public LayerMask layerMask;

    private bool attacking = false;
    private float nextTimeToAttack = 0f;

    enum State
    {
        Idle,
        walkingTo,
        attacking,
        waiting
    }

    State state;

    private void Awake()
    {
        target = GameObject.Find("player");
        state = State.Idle;
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.stoppingDistance = stoppingDistance;

        damageDealer.SetDamage(Damage);
        damageDealer.SetLayerMask(layerMask);
    }

    private void Update()
    {
        anim.SetFloat("speed", navMeshAgent.velocity.magnitude / navMeshAgent.speed);
        float distance = Vector3.Distance(target.transform.position, transform.position);

        if (distance <= navMeshAgent.stoppingDistance + 0.5f)
        {
            navMeshAgent.destination = transform.position;
            if (attacking == false) // Only face target if not attacking
            {
                FaceTarget();
            }
            // Ensure the skeleton is facing the target to attack
            if (IsFacingTarget() && Time.time >= nextTimeToAttack)
            {
                attack();
            }
        }
        else
        {
            navMeshAgent.destination = target.transform.position;
            state = State.walkingTo;
        }
    }

    public void takeDamage()
    {
        anim.SetTrigger("damage");
    }

    void FaceTarget()
    {
        Vector3 direction = (target.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * lookSpeed);
    }

    bool IsFacingTarget()
    {
        Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
        float dotProduct = Vector3.Dot(transform.forward, new Vector3(directionToTarget.x, 0, directionToTarget.z));
        return dotProduct > 0.75f; // Adjust this threshold as needed (close to 1 means facing the target)
    }

    void attack()
    {
        navMeshAgent.destination = transform.position;

        if (Time.time >= nextTimeToAttack)
        {
            nextTimeToAttack = Time.time + timeToAttack;
            anim.SetTrigger("attack");
        }
    }

    public void StartAnimation() => attacking = true;

    public void EndAnimation() => attacking = false;

    public void StartDealDamage() => damageDealer.StartDealDamage();

    public void EndDealDamage() => damageDealer.EndDealDamage();
}