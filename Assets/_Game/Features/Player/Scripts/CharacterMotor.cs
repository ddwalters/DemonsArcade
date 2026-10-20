using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMotor : MonoBehaviour
{
    [Header("Ground movement")]
    [SerializeField] private Animator animator;
    [SerializeField, Min(0.01f)] private float movementBlendTime = 0.25f;
    [SerializeField, Min(0.01f)] private float stoppingBlendTime = 0.14f;
    [SerializeField] private LayerMask groundMask = ~((1 << 9) | (1 << 5));
    [SerializeField, Min(0f)] private float groundProbeDistance = 0.15f;

    [Header("Air movement")]
    [SerializeField] private float gravity = -24f;
    [SerializeField, Min(0f)] private float jumpHeight = 1.1f;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;
    [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
    [SerializeField, Min(0f)] private float airSpeed = 2.5f;
    [SerializeField, Min(0f)] private float airAcceleration = 4f;
    [SerializeField, Min(0f)] private float knockbackBraking = 8f;

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int Grounded = Animator.StringToHash("Grounded");

    private CharacterController controller;
    private Vector2 animationInput;
    private Vector2 blendVelocity;
    private Vector3 airVelocity;
    private Vector3 externalVelocity;
    private Vector3 groundNormal = Vector3.up;
    private float verticalVelocity;
    private float lastGroundedTime = float.NegativeInfinity;
    private float jumpRequestedTime = float.NegativeInfinity;
    private float lastJumpTime = float.NegativeInfinity;
    private bool movementAllowed;

    public bool IsGrounded { get; private set; }
    public Vector2 AnimationInput => animationInput;
    public Animator Animator => animator;

    private void Awake() => controller = GetComponent<CharacterController>();

    public void RequestJump() => jumpRequestedTime = Time.time;

    // Input selects and blends motion. The animation supplies the actual ground displacement.
    public void Tick(Vector2 input, bool sprinting, bool allowsMovement, float deltaTime)
    {
        if (deltaTime <= 0f || !controller.enabled)
            return;

        movementAllowed = allowsMovement;
        input = allowsMovement ? Vector2.ClampMagnitude(input, 1f) : Vector2.zero;
        UpdateGrounding();

        Vector2 target = input * (sprinting ? 2f : 1f);
        float blendTime = target.sqrMagnitude < 0.001f ? stoppingBlendTime : movementBlendTime;
        animationInput = Vector2.SmoothDamp(animationInput, target, ref blendVelocity,
            blendTime, Mathf.Infinity, deltaTime);

        if (IsGrounded)
        {
            lastGroundedTime = Time.time;
            verticalVelocity = -2f;
        }

        if (allowsMovement && Time.time - jumpRequestedTime <= jumpBufferTime &&
            Time.time - lastGroundedTime <= coyoteTime)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpRequestedTime = float.NegativeInfinity;
            lastGroundedTime = float.NegativeInfinity;
            lastJumpTime = Time.time;
            IsGrounded = false;
        }

        if (!IsGrounded)
        {
            verticalVelocity += gravity * deltaTime;
            Vector3 targetVelocity = transform.TransformDirection(new Vector3(input.x, 0f, input.y)) * airSpeed;
            airVelocity = Vector3.MoveTowards(airVelocity, targetVelocity, airAcceleration * deltaTime);
        }

        animator.SetFloat(MoveX, animationInput.x);
        animator.SetFloat(MoveY, animationInput.y);
        animator.SetBool(Grounded, IsGrounded);
    }

    private void UpdateGrounding()
    {
        IsGrounded = false;
        groundNormal = Vector3.up;
        if (verticalVelocity > 0f || Time.time - lastJumpTime < 0.1f)
            return;

        float radius = controller.radius * 0.9f;
        Vector3 center = transform.TransformPoint(controller.center);
        Vector3 origin = center + Vector3.down * (controller.height * 0.5f - controller.radius) + Vector3.up * 0.05f;
        float distance = groundProbeDistance + controller.radius - radius + 0.05f;
        if (Physics.SphereCast(origin, radius, Vector3.down, out var hit, distance,
                groundMask, QueryTriggerInteraction.Ignore) &&
            Vector3.Angle(hit.normal, Vector3.up) <= controller.slopeLimit)
        {
            IsGrounded = true;
            groundNormal = hit.normal;
        }
    }

    public void ApplyRootMotion(Vector3 displacement, float deltaTime)
    {
        if (deltaTime <= 0f || !controller.enabled)
            return;

        displacement.y = 0f;
        if (!movementAllowed)
        {
            displacement = Vector3.zero;
            airVelocity = Vector3.zero;
        }
        else if (IsGrounded)
        {
            airVelocity = displacement / deltaTime;
            displacement = Vector3.ProjectOnPlane(displacement, groundNormal);
        }
        else
            displacement = airVelocity * deltaTime;

        displacement += (Vector3.up * verticalVelocity + externalVelocity) * deltaTime;
        CollisionFlags collisions = controller.Move(displacement);
        if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;

        externalVelocity = Vector3.MoveTowards(externalVelocity, Vector3.zero, knockbackBraking * deltaTime);
    }

    public void AddImpulse(Vector3 velocityChange)
    {
        externalVelocity += new Vector3(velocityChange.x, 0f, velocityChange.z);
        verticalVelocity += velocityChange.y;
        if (velocityChange.y > 0f)
        {
            IsGrounded = false;
            lastGroundedTime = float.NegativeInfinity;
        }
    }

    public void Stop()
    {
        movementAllowed = false;
        animationInput = Vector2.zero;
        blendVelocity = Vector2.zero;
        airVelocity = Vector3.zero;
        externalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        jumpRequestedTime = float.NegativeInfinity;
        lastGroundedTime = float.NegativeInfinity;
        if (animator != null)
        {
            animator.SetFloat(MoveX, 0f);
            animator.SetFloat(MoveY, 0f);
        }
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        Stop();
        bool wasEnabled = controller.enabled;
        controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        controller.enabled = wasEnabled;
        IsGrounded = false;
        lastJumpTime = float.NegativeInfinity;
    }
}
