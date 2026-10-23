using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMotor : MonoBehaviour
{
    [Header("Ground movement")]
    private Animator animator;
    [SerializeField, Min(0f)] private float walkSpeed = 2.2f;
    [SerializeField, Min(0f)] private float sprintSpeed = 5f;
    [SerializeField, Min(0f)] private float acceleration = 30f;
    [SerializeField, Min(0f)] private float braking = 40f;
    [SerializeField, Min(0f)] private float turnSpeed = 720f;
    [SerializeField, Min(0.01f)] private float animationBlendTime = 0.08f;
    [SerializeField] private LayerMask groundMask = ~((1 << 9) | (1 << 5));
    [SerializeField, Min(0f)] private float groundProbeDistance = 0.15f;

    [Header("Steps")]
    [Tooltip("Leave empty to use the shared Resources/DefaultHumanoidLocomotionProfile. Assign one to override it for this character.")]
    [SerializeField] private CharacterLocomotionProfile locomotionProfile;
    [Tooltip("Read only. Measured from the rig at startup.")]
    [SerializeField] private float measuredLegLength;
    [Tooltip("Read only. The profile's step ratio times the measured leg length.")]
    [SerializeField] private float effectiveMaxStepHeight;

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
    [SerializeField, Range(0f, 1f)] private float jumpApexPoseWeight = 0.65f;
    [SerializeField, Min(0.01f)] private float fullFallAnimationTime = 1f;
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

    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int Grounded = Animator.StringToHash("Grounded");
    private static readonly int Jumping = Animator.StringToHash("Jumping");
    private static readonly int SprintJump = Animator.StringToHash("SprintJump");
    private static readonly int JumpTime = Animator.StringToHash("JumpTime");
    private static readonly int JumpPoseWeight = Animator.StringToHash("JumpPoseWeight");
    private static readonly int LandingActive = Animator.StringToHash("LandingActive");
    private static readonly int FallAmount = Animator.StringToHash("FallAmount");
    private const float MinimumFallAnimationSpeed = 0.15f;
    private const string SharedProfileResource = "DefaultHumanoidLocomotionProfile";
    private const float StepRayHeight = 0.012f;
    private const float StepRaySpread = 0.6f;
    private const float StepProbeReach = 0.08f;
    private const float StepVerticalNormalY = 0.3f;
    private const float StepTreadSample = 0.05f;
    private const float StepTreadDepthMargin = 0.05f;
    private const float StepLiftMargin = 0.02f;
    private const float StepSkinLift = 0.01f;
    private const float StepAdvance = 0.06f;
    private const float StepUpGraceTime = 0.15f;
    private const float StepRejectLogInterval = 0.5f;
    private const float StepGroundSearch = 0.2f;
    private const float MinimumStepRise = 0.015f;
    private const float StepHeightTolerance = 0.005f;
    private const float ControllerStepOffsetWithMotorStepUp = 0.05f;
    private const float MinimumReportedStep = 0.02f;
    private const float MaximumReportedStepRatio = 1.25f;

    private CharacterController controller;
    private CharacterLocomotionProfile profile;
    private StepSmoothing stepSmoothing;
    private float legLength;
    private float lastStepRejectLogTime;
    private float stepUpGraceRemaining;
    private float animationGait;
    private Vector3 planarVelocity;
    private Vector3 externalVelocity;
    private float verticalVelocity;
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
    private float fallAnimationElapsed;
    private float fallAmount;
    private bool jumpAnimationActive;
    private bool landingAnimationActive;
    private bool sprintJumpAnimation;
    private bool jumpRequested;
    private bool hasInitializedGroundState;
    private bool presentationGroundedLatch;
    private bool foundWalkableGroundContact;
    private Vector3 walkableGroundContactNormal = Vector3.up;

    public bool IsGrounded { get; private set; }
    public CharacterLocomotionProfile Profile => profile;
    public float LegLength => legLength;
    public float MaxStepHeight => profile.MaxStepHeight(legLength);
    public LayerMask GroundMask => groundMask;
    public Vector3 PlanarVelocity => planarVelocity;

    public event Action<float> SteppedVertically;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.applyRootMotion = false;

        profile = locomotionProfile != null ? locomotionProfile : LoadSharedProfile();
        RefreshRigMetrics();
        AddLocomotionComponents();
    }

    private static CharacterLocomotionProfile LoadSharedProfile()
    {
        CharacterLocomotionProfile shared = Resources.Load<CharacterLocomotionProfile>(SharedProfileResource);
        return shared != null ? shared : CharacterLocomotionProfile.CreateDefault();
    }

    public void RefreshRigMetrics()
    {
        if (!HumanoidLegMetrics.TryMeasure(animator, out legLength))
            legLength = controller.height * 0.5f;

        if (profile.UsesMotorStepUp)
            controller.stepOffset = Mathf.Min(ControllerStepOffsetWithMotorStepUp, controller.height);

        measuredLegLength = legLength;
        effectiveMaxStepHeight = MaxStepHeight;
    }

    private void AddLocomotionComponents()
    {
        stepSmoothing = GetComponent<StepSmoothing>();
        if (stepSmoothing == null)
            stepSmoothing = gameObject.AddComponent<StepSmoothing>();

        stepSmoothing.Initialize(this, animator);

        if (animator == null || !animator.isHuman)
            return;

        FootPlacementIK footIk = animator.GetComponent<FootPlacementIK>();
        if (footIk == null)
            footIk = animator.gameObject.AddComponent<FootPlacementIK>();

        footIk.Initialize(this, animator);
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
        stepUpGraceRemaining = Mathf.Max(0f, stepUpGraceRemaining - deltaTime);
        bool groundedAtStart = TryGetGround(out Vector3 groundNormal) || stepUpGraceRemaining > 0f;
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
        if (profile.UsesMotorStepUp && groundedAtStart && !acceptedJumpThisTick && verticalVelocity <= 0f &&
            TryStepUp(locomotionDisplacement + externalDisplacement, out float stepRise))
        {
            controller.Move(Vector3.up * stepRise);
            stepUpGraceRemaining = StepUpGraceTime;
        }

        CollisionFlags collisions = controller.Move(displacement);
        if (ShouldSnapToGround(previouslyGrounded, acceptedJumpThisTick, collisions))
            SnapToGround();

        Vector3 actualDisplacement = transform.position - startingPosition;
        if (groundedAtStart && !acceptedJumpThisTick && (collisions & CollisionFlags.Sides) != 0)
            LimitVelocityToAchievedMotion(actualDisplacement - externalDisplacement, deltaTime);

        if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;

        Vector3 probedGroundNormal = Vector3.up;
        bool hasGroundProbe = verticalVelocity <= 0f && TryGetGround(out probedGroundNormal);
        bool hasWalkableGround = verticalVelocity <= 0f &&
            (foundWalkableGroundContact || hasGroundProbe || stepUpGraceRemaining > 0f);
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

        if (previouslyGrounded && IsGrounded && !acceptedJumpThisTick)
        {
            float expectedVerticalTravel = locomotionDisplacement.y + externalDisplacement.y;
            ReportVerticalStep(actualDisplacement.y - expectedVerticalTravel);
        }

        float actualDownwardSpeed = Mathf.Max(0f, -actualDisplacement.y / deltaTime);
        UpdateFallAmount(actualDownwardSpeed, acceptedJumpThisTick, collisions, deltaTime);

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
        float actualPlanarSpeed = planarVelocity.sqrMagnitude > 0.0001f
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

    private bool TryStepUp(Vector3 displacement, out float rise)
    {
        rise = 0f;
        Vector3 planar = Vector3.ProjectOnPlane(displacement, Vector3.up);
        float planarDistance = planar.magnitude;
        if (planarDistance <= 0.0001f)
            return false;

        Vector3 direction = planar / planarDistance;
        Vector3 center = transform.TransformPoint(controller.center);
        float feetY = center.y - controller.height * 0.5f;
        float expectedGroundY = feetY - controller.skinWidth;
        float groundY = expectedGroundY;
        if (Physics.Raycast(center, Vector3.down, out RaycastHit groundHit,
                controller.height * 0.5f + StepGroundSearch, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundY = groundHit.point.y;
        }

        float supportY = Mathf.Max(groundY, expectedGroundY);
        if (!TryFindRiserFace(center, supportY, direction, planarDistance, out float faceDistance))
            return false;

        float maximumRise = MaxStepHeight;
        float walkableNormalY = Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad);
        Vector3 treadOrigin = new Vector3(center.x, supportY + maximumRise + StepLiftMargin, center.z) +
                              direction * (faceDistance + StepTreadSample);
        float treadSearch = maximumRise + StepLiftMargin + StepTreadDepthMargin;
        if (!Physics.Raycast(treadOrigin, Vector3.down, out RaycastHit tread, treadSearch, groundMask,
                QueryTriggerInteraction.Ignore) || tread.normal.y < walkableNormalY)
        {
            return RejectStep("no walkable tread within " + maximumRise.ToString("0.00") + " m beyond the riser");
        }

        float treadRise = tread.point.y - supportY;
        if (profile.DrawDebug)
            Debug.DrawLine(treadOrigin, tread.point, Color.green);

        if (treadRise < MinimumStepRise)
            return false;

        if (treadRise > maximumRise + StepHeightTolerance)
        {
            return RejectStep("rise " + treadRise.ToString("0.00") + " m is above the max step " +
                              maximumRise.ToString("0.00") + " m");
        }

        float clearanceRadius = Mathf.Max(0.01f, controller.radius - controller.skinWidth * 2f);
        float halfSegment = Mathf.Max(0f, controller.height * 0.5f - controller.radius);
        Vector3 bottom = center + Vector3.down * halfSegment;
        Vector3 top = center + Vector3.up * halfSegment;
        float lift = treadRise + StepSkinLift;
        if (Physics.CapsuleCast(bottom, top, clearanceRadius, Vector3.up, lift, groundMask,
                QueryTriggerInteraction.Ignore))
        {
            return RejectStep("no headroom to lift " + lift.ToString("0.00") + " m");
        }

        Vector3 liftOffset = Vector3.up * (lift + StepLiftMargin);
        float advance = Mathf.Max(0f, faceDistance - controller.radius) + StepAdvance;
        if (Physics.CapsuleCast(bottom + liftOffset, top + liftOffset, clearanceRadius, direction, advance,
                groundMask, QueryTriggerInteraction.Ignore))
        {
            return RejectStep("blocked above the step");
        }

        rise = lift;
        return true;
    }

    private void LimitVelocityToAchievedMotion(Vector3 achievedDisplacement, float deltaTime)
    {
        Vector3 achievedVelocity = Vector3.ProjectOnPlane(achievedDisplacement, Vector3.up) / deltaTime;
        if (achievedVelocity.sqrMagnitude < planarVelocity.sqrMagnitude)
            planarVelocity = achievedVelocity;
    }
    private bool RejectStep(string reason)
    {
        if (profile.DrawDebug && Time.time - lastStepRejectLogTime >= StepRejectLogInterval)
        {
            lastStepRejectLogTime = Time.time;
            Debug.Log("Step rejected: " + reason, this);
        }

        return false;
    }
    private bool TryFindRiserFace(Vector3 center, float groundY, Vector3 direction, float planarDistance,
        out float faceDistance)
    {
        faceDistance = float.MaxValue;
        Vector3 side = Vector3.Cross(Vector3.up, direction);
        float reach = controller.radius + planarDistance + StepProbeReach;
        for (int i = -1; i <= 1; i++)
        {
            Vector3 origin = new Vector3(center.x, groundY + StepRayHeight, center.z) +
                             side * (controller.radius * StepRaySpread * i);
            bool hitRiser = Physics.Raycast(origin, direction, out RaycastHit hit, reach, groundMask,
                QueryTriggerInteraction.Ignore) && hit.normal.y < StepVerticalNormalY;
            if (profile.DrawDebug)
                Debug.DrawRay(origin, direction * reach, hitRiser ? Color.magenta : Color.gray);

            if (hitRiser && hit.distance < faceDistance)
                faceDistance = hit.distance;
        }

        return faceDistance < float.MaxValue;
    }    private void ReportVerticalStep(float stepDelta)
    {
        float magnitude = Mathf.Abs(stepDelta);
        if (magnitude < MinimumReportedStep || magnitude > MaxStepHeight * MaximumReportedStepRatio)
            return;

        SteppedVertically?.Invoke(stepDelta);
    }

    private bool ShouldSnapToGround(bool previouslyGrounded, bool jumpedThisTick, CollisionFlags collisions)
    {
        return previouslyGrounded && !jumpedThisTick && verticalVelocity <= 0f &&
               jumpGroundingGrace <= 0f && stepUpGraceRemaining <= 0f &&
               (collisions & CollisionFlags.Below) == 0 && !foundWalkableGroundContact;
    }

    private void SnapToGround()
    {
        float radius = Mathf.Max(0.01f, controller.radius * 0.9f);
        Vector3 center = transform.TransformPoint(controller.center);
        Vector3 capsuleBottom = center + Vector3.down * (controller.height * 0.5f - controller.radius);
        const float originLift = 0.05f;
        float maximumDrop = MaxStepHeight;
        Vector3 origin = capsuleBottom + Vector3.up * originLift;
        if (!Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit,
                originLift + maximumDrop + 0.1f, groundMask, QueryTriggerInteraction.Ignore) ||
            !IsWalkableGroundHit(hit))
        {
            return;
        }

        float verticalGap = hit.distance - originLift -
            (controller.radius - radius) / Mathf.Max(0.01f, hit.normal.y);
        if (verticalGap <= groundProbeContactTolerance || verticalGap > maximumDrop)
            return;

        controller.Move(Vector3.down * verticalGap);
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
        if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit,
                distance, groundMask, QueryTriggerInteraction.Ignore) &&
            IsWalkableGroundHit(hit))
        {
            // The cast uses a slightly smaller sphere than the controller. Correct its vertical
            // clearance for the probe lift and slope, then allow the CharacterController's skin gap.
            float verticalGap = hit.distance - originLift -
                (controller.radius - radius) / Mathf.Max(0.01f, hit.normal.y);
            float contactAllowance = groundProbeContactTolerance +
                controller.skinWidth / Mathf.Max(0.01f, hit.normal.y);
            if (verticalGap <= contactAllowance)
            {
                normal = hit.normal;
                return true;
            }
        }

        return TryGetGroundFromSupportRays(capsuleBottom, originLift, out normal);
    }

    private bool IsWalkableGroundHit(RaycastHit hit)
    {
        return Vector3.Angle(hit.normal, Vector3.up) <= controller.slopeLimit;
    }

    private bool TryGetGroundFromSupportRays(Vector3 capsuleBottom, float originLift,
        out Vector3 normal)
    {
        normal = Vector3.up;
        float radius = controller.radius;
        float offsetDistance = radius * 0.6f;
        Vector3 rightDirection = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 forwardDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 rightOffset = rightDirection * offsetDistance;
        Vector3 forwardOffset = forwardDirection * offsetDistance;

        float minimumNormalY = Mathf.Max(0.01f,
            Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad));
        float maximumContactAllowance = groundProbeContactTolerance +
            controller.skinWidth / minimumNormalY;
        // Keep the ray search within the same skin/contact band used by the sphere probe.
        float rayOriginY = capsuleBottom.y - radius + originLift + maximumContactAllowance;
        float rayDistance = originLift + maximumContactAllowance * 2f;

        if (TryGetGroundFromSupportRay(capsuleBottom, Vector3.zero, radius, rayOriginY,
                rayDistance, out normal) ||
            TryGetGroundFromSupportRay(capsuleBottom, rightOffset, radius, rayOriginY,
                rayDistance, out normal) ||
            TryGetGroundFromSupportRay(capsuleBottom, -rightOffset, radius, rayOriginY,
                rayDistance, out normal) ||
            TryGetGroundFromSupportRay(capsuleBottom, forwardOffset, radius, rayOriginY,
                rayDistance, out normal) ||
            TryGetGroundFromSupportRay(capsuleBottom, -forwardOffset, radius, rayOriginY,
                rayDistance, out normal))
        {
            return true;
        }

        return false;
    }

    private bool TryGetGroundFromSupportRay(Vector3 capsuleBottom, Vector3 offset, float radius,
        float rayOriginY, float rayDistance, out Vector3 normal)
    {
        normal = Vector3.up;
        Vector3 origin = capsuleBottom + Vector3.ProjectOnPlane(offset, Vector3.up);
        origin.y = rayOriginY;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance,
                groundMask, QueryTriggerInteraction.Ignore) || !IsWalkableGroundHit(hit))
        {
            return false;
        }

        float footLevel = capsuleBottom.y - radius;
        float verticalGap = footLevel - hit.point.y;
        float contactAllowance = groundProbeContactTolerance +
            controller.skinWidth / Mathf.Max(0.01f, hit.normal.y);
        if (verticalGap > contactAllowance)
            return false;

        normal = hit.normal;
        return true;
    }

    private void UpdateFallAmount(float actualDownwardSpeed, bool acceptedJumpThisTick,
        CollisionFlags collisions, float deltaTime)
    {
        bool belowContactHoldingCharacter =
            (collisions & CollisionFlags.Below) != 0 &&
            actualDownwardSpeed <= MinimumFallAnimationSpeed;
        bool jumpGroundingIsProtected = verticalVelocity > 0f || jumpGroundingGrace > 0f;

        if (IsGrounded || belowContactHoldingCharacter || acceptedJumpThisTick ||
            jumpGroundingIsProtected || actualDownwardSpeed <= MinimumFallAnimationSpeed)
        {
            fallAnimationElapsed = 0f;
        }
        else
        {
            fallAnimationElapsed += deltaTime;
        }

        float fullFallTime = Mathf.Max(0.01f, fullFallAnimationTime);
        fallAmount = Mathf.Clamp01(fallAnimationElapsed / fullFallTime);
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
            ? Mathf.SmoothDamp(animationGait, gaitValue, ref animationBlendVelocity,
                blendTime, Mathf.Infinity, deltaTime)
            : gaitValue;
        animationGait = nextGait;

        if (animator == null || !animator.isActiveAndEnabled)
            return;

        animator.SetFloat(MoveY, animationGait);
        animator.SetBool(Grounded, grounded);
        animator.SetBool(Jumping, jumpAnimationActive);
        animator.SetBool(SprintJump, sprintJumpAnimation);
        animator.SetFloat(JumpTime, jumpAnimationTime);
        float jumpPoseWeight = jumpAnimationActive
            ? Mathf.Lerp(jumpApexPoseWeight, 1f, Mathf.Clamp01(
                Mathf.Abs(verticalVelocity) / Mathf.Max(0.01f, jumpAnimationLaunchSpeed)))
            : 1f;
        animator.SetFloat(JumpPoseWeight, jumpPoseWeight);
        animator.SetBool(LandingActive, landingAnimationActive);
        animator.SetFloat(FallAmount, fallAmount);
    }

    public void AddImpulse(Vector3 velocityChange)
    {
        ResetFallAnimation();
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
        animationGait = 0f;
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
        ResetFallAnimation();
        jumpAnimationActive = false;
        landingAnimationActive = false;
        sprintJumpAnimation = false;
        jumpRequested = false;
        presentationGroundedLatch = IsGrounded;
        if (!IsGrounded)
            airbornePresentationElapsed = fallPresentationGrace;

        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetFloat(MoveY, 0f);
            animator.SetBool(Grounded, IsGrounded);
            animator.SetBool(Jumping, false);
            animator.SetBool(SprintJump, false);
            animator.SetFloat(JumpTime, 0f);
            animator.SetFloat(JumpPoseWeight, 1f);
            animator.SetBool(LandingActive, false);
        }
    }

    private void ResetFallAnimation()
    {
        fallAnimationElapsed = 0f;
        fallAmount = 0f;

        if (animator != null && animator.isActiveAndEnabled)
            animator.SetFloat(FallAmount, 0f);
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        Stop();
        bool wasEnabled = controller.enabled;
        controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        controller.enabled = wasEnabled;
        if (stepSmoothing != null)
            stepSmoothing.ResetOffset();

        IsGrounded = false;
        hasInitializedGroundState = false;
        presentationGroundedLatch = false;
        airbornePresentationElapsed = fallPresentationGrace;
    }
}
