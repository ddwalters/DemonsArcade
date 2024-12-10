using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class KingSlime : MonoBehaviour
{
    public float health;
    private float maxHealth;

    public enum State
    {
        idle,
        jumping,
        shooting,
        slam,
        dash,
        bubble
    }

    State lastState;

    public enum Phase
    {
        none,
        one,
        two
    }

    [Header("Slime King")]
    [SerializeField] Phase phase = Phase.none;
    [SerializeField] State state = State.idle;

    [Space]
    [Header("Settings")]
    public float[] weights = { 0f, 0.6f, 0.3f, 0.1f };
    public LayerMask playerLayer;
    public LayerMask groundLayer;   // Layer mask for ground detection
    [Header("State machine")]
    public float stateSwitchInterval; // Interval between state switches
    public float idleDuration; // Duration of idle state
    [Header("Damage")]
    public float leapDamage;
    public float slimeBallDamage;
    public float slimeWaveDamage; // Damage inflicted to players hit by the wave
    [Header("Leap")]
    public float leapForce;
    public float leapHeight;
    public float pushForce;
    [Header("Slime wave")]
    public float slimeWaveRadius; // Radius of the slime wave effect
    public float slimeWaveForce; // Force applied to players hit by the wave
    [Header("Slime launch")]
    public GameObject slimeBallPrefab; // Slime Ball prefab
    public float slimeBallLaunchForce; // Force to launch the slime ball
    [Space]
    [Header("Components")]
    public GameObject Player;
    private PlayerStatsManager playerStats;
    public BoxCollider collider;
    [SerializeField] Slider mainSlider;
    [SerializeField] Animator sliderAnim;
    [SerializeField] bool hasMultipleBars;
    [SerializeField] Slider slider1;
    [SerializeField] Animator sliderAnim1;
    [SerializeField] Slider slider2;
    [SerializeField] Animator sliderAnim2;
    [Header("Clones")]
    public bool startNextPhase = false;
    [SerializeField] GameObject stuff;
    [SerializeField] GameObject slimeClone1;
    [SerializeField] Transform slimeCloneSpawn1;
    [SerializeField] GameObject slimeClone2;
    [SerializeField] Transform slimeCloneSpawn2;
    private Animator anim;
    private Rigidbody rb;

    private bool isLeaping = false;
    private bool isGrounded;

    void Start()
    {
        Player = FindAnyObjectByType<PlayerController>().gameObject;
        playerStats = Player.GetComponent<PlayerStatsManager>();

        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        maxHealth = health;
    }

    private void slam() => anim.SetTrigger("Slam");

    private void leap() => anim.SetTrigger("Leap");

    private void shoot() => anim.SetTrigger("Shoot");

    public IEnumerator startFight()
    {
        mainSlider.maxValue = health;
        mainSlider.value = maxHealth;
        sliderAnim.SetTrigger("On");
        yield return new WaitForSeconds(3f);
        phase = Phase.one;
        StartCoroutine(StateMachine());
    }

    void Update()
    {
        // Perform ground check
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 0.2f, groundLayer);

        // Draw the raycast in the Scene view
        Debug.DrawRay(transform.position, Vector3.down * 0.2f, Color.red);

        // Rotate to face the player when idle or during leaping
        if (state == State.idle || isLeaping)
        {
            RotateToFacePlayer();
        }

        if (startNextPhase == true)
        {
            startNextPhase = false;
            StartCoroutine(phaseTwo());
        }
            
    }

    private IEnumerator StateMachine()
    {
        while (phase == Phase.one)
        {
            // Select the next state based on weighted randomness
            state = SelectNextState();

            // Execute the action associated with the selected state
            switch (state)
            {
                case State.jumping:
                    yield return StartCoroutine(ExecuteLeap());
                    yield return new WaitForSeconds(3f);
                    isLeaping = false;
                    break;
                case State.shooting:
                    yield return StartCoroutine(ExecuteShoot());
                    break;
                case State.slam:
                    slam();
                    break;
            }

            // Wait for the duration of the current action
            yield return new WaitForSeconds(stateSwitchInterval);

            // Transition to Idle state for a brief moment
            state = State.idle;
            yield return new WaitForSeconds(idleDuration);
        }

        while (phase == Phase.two)
        {
            // Select the next state based on weighted randomness
            state = SelectNextState();

            // Execute the action associated with the selected state
            switch (state)
            {
                case State.dash:
                    
                    break;
                case State.shooting:
                    
                    break;
                case State.bubble:
                    
                    break;
            }

            // Wait for the duration of the current action
            yield return new WaitForSeconds(stateSwitchInterval);

            // Transition to Idle state for a brief moment
            state = State.idle;
            yield return new WaitForSeconds(idleDuration);
        }
    }

    public IEnumerator phaseTwo()
    {
        Debug.Log("spawn clones");
        mainSlider.enabled = false;
        yield return new WaitForSeconds(2f);
        anim.SetTrigger("Split");
    }

    public void spawnClones()
    {
        GameObject clone1 = Instantiate(slimeClone1, slimeCloneSpawn1.position, slimeCloneSpawn1.rotation);
        GameObject clone2 = Instantiate(slimeClone2, slimeCloneSpawn2.position, slimeCloneSpawn2.rotation);

        stuff.SetActive(false);
    }

    public void takeDamage(float damage)
    {
        health -= damage;
        if (health < 0f)
            health = 0f;
        if (health == 0f)
            phaseTwo();

        sliderAnim.SetTrigger("Hurt");

        if (phase == Phase.one)
            mainSlider.value = health;
    }

    public void dealDamage(float damage)
    {
        playerStats.DamagePlayer(damage);
    }

    private IEnumerator ExecuteLeap()
    {
        isLeaping = true;

        yield return null;

        leap();

        yield return new WaitForSeconds(2f); // Adjust timing as needed
        yield return null;

        leap();

        yield return new WaitForSeconds(2f); // Adjust timing as needed
        yield return null;

        leap();

        yield return null;
    }

    private IEnumerator ExecuteShoot()
    {
        for (int i = 0; i < 5; i++)
        {
            shoot();
            yield return new WaitForSeconds(0.4f);
        }
    }

    public void LeapAttack()
    {
        if (Player == null || !isGrounded) Debug.Log("Jump");
        else
        {
            Vector3 direction = (Player.transform.position - transform.position).normalized;
            direction.y = 0;
            Vector3 leapVector = direction * leapForce + Vector3.up * leapHeight;
            rb.AddForce(leapVector, ForceMode.Impulse);
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        // Check if collided object's layer matches the playerLayer
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            // Reduce King Slime's horizontal velocity to fall straight down
            Vector3 currentVelocity = rb.velocity;
            rb.velocity = new Vector3(0, currentVelocity.y, 0); // Zero out horizontal velocity

            dealDamage(leapDamage);
        }
    }

    public void TriggerSlam()
    {
        rb.AddForce(Vector3.up * 230f, ForceMode.Impulse);

        // Start coroutine to check when King Slime hits the ground after slam
        StartCoroutine(CheckGroundedAfterSlam());
    }

    public void TriggerSmallJump() => rb.AddForce(Vector3.up * 100f, ForceMode.Impulse);

    private IEnumerator CheckGroundedAfterSlam()
    {
        yield return new WaitForSeconds(1f);
        yield return new WaitUntil(() => isGrounded); // Wait until King Slime is grounded after slam

        // Once grounded, trigger the slime wave attack
        TriggerSlimeWave();
    }

    public void TriggerSlimeWave()
    {
        anim.SetTrigger("Land");

        // Detect players within the wave radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, slimeWaveRadius, playerLayer);

        foreach (Collider hitCollider in hitColliders)
        {
            dealDamage(slimeWaveDamage);

            PlayerController playerController = hitCollider.gameObject.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // Deal damage to player (implement this according to your game's damage system)

                // Calculate push direction (away from the King Slime)
                Vector3 pushDirection = (hitCollider.transform.position - transform.position).normalized;

                // Apply push force to the player
                Rigidbody playerRB = playerController.GetComponent<Rigidbody>();

                // Apply upward force to make the player jump a little
                playerRB.AddForce(pushDirection * slimeWaveForce + Vector3.up * 5f, ForceMode.Impulse);
            }
        }
    }

    public void LaunchSlimeBall()
    {
        if (slimeBallPrefab == null || Player == null) return;

        // Instantiate the slime ball at the King Slime's position
        GameObject slimeBall = Instantiate(slimeBallPrefab, transform.position + Vector3.up, Quaternion.identity);

        // Calculate the direction to launch the slime ball towards the player
        Vector3 targetPosition = Player.transform.position;
        Vector3 launchVelocity = CalculateLaunchVelocity(slimeBall.transform.position, targetPosition, slimeBallLaunchForce);

        // Apply the calculated force to the slime ball
        Rigidbody slimeBallRB = slimeBall.GetComponent<Rigidbody>();
        slimeBallRB.velocity = launchVelocity;
    }

    // Method to calculate the launch velocity required to hit the target
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

    private void RotateToFacePlayer()
    {
        if (Player != null)
        {
            Vector3 direction = (Player.transform.position - transform.position).normalized;
            direction.y = 0; // Keep only horizontal direction

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f); // Adjust speed as necessary
            }
        }
    }

    // Method to select the next state based on weighted randomness
    private State SelectNextState()
    {
        // Weights for each state: [idle, jumping, shooting, slam]
        float totalWeight = 1f;

        float randomValue = Random.value * totalWeight;

        float cumulativeWeight = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulativeWeight += weights[i];
            if (randomValue < cumulativeWeight)
            {
                State selectedState = (State)i;

                // Check if the selected state is the same as the last state (unless it's the leap attack)
                if (selectedState != State.jumping && selectedState == lastState)
                {
                    // Re-roll for a new state
                    return SelectNextState();
                }

                return selectedState;
            }
        }

        // Fallback to idle state if no other state is selected (should not happen)
        return State.idle;
    }
}
