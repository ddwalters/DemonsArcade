using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class KingSlimeClone : MonoBehaviour
{
    public float health;
    private float maxHealth;

    public enum State
    {
        idle,
        shooting,
        dash,
        bubble
    }

    [SerializeField] private State state = State.idle;
    [Header("Slime launch")]
    public GameObject slimeBallPrefab; // Slime Ball prefab
    public float slimeBallLaunchForce; // Force to launch the slime ball

    private GameObject player;
    private NavMeshAgent agent;
    private PlayerStatsManager playerStats;
    private Animator anim;

    [Header("Circling Settings")]
    public float circlingDistance = 5f; // Distance from the player to maintain
    public float circlingSpeed = 1f;    // Speed at which to circle around the player
    public float avoidanceDistance = 2f; // Minimum distance to keep from other slimes

    private Vector3 circleCenter;
    private float angleOffset; // Unique angle offset for each slime

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>().gameObject;
        playerStats = player.GetComponent<PlayerStatsManager>();

        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        maxHealth = health;

        // Assign a unique angle offset to each slime clone
        angleOffset = Random.Range(0, 2 * Mathf.PI);
    }

    void Update()
    {
        switch (state)
        {
            case State.idle:
                HandleCircling();
                break;
            case State.shooting:
                // Implement other states if necessary
                break;
            case State.dash:
                // Implement other states if necessary
                break;
            case State.bubble:
                // Implement other states if necessary
                break;
        }
    }

    private void HandleCircling()
    {
        if (player != null)
        {
            // Calculate the new position around the player
            circleCenter = player.transform.position;
            Vector3 targetPosition = CalculateCirclingPosition(circleCenter, circlingDistance, angleOffset);

            // Set the destination for the NavMeshAgent
            agent.SetDestination(targetPosition);

            // Update the angle offset to circle the player
            angleOffset += circlingSpeed * Time.deltaTime;
        }
    }

    private Vector3 CalculateCirclingPosition(Vector3 center, float radius, float angle)
    {
        float x = center.x + radius * Mathf.Cos(angle);
        float z = center.z + radius * Mathf.Sin(angle);

        return new Vector3(x, center.y, z);
    }

    private void AvoidOtherSlimes()
    {
        // Find all other slimes in the scene
        KingSlimeClone[] allSlimes = FindObjectsOfType<KingSlimeClone>();

        foreach (var slime in allSlimes)
        {
            if (slime == this) continue; // Skip itself

            float distance = Vector3.Distance(transform.position, slime.transform.position);
            if (distance < avoidanceDistance)
            {
                Vector3 directionAway = transform.position - slime.transform.position;
                agent.SetDestination(transform.position + directionAway.normalized * avoidanceDistance);
            }
        }
    }

    public void LaunchSlimeBall()
    {
        if (slimeBallPrefab == null || player == null) return;

        // Instantiate the slime ball at the King Slime's position
        GameObject slimeBall = Instantiate(slimeBallPrefab, transform.position + Vector3.up, Quaternion.identity);

        // Calculate the direction to launch the slime ball towards the player
        Vector3 targetPosition = player.transform.position;
        Vector3 launchVelocity = CalculateLaunchVelocity(slimeBall.transform.position, targetPosition, slimeBallLaunchForce);

        // Apply the calculated force to the slime ball
        Rigidbody slimeBallRB = slimeBall.GetComponent<Rigidbody>();
        slimeBallRB.velocity = launchVelocity;
    }

    private Vector3 CalculateLaunchVelocity(Vector3 start, Vector3 target, float initialSpeed)
    {
        // Vector to the target from the start
        Vector3 direction = target - start;
        Vector3 horizontalDirection = new Vector3(direction.x, 0, direction.z); // Ignore vertical component
        float horizontalDistance = horizontalDirection.magnitude; // Horizontal distance
        float verticalDistance = direction.y; // Vertical distance

        float gravity = Physics.gravity.y;

        // Calculate the angle required to hit the target (simplification for 45 degrees)
        float angleInRadians = Mathf.Deg2Rad * 45f; // Adjust as needed for trajectory height

        // Calculate the initial velocity components using projectile motion equations
        float initialVelocityY = initialSpeed * Mathf.Sin(angleInRadians);
        float initialVelocityXZ = initialSpeed * Mathf.Cos(angleInRadians);

        // If the target is very close, adjust the force to avoid overshooting
        if (horizontalDistance < 0.1f)
        {
            initialVelocityXZ = horizontalDistance / Time.fixedDeltaTime;
            initialVelocityY = verticalDistance / Time.fixedDeltaTime;
        }

        // Ensure the initial speed is enough to reach the target
        if (initialSpeed < Mathf.Sqrt(horizontalDistance * -gravity / (2 * (verticalDistance - Mathf.Tan(angleInRadians) * horizontalDistance))))
        {
            initialSpeed = Mathf.Sqrt(horizontalDistance * -gravity / (2 * (verticalDistance - Mathf.Tan(angleInRadians) * horizontalDistance)));
            initialVelocityY = initialSpeed * Mathf.Sin(angleInRadians);
            initialVelocityXZ = initialSpeed * Mathf.Cos(angleInRadians);
        }

        // Combine the horizontal and vertical components into a final velocity vector
        Vector3 launchDirection = horizontalDirection.normalized * initialVelocityXZ;
        launchDirection.y = initialVelocityY;

        return launchDirection;
    }

    // Use this function to disable NavMeshAgent when not in idle state
    public void SetState(State newState)
    {
        state = newState;
        agent.enabled = (state == State.idle);
    }
}
