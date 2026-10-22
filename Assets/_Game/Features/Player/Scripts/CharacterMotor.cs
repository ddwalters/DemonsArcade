using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMotor : MonoBehaviour
{
    [Header("Ground movement")]
    [SerializeField] private Animator animator;
    [SerializeField, Min(0f)] private float walkSpeed = 2.2f;
    [SerializeField, Min(0f)] private float sprintSpeed = 5f;
    [SerializeField, Min(0f)] private float acceleration = 30f;
    [SerializeField, Min(0f)] private float braking = 40f;
    [SerializeField, Min(0f)] private float turnSpeed = 720f;
    [SerializeField, Min(0.01f)] private float animationBlendTime = 0.08f;
    [SerializeField] private LayerMask groundMask = ~((1 << 9) | (1 << 5));
    [SerializeField, Min(0f)] private float groundProbeDistance = 0.15f;

    [Header("Air movement")]
    [SerializeField] private float gravity = -24f;
    [SerializeField, Min(0f)] private float jumpHeight = 1.1f;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;
    [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
    [SerializeField, Min(0f)] private float airAcceleration = 4f;
    [SerializeField, Min(0f)] private float knockbackBraking = 8f;

    [Header("Jump animation timing")]
    [SerializeField, Min(0.01f)] private float idleLandingRecoveryTime = 0.18f;
    [SerializeField, Min(0.01f)] private float movingLandingRecoveryTime = 0.1f;
    [SerializeField, Min(0f)] private float jumpAnimationFallGrace = 0.12f;
    [SerializeField, Min(0f)] private float groundProbeContactTolerance = 0.02f;
    [SerializeField, Min(0f)] private float fallPresentationGrace = 0.07f;
    [SerializeField, Range(0f, 0.05f)] private float jumpContactPhaseLead = 0.002f;

    [Serializable]
    private struct JumpAnimationPhases
    {
        [Range(0f, 1f), Tooltip("Clip time where the character leaves the ground.")]
        public float takeoff;
        [Range(0f, 1f), Tooltip("Clip time where vertical velocity reaches zero.")]
        public float apex;
        [Range(0f, 1f), Tooltip("Clip time where the feet make landing contact.")]
        public float contact;
        [Range(0f, 1f), Tooltip("Clip time where the landing recovery is complete.")]
        public float recovered;

        public JumpAnimationPhases(float takeoff, float apex, float contact, float recovered)
        {
            this.takeoff = takeoff;
            this.apex = apex;
            this.contact = contact;
            this.recovered = recovered;
        }
    }

    [Header("Jump clip phase markers")]
    [SerializeField] private JumpAnimationPhases normalJumpPhases =
        new JumpAnimationPhases(16f / 57f, 24f / 57f, 34f / 57f, 48f / 57f);
    [SerializeField] private JumpAnimationPhases sprintJumpPhases =
        new JumpAnimationPhases(2f / 27f, 10f / 27f, 19f / 27f, 1f);

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int Grounded = Animator.StringToHash("Grounded");
    private static readonly int Jumping = Animator.StringToHash("Jumping");
    private static readonly int SprintJump = Animator.StringToHash("SprintJump");
    private static readonly int VerticalVelocityParameter = Animator.StringToHash("VerticalVelocity");
    private static readonly int JumpTime = Animator.StringToHash("JumpTime");
    private static readonly int LandingActive = Animator.StringToHash("LandingActive");

    private CharacterController controller;
    private Vector2 animationInput;
    private Vector3 planarVelocity;
    private Vector3 externalVelocity;
    private float verticalVelocity;
    private float actualPlanarSpeed;
    private float animationBlendVelocity;
    private float coyoteTimeRemaining;
    private float jumpRequestAge;
    private float jumpGroundingGrace;
    private float jumpAnimationElapsed;
    private float jumpAnimationFlightTime;
    private float jumpAnimationLaunchSpeed;
    private float jumpAnimationTime;
    private float landingRecoveryElapsed;
    private float landingRecoveryDuration;
    private float airbornePresentationElapsed;
    private bool jumpAnimationActive;
    private bool landingAnimationActive;
    private bool sprintJumpAnimation;
    private bool jumpRequested;
    private bool hasInitializedGroundState;
    private bool presentationGroundedLatch;
    private bool foundWalkableGroundContact;
    private Vector3 walkableGroundContactNormal = Vector3.up;

    public bool IsGrounded { get; private set; }
    public Vector2 AnimationInput => animationInput;
    public Animator Animator => animator;
    public Vector3 PlanarVelocity => planarVelocity;
    public float PlanarSpeed => planarVelocity.magnitude;
    public float ActualPlanarSpeed => actualPlanarSpeed;
    public float VerticalVelocity => verticalVelocity;
    public float JumpAnimationTime => jumpAnimationTime;
    public bool IsJumpAnimationActive => jumpAnimationActive;
    public bool IsLandingAnimationActive => landingAnimationActive;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.applyRootMotion = false;
    }

    public void RequestJump()
    {
        jumpRequested = true;
        jumpRequestAge = 0f;
    }

    // The motor is the sole owner of player translation. Animators only read its state.
    public void Tick(Vector2 input, bool sprinting, bool allowsMovement, float deltaTime,
        Vector3? worldMovement = null)
    {
        if (controller == null || deltaTime <= 0f)
            return;

        if (!controller.enabled)
            return;

        if (!allowsMovement)
        {
            input = Vector2.zero;
            sprinting = false;
            jumpRequested = false;
            jumpRequestAge = 0f;
            planarVelocity = Vector3.zero;
            externalVelocity = Vector3.zero;
        }
        else
        {
            input = Vector2.ClampMagnitude(input, 1f);
        }

        Vector3 requestedDirection = GetRequestedDirection(input, worldMovement);
        float inputMagnitude = input.magnitude;
        bool hasMovementInput = inputMagnitude > 0.001f && requestedDirection.sqrMagnitude > 0.001f;
        float movementSpeed = sprinting ? sprintSpeed : walkSpeed;
        Vector3 targetVelocity = hasMovementInput
            ? requestedDirection * (movementSpeed * inputMagnitude)
            : Vector3.zero;

        if (hasMovementInput)
        {
            Quaternion targetFacing = Quaternion.LookRotation(requestedDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetFacing, turnSpeed * deltaTime);
        }

        bool previouslyGrounded = IsGrounded;
        bool groundedAtStart = TryGetGround(out Vector3 groundNormal);
        IsGrounded = groundedAtStart;
        if (groundedAtStart)
        {
            coyoteTimeRemaining = coyoteTime;
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;
        }
        else
        {
            coyoteTimeRemaining = Mathf.Max(0f, coyoteTimeRemaining - deltaTime);
        }

        bool jumpIsBuffered = jumpRequested && jumpRequestAge <= jumpBufferTime;
        bool acceptedJumpThisTick = false;
        if (jumpIsBuffered && (groundedAtStart || coyoteTimeRemaining > 0f))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpRequested = false;
            coyoteTimeRemaining = 0f;
            jumpGroundingGrace = 0.1f;
            jumpAnimationActive = true;
            sprintJumpAnimation = sprinting && hasMovementInput;
            landingAnimationActive = false;
            landingRecoveryElapsed = 0f;
            jumpAnimationElapsed = 0f;
            jumpAnimationLaunchSpeed = verticalVelocity;
            jumpAnimationFlightTime = jumpAnimationLaunchSpeed * 2f / Mathf.Max(0.01f, -gravity);
            jumpAnimationTime = GetTakeoffPhase(sprintJumpAnimation);
            acceptedJumpThisTick = true;
            IsGrounded = false;
            groundedAtStart = false;
        }
        else if (jumpRequested)
        {
            jumpRequestAge += deltaTime;
            if (jumpRequestAge > jumpBufferTime)
                jumpRequested = false;
        }

        jumpGroundingGrace = Mathf.Max(0f, jumpGroundingGrace - deltaTime);
        if (groundedAtStart)
        {
            float rate = hasMovementInput ? acceleration : braking;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, rate * deltaTime);
        }
        else if (hasMovementInput)
        {
            // Keep launch momentum and allow only modest steering while airborne.
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, airAcceleration * deltaTime);
        }

        if (IsGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * deltaTime;

        Vector3 locomotionDisplacement = planarVelocity * deltaTime;
        if (groundedAtStart)
            locomotionDisplacement = Vector3.ProjectOnPlane(locomotionDisplacement, groundNormal);

        Vector3 externalDisplacement = externalVelocity * deltaTime;
        Vector3 displacement = locomotionDisplacement + externalDisplacement +
            Vector3.up * (verticalVelocity * deltaTime);

        foundWalkableGroundContact = false;
        walkableGroundContactNormal = Vector3.up;
        Vector3 startingPosition = transform.position;
        CollisionFlags collisions = controller.Move(displacement);
        Vector3 actualDisplacement = transform.position - startingPosition;

        if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;

        Vector3 probedGroundNormal = Vector3.up;
        bool hasGroundProbe = verticalVelocity <= 0f && TryGetGround(out probedGroundNormal);
        bool hasWalkableGround = verticalVelocity <= 0f &&
            (foundWalkableGroundContact || hasGroundProbe);
        if (hasWalkableGround)
        {
            IsGrounded = true;
            coyoteTimeRemaining = coyoteTime;
            if (foundWalkableGroundContact)
                groundNormal = walkableGroundContactNormal;
            else
                groundNormal = probedGroundNormal;
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;
        }
        else
        {
            IsGrounded = false;
            groundNormal = Vector3.up;
        }

        bool landedThisTick = hasInitializedGroundState && !previouslyGrounded && IsGrounded;
        bool meaningfulLanding = jumpAnimationActive || airbornePresentationElapsed >= fallPresentationGrace;
        bool beganLandingThisTick = false;
        if (IsGrounded)
        {
            if ((landedThisTick && meaningfulLanding) ||
                (IsGrounded && jumpAnimationActive && !acceptedJumpThisTick))
            {
                BeginLandingRecovery(hasMovementInput || planarVelocity.magnitude > 0.5f);
                beganLandingThisTick = true;
            }

            airbornePresentationElapsed = 0f;
            presentationGroundedLatch = true;
        }
        else
        {
            airbornePresentationElapsed += deltaTime;
            if (acceptedJumpThisTick || airbornePresentationElapsed >= fallPresentationGrace)
                presentationGroundedLatch = false;
            else if (previouslyGrounded)
                presentationGroundedLatch = true;
        }

        if (jumpAnimationActive)
        {
            jumpAnimationElapsed += deltaTime;
            if (jumpAnimationElapsed > jumpAnimationFlightTime + jumpAnimationFallGrace)
            {
                // Fall back to a stable loop if the character remains airborne past the clip's landing window.
                jumpAnimationActive = false;
            }
            else
            {
                jumpAnimationTime = CalculateAirborneJumpPhase(sprintJumpAnimation, verticalVelocity);
            }
        }

        if (landingAnimationActive)
        {
            if (!beganLandingThisTick)
                landingRecoveryElapsed += deltaTime;
            float recoveryProgress = landingRecoveryDuration > 0f
                ? Mathf.Clamp01(landingRecoveryElapsed / landingRecoveryDuration)
                : 1f;
            jumpAnimationTime = Mathf.Lerp(GetContactPhase(sprintJumpAnimation),
                GetRecoveredPhase(sprintJumpAnimation), recoveryProgress);

            if (recoveryProgress >= 1f)
            {
                landingAnimationActive = false;
                sprintJumpAnimation = false;
            }
        }

        hasInitializedGroundState = true;

        externalVelocity = Vector3.MoveTowards(
            externalVelocity, Vector3.zero, knockbackBraking * deltaTime);

        Vector3 resolvedPlanarTravel = Vector3.ProjectOnPlane(
            actualDisplacement - externalDisplacement, Vector3.up);
        actualPlanarSpeed = planarVelocity.sqrMagnitude > 0.0001f
            ? Mathf.Clamp(Vector3.Dot(resolvedPlanarTravel, planarVelocity.normalized) / deltaTime,
                0f, planarVelocity.magnitude)
            : 0f;

        UpdateAnimator(GetGaitValue(actualPlanarSpeed), presentationGroundedLatch, deltaTime);
    }

    private Vector3 GetRequestedDirection(Vector2 input, Vector3? worldMovement)
    {
        if (input.sqrMagnitude <= 0.000001f)
            return Vector3.zero;

        Vector3 direction = worldMovement.HasValue
            ? worldMovement.Value
            : transform.TransformDirection(new Vector3(input.x, 0f, input.y));
        direction = Vector3.ProjectOnPlane(direction, Vector3.up);
        return direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector3.zero;
    }

    private bool TryGetGround(out Vector3 normal)
    {
        normal = Vector3.up;
        if (verticalVelocity > 0f || jumpGroundingGrace > 0f)
            return false;

        float radius = Mathf.Max(0.01f, controller.radius * 0.9f);
        Vector3 center = transform.TransformPoint(controller.center);
        Vector3 capsuleBottom = center + Vector3.down * (controller.height * 0.5f - controller.radius);
        const float originLift = 0.05f;
        Vector3 origin = capsuleBottom + Vector3.up * originLift;
        float distance = groundProbeDistance + 0.1f;
        if (!Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit,
                distance, groundMask, QueryTriggerInteraction.Ignore))
            return false;

        if (Vector3.Angle(hit.normal, Vector3.up) > controller.slopeLimit)
            return false;

        // The cast uses a slightly smaller sphere than the controller. Correct its vertical
        // clearance for the probe lift and slope, then allow the CharacterController's skin gap.
        float verticalGap = hit.distance - originLift -
            (controller.radius - radius) / Mathf.Max(0.01f, hit.normal.y);
        float contactAllowance = groundProbeContactTolerance +
            controller.skinWidth / Mathf.Max(0.01f, hit.normal.y);
        if (verticalGap > contactAllowance)
            return false;

        normal = hit.normal;
        return true;
    }

    private void BeginLandingRecovery(bool hasMovementInput)
    {
        jumpAnimationActive = false;
        landingAnimationActive = true;
        landingRecoveryElapsed = 0f;
        landingRecoveryDuration = hasMovementInput
            ? movingLandingRecoveryTime
            : idleLandingRecoveryTime;
        jumpAnimationTime = GetContactPhase(sprintJumpAnimation);
    }

    private float GetTakeoffPhase(bool sprintJump) => GetJumpPhases(sprintJump).takeoff;

    private float GetApexPhase(bool sprintJump) => GetJumpPhases(sprintJump).apex;

    private float GetContactPhase(bool sprintJump) => GetJumpPhases(sprintJump).contact;

    private float GetRecoveredPhase(bool sprintJump) => GetJumpPhases(sprintJump).recovered;

    private JumpAnimationPhases GetJumpPhases(bool sprintJump)
    {
        JumpAnimationPhases phases = sprintJump ? sprintJumpPhases : normalJumpPhases;
        phases.takeoff = Mathf.Clamp01(phases.takeoff);
        phases.apex = Mathf.Clamp(phases.apex, phases.takeoff, 1f);
        phases.contact = Mathf.Clamp(phases.contact, phases.apex, 1f);
        phases.recovered = Mathf.Clamp(phases.recovered, phases.contact, 1f);
        return phases;
    }

    private float CalculateAirborneJumpPhase(bool sprintJump, float currentVerticalVelocity)
    {
        float launchSpeed = Mathf.Max(0.01f, jumpAnimationLaunchSpeed);
        float takeoff = GetTakeoffPhase(sprintJump);
        float apex = GetApexPhase(sprintJump);
        float contact = Mathf.Max(apex, GetContactPhase(sprintJump) - jumpContactPhaseLead);

        if (currentVerticalVelocity > 0f)
        {
            float ascentProgress = 1f - Mathf.Clamp01(currentVerticalVelocity / launchSpeed);
            return Mathf.Lerp(takeoff, apex, ascentProgress);
        }

        float descentProgress = Mathf.Clamp01(-currentVerticalVelocity / launchSpeed);
        return Mathf.Lerp(apex, contact, descentProgress);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (Vector3.Angle(hit.normal, Vector3.up) <= controller.slopeLimit)
        {
            foundWalkableGroundContact = true;
            if (hit.normal.y > walkableGroundContactNormal.y)
                walkableGroundContactNormal = hit.normal;
        }

        if (Vector3.Dot(hit.normal, Vector3.up) >= 0.5f)
            return;

        Vector3 sideNormal = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
        if (sideNormal.sqrMagnitude <= 0.0001f)
            return;

        if (Vector3.Dot(planarVelocity, sideNormal) < 0f)
            planarVelocity = Vector3.ProjectOnPlane(planarVelocity, sideNormal);
        if (Vector3.Dot(externalVelocity, sideNormal) < 0f)
            externalVelocity = Vector3.ProjectOnPlane(externalVelocity, sideNormal);
    }

    private float GetGaitValue(float speed)
    {
        if (speed <= 0.01f || walkSpeed <= 0f)
            return 0f;

        if (speed <= walkSpeed)
            return Mathf.Clamp01(speed / walkSpeed);

        float runRange = Mathf.Max(0.01f, sprintSpeed - walkSpeed);
        return 1f + Mathf.Clamp01((speed - walkSpeed) / runRange);
    }

    private void UpdateAnimator(float gaitValue, bool grounded, float deltaTime = 0f)
    {
        float blendTime = Mathf.Max(0.01f, animationBlendTime);
        float nextGait = deltaTime > 0f
            ? Mathf.SmoothDamp(animationInput.y, gaitValue, ref animationBlendVelocity,
                blendTime, Mathf.Infinity, deltaTime)
            : gaitValue;
        animationInput = new Vector2(0f, nextGait);

        if (animator == null || !animator.isActiveAndEnabled)
            return;

        animator.SetFloat(MoveX, 0f);
        animator.SetFloat(MoveY, animationInput.y);
        animator.SetBool(Grounded, grounded);
        animator.SetBool(Jumping, jumpAnimationActive);
        animator.SetBool(SprintJump, sprintJumpAnimation);
        animator.SetFloat(VerticalVelocityParameter, verticalVelocity);
        animator.SetFloat(JumpTime, jumpAnimationTime);
        animator.SetBool(LandingActive, landingAnimationActive);
    }

    public void AddImpulse(Vector3 velocityChange)
    {
        externalVelocity += new Vector3(velocityChange.x, 0f, velocityChange.z);
        verticalVelocity += velocityChange.y;
        if (velocityChange.y > 0f)
        {
            IsGrounded = false;
            coyoteTimeRemaining = 0f;
            jumpGroundingGrace = 0.1f;
            jumpAnimationActive = false;
            landingAnimationActive = false;
            sprintJumpAnimation = false;
            airbornePresentationElapsed = fallPresentationGrace;
            presentationGroundedLatch = false;
        }
    }

    public void Stop()
    {
        planarVelocity = Vector3.zero;
        externalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        actualPlanarSpeed = 0f;
        animationInput = Vector2.zero;
        animationBlendVelocity = 0f;
        coyoteTimeRemaining = 0f;
        jumpRequestAge = 0f;
        jumpGroundingGrace = 0f;
        jumpAnimationElapsed = 0f;
        jumpAnimationFlightTime = 0f;
        jumpAnimationLaunchSpeed = 0f;
        jumpAnimationTime = 0f;
        landingRecoveryElapsed = 0f;
        landingRecoveryDuration = 0f;
        airbornePresentationElapsed = 0f;
        jumpAnimationActive = false;
        landingAnimationActive = false;
        sprintJumpAnimation = false;
        jumpRequested = false;
        presentationGroundedLatch = IsGrounded;
        if (!IsGrounded)
            airbornePresentationElapsed = fallPresentationGrace;

        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetFloat(MoveX, 0f);
            animator.SetFloat(MoveY, 0f);
            animator.SetBool(Grounded, IsGrounded);
            animator.SetBool(Jumping, false);
            animator.SetBool(SprintJump, false);
            animator.SetFloat(VerticalVelocityParameter, 0f);
            animator.SetFloat(JumpTime, 0f);
            animator.SetBool(LandingActive, false);
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
        hasInitializedGroundState = false;
        presentationGroundedLatch = false;
        airbornePresentationElapsed = fallPresentationGrace;
    }
}
