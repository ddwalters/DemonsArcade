using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class CharacterMotor : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float walkSpeed = 2.2f;
    [SerializeField, Min(0f)] private float sprintSpeed = 5f;
    [SerializeField, Min(0f)] private float jumpHeight = 1.1f;
    [Tooltip("Layers the character can stand on.")]
    [FormerlySerializedAs("groundMask")]
    [SerializeField] private LayerMask groundLayers = ~((1 << 9) | (1 << 5));

    [Header("Stepping and feet")]
    [Tooltip("Leave empty to use Resources/DefaultHumanoidLocomotionProfile. Assign one to override it for this character.")]
    [SerializeField] private CharacterLocomotionProfile locomotionProfile;

    private struct JumpAnimationPhases
    {
        public float takeoff;
        public float apex;
        public float contact;
        public float recovered;

        public JumpAnimationPhases(float takeoff, float apex, float contact, float recovered)
        {
            this.takeoff = takeoff;
            this.apex = apex;
            this.contact = contact;
            this.recovered = recovered;
        }
    }

    private static readonly JumpAnimationPhases NormalJumpPhases =
        new JumpAnimationPhases(16f / 57f, 24f / 57f, 34f / 57f, 48f / 57f);
    private static readonly JumpAnimationPhases SprintJumpPhases =
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
    private const float MinimumSurfaceNormalY = 0.5f;
    private const float SmallSnapDrop = 0.1f;
    private const float MinimumChainDrop = 0.03f;
    private const float LedgeEscapeSpeed = 2f;
    private const float LedgeSupportSearch = 0.6f;
    private const float LedgeInwardIntent = -0.2f;
    private const float LedgeSupportInset = 0.12f;
    private const float LedgeProbeMargin = 0.1f;
    private const int LedgeProbeCount = 8;
    private const float Acceleration = 30f;
    private const float Braking = 40f;
    private const float TurnSpeed = 720f;
    private const float AnimationBlendTime = 0.08f;
    private const float Gravity = -24f;
    private const float JumpBufferTime = 0.12f;
    private const float CoyoteTime = 0.1f;
    private const float AirAcceleration = 4f;
    private const float KnockbackBraking = 8f;
    private const float GroundProbeDistance = 0.15f;
    private const float ProbeOriginLift = 0.05f;
    private const float GroundProbeContactTolerance = 0.02f;
    private const float IdleLandingRecoveryTime = 0.18f;
    private const float MovingLandingRecoveryTime = 0.1f;
    private const float JumpAnimationFallGrace = 0.12f;
    private const float FallPresentationGrace = 0.07f;
    private const float JumpApexPoseWeight = 0.65f;
    private const float FullFallAnimationTime = 1f;
    private const float JumpContactPhaseLead = 0.002f;
    private const string SharedProfileResource = "DefaultHumanoidLocomotionProfile";
    private const float StepRayHeight = 0.012f;
    private const float StepRaySpread = 0.6f;
    private const float StepProbeReach = 0.08f;
    private const float StepMinimumApproachCos = 0.5f;
    private const float StepVerticalNormalY = 0.3f;
    private const float StepMargin = 0.05f;
    private const float StepLiftMargin = 0.02f;
    private const float StepSkinLift = 0.01f;
    private const float StepGraceTime = 0.4f;
    private const float StepGroundSearch = 0.2f;
    private const float MinimumStepRise = 0.015f;
    private const float StepHeightTolerance = 0.005f;
    private const float ControllerStepOffset = 0.05f;

    private CharacterController controller;
    private Animator animator;
    private CharacterLocomotionProfile profile;
    private StepSmoothing stepSmoothing;
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
    private Vector3 ledgeEscapeDirection;
    private bool jumpAnimationActive;
    private bool landingAnimationActive;
    private bool sprintJumpAnimation;
    private bool jumpRequested;
    private bool hasInitializedGroundState;
    private bool presentationGroundedLatch;
    private bool foundWalkableGroundContact;
    private bool restedOnContact;
    private bool fallingOntoStep;
    private float stairChainRemaining;
    private float leftGroundHeight;

    private JumpAnimationPhases JumpPhases => sprintJumpAnimation ? SprintJumpPhases : NormalJumpPhases;

    public bool IsGrounded { get; private set; }
    public bool HasGroundSupport => IsGrounded || coyoteTimeRemaining > 0f || fallingOntoStep;
    public CharacterLocomotionProfile Profile => profile;
    public float MaxStepHeight => profile.MaxStepHeight;
    public Vector3 PlanarVelocity => planarVelocity;

    public bool TryFindWalkableGround(Vector3 origin, float distance, out RaycastHit hit)
    {
        return Physics.Raycast(origin, Vector3.down, out hit, distance, groundLayers,
            QueryTriggerInteraction.Ignore) && hit.normal.y >= MinimumSurfaceNormalY;
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.applyRootMotion = false;

        profile = locomotionProfile != null ? locomotionProfile : LoadSharedProfile();
        controller.stepOffset = ControllerStepOffset;
        AddLocomotionComponents();
    }

    private static CharacterLocomotionProfile LoadSharedProfile()
    {
        CharacterLocomotionProfile shared = Resources.Load<CharacterLocomotionProfile>(SharedProfileResource);
        return shared != null ? shared : CharacterLocomotionProfile.CreateDefault();
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
                transform.rotation, targetFacing, TurnSpeed * deltaTime);
        }

        bool previouslyGrounded = IsGrounded;
        stepUpGraceRemaining = Mathf.Max(0f, stepUpGraceRemaining - deltaTime);
        stairChainRemaining = Mathf.Max(0f, stairChainRemaining - deltaTime);
        bool groundedAtStart = TryGetGround(out Vector3 groundNormal) || stepUpGraceRemaining > 0f;
        IsGrounded = groundedAtStart;
        if (groundedAtStart)
        {
            coyoteTimeRemaining = CoyoteTime;
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;
        }
        else
        {
            coyoteTimeRemaining = Mathf.Max(0f, coyoteTimeRemaining - deltaTime);
        }

        bool jumpIsBuffered = jumpRequested && jumpRequestAge <= JumpBufferTime;
        bool acceptedJumpThisTick = false;
        if (jumpIsBuffered && (groundedAtStart || coyoteTimeRemaining > 0f))
        {
            BeginJump(sprinting && hasMovementInput);
            acceptedJumpThisTick = true;
            IsGrounded = false;
            groundedAtStart = false;
        }
        else if (jumpRequested)
        {
            jumpRequestAge += deltaTime;
            if (jumpRequestAge > JumpBufferTime)
                jumpRequested = false;
        }

        jumpGroundingGrace = Mathf.Max(0f, jumpGroundingGrace - deltaTime);
        if (groundedAtStart)
        {
            float rate = hasMovementInput ? Acceleration : Braking;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, rate * deltaTime);
        }
        else if (hasMovementInput)
        {
            // Keep launch momentum and allow only modest steering while airborne.
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, AirAcceleration * deltaTime);
        }

        if (IsGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += Gravity * deltaTime;

        Vector3 locomotionDisplacement = planarVelocity * deltaTime;
        float horizontalShare = groundedAtStart ? AlignToSurface(ref locomotionDisplacement, groundNormal) : 1f;
        Vector3 externalDisplacement = externalVelocity * deltaTime;
        Vector3 ledgePush = UpdateLedgeEscape(groundedAtStart, acceptedJumpThisTick, hasMovementInput,
            requestedDirection, deltaTime);
        Vector3 ledgeDisplacement = ledgePush * deltaTime;
        Vector3 displacement = locomotionDisplacement + externalDisplacement + ledgeDisplacement +
            Vector3.up * (verticalVelocity * deltaTime);

        foundWalkableGroundContact = false;
        Vector3 startingPosition = transform.position;
        if (groundedAtStart && !acceptedJumpThisTick && verticalVelocity <= 0f)
        {
            // Forced edge escape is a correction, not an approach direction for a stair step.
            Vector3 stepTravel = Vector3.ProjectOnPlane(locomotionDisplacement + externalDisplacement, Vector3.up);
            Vector3 stepDirection = hasMovementInput ? requestedDirection : stepTravel.normalized;
            float stepDistance = hasMovementInput
                ? Mathf.Max(stepTravel.magnitude, movementSpeed * deltaTime)
                : stepTravel.magnitude;
            if (TryStepUp(stepDirection, stepDistance, out float stepRise))
            {
                controller.Move(Vector3.up * stepRise);
                stepUpGraceRemaining = StepGraceTime;
            }
        }

        CollisionFlags collisions = controller.Move(displacement);
        restedOnContact = (collisions & CollisionFlags.Below) != 0 || foundWalkableGroundContact;
        if (ShouldSnapToGround(acceptedJumpThisTick, collisions))
            SnapToGround();

        if (stepUpGraceRemaining > 0f && (foundWalkableGroundContact || TryGetGround(out _)))
            stepUpGraceRemaining = 0f;

        Vector3 actualDisplacement = transform.position - startingPosition;
        if (groundedAtStart && !acceptedJumpThisTick && stepUpGraceRemaining <= 0f &&
            (collisions & CollisionFlags.Sides) != 0)
            LimitVelocityToAchievedMotion(actualDisplacement - externalDisplacement - ledgeDisplacement, deltaTime);

        if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;

        bool hasWalkableGround = verticalVelocity <= 0f &&
            (foundWalkableGroundContact || TryGetGround(out _) || stepUpGraceRemaining > 0f);
        if (hasWalkableGround)
        {
            IsGrounded = true;
            coyoteTimeRemaining = CoyoteTime;
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;
        }
        else
        {
            IsGrounded = false;
            CarryLedgeEscapeMomentum();
        }

        float actualDownwardSpeed = Mathf.Max(0f, -actualDisplacement.y / deltaTime);
        UpdateFallAmount(actualDownwardSpeed, acceptedJumpThisTick, collisions, deltaTime);

        bool beganLandingThisTick = UpdateGroundPresentation(previouslyGrounded, acceptedJumpThisTick,
            hasMovementInput, startingPosition.y, deltaTime);
        UpdateJumpAndLandingAnimation(beganLandingThisTick, deltaTime);

        hasInitializedGroundState = true;

        externalVelocity = Vector3.MoveTowards(
            externalVelocity, Vector3.zero, KnockbackBraking * deltaTime);

        float surfaceSpeed = GetResolvedPlanarSpeed(actualDisplacement - externalDisplacement, ledgePush, deltaTime) /
                             Mathf.Max(MinimumSurfaceNormalY, horizontalShare);
        UpdateAnimator(GetGaitValue(surfaceSpeed), presentationGroundedLatch, deltaTime);
    }

    private void BeginJump(bool sprintJump)
    {
        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Gravity);
        jumpRequested = false;
        coyoteTimeRemaining = 0f;
        jumpGroundingGrace = 0.1f;
        jumpAnimationActive = true;
        sprintJumpAnimation = sprintJump;
        landingAnimationActive = false;
        landingRecoveryElapsed = 0f;
        jumpAnimationElapsed = 0f;
        jumpAnimationLaunchSpeed = verticalVelocity;
        jumpAnimationFlightTime = jumpAnimationLaunchSpeed * 2f / Mathf.Max(0.01f, -Gravity);
        jumpAnimationTime = JumpPhases.takeoff;
    }

    private static float AlignToSurface(ref Vector3 displacement, Vector3 groundNormal)
    {
        Vector3 alongSurface = Vector3.ProjectOnPlane(displacement, groundNormal);
        if (alongSurface.sqrMagnitude <= 0.0000001f)
            return 1f;

        float requestedLength = displacement.magnitude;
        displacement = alongSurface * (requestedLength / alongSurface.magnitude);
        return Vector3.ProjectOnPlane(displacement, Vector3.up).magnitude / requestedLength;
    }

    private Vector3 UpdateLedgeEscape(bool groundedAtStart, bool acceptedJumpThisTick, bool hasMovementInput,
        Vector3 requestedDirection, float deltaTime)
    {
        bool canApply = (groundedAtStart || restedOnContact) && !acceptedJumpThisTick &&
                        jumpGroundingGrace <= 0f && stepUpGraceRemaining <= 0f;
        if (!canApply)
        {
            ResetLedgeEscapeDirection();
            return Vector3.zero;
        }

        if (!TryGetLedgePush(hasMovementInput ? requestedDirection : Vector3.zero, out Vector3 push))
            return Vector3.zero;

        if (!hasMovementInput && push.sqrMagnitude > 0.0001f)
        {
            Quaternion targetFacing = Quaternion.LookRotation(push, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetFacing, TurnSpeed * deltaTime);
        }

        return push;
    }

    private void CarryLedgeEscapeMomentum()
    {
        if (ledgeEscapeDirection == Vector3.zero)
            return;

        float outwardSpeed = Vector3.Dot(planarVelocity, ledgeEscapeDirection);
        if (outwardSpeed < LedgeEscapeSpeed)
            planarVelocity += ledgeEscapeDirection * (LedgeEscapeSpeed - outwardSpeed);
    }

    private bool UpdateGroundPresentation(bool previouslyGrounded, bool acceptedJumpThisTick,
        bool hasMovementInput, float startingHeight, float deltaTime)
    {
        bool landedThisTick = hasInitializedGroundState && !previouslyGrounded && IsGrounded;
        if (previouslyGrounded && !IsGrounded)
            leftGroundHeight = startingHeight;

        float stepDownLanding = leftGroundHeight - transform.position.y;
        if (landedThisTick && !jumpAnimationActive && airbornePresentationElapsed <= StepGraceTime &&
            stepDownLanding > MinimumChainDrop && stepDownLanding <= MaxStepHeight + MinimumChainDrop)
            stairChainRemaining = StepGraceTime;

        bool meaningfulLanding = jumpAnimationActive || airbornePresentationElapsed >= FallPresentationGrace;
        bool beganLanding = false;
        if (IsGrounded)
        {
            if ((landedThisTick && meaningfulLanding) || (jumpAnimationActive && !acceptedJumpThisTick))
            {
                BeginLandingRecovery(hasMovementInput || planarVelocity.magnitude > 0.5f);
                beganLanding = true;
            }

            airbornePresentationElapsed = 0f;
            presentationGroundedLatch = true;
            fallingOntoStep = false;
        }
        else
        {
            airbornePresentationElapsed += deltaTime;
            fallingOntoStep = !acceptedJumpThisTick && !jumpAnimationActive &&
                              airbornePresentationElapsed <= StepGraceTime &&
                              verticalVelocity <= 0f && TryMeasureStepDown(out _);
            if (acceptedJumpThisTick || (airbornePresentationElapsed >= FallPresentationGrace && !fallingOntoStep))
                presentationGroundedLatch = false;
            else if (previouslyGrounded)
                presentationGroundedLatch = true;
        }

        return beganLanding;
    }

    private void UpdateJumpAndLandingAnimation(bool beganLandingThisTick, float deltaTime)
    {
        if (jumpAnimationActive)
        {
            jumpAnimationElapsed += deltaTime;
            if (jumpAnimationElapsed > jumpAnimationFlightTime + JumpAnimationFallGrace)
            {
                // Fall back to a stable loop if the character remains airborne past the clip's landing window.
                jumpAnimationActive = false;
            }
            else
            {
                jumpAnimationTime = CalculateAirborneJumpPhase();
            }
        }

        if (!landingAnimationActive)
            return;

        if (!beganLandingThisTick)
            landingRecoveryElapsed += deltaTime;
        float recoveryProgress = landingRecoveryDuration > 0f
            ? Mathf.Clamp01(landingRecoveryElapsed / landingRecoveryDuration)
            : 1f;
        jumpAnimationTime = Mathf.Lerp(JumpPhases.contact, JumpPhases.recovered, recoveryProgress);

        if (recoveryProgress >= 1f)
        {
            landingAnimationActive = false;
            sprintJumpAnimation = false;
        }
    }

    private float GetResolvedPlanarSpeed(Vector3 resolvedDisplacement, Vector3 ledgePush, float deltaTime)
    {
        Vector3 resolvedPlanarTravel = Vector3.ProjectOnPlane(resolvedDisplacement, Vector3.up);
        float actualPlanarSpeed = planarVelocity.sqrMagnitude > 0.0001f
            ? Mathf.Clamp(Vector3.Dot(resolvedPlanarTravel, planarVelocity.normalized) / deltaTime,
                0f, planarVelocity.magnitude)
            : 0f;

        if (ledgePush.sqrMagnitude > 0.0001f)
        {
            float actualEscapeSpeed = Mathf.Max(0f,
                Vector3.Dot(resolvedPlanarTravel, ledgePush.normalized) / deltaTime);
            actualPlanarSpeed = Mathf.Max(actualPlanarSpeed, actualEscapeSpeed);
        }

        return actualPlanarSpeed;
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

    private bool TryStepUp(Vector3 direction, float planarDistance, out float rise)
    {
        rise = 0f;
        if (planarDistance <= 0.0001f || direction.sqrMagnitude <= 0.0001f)
            return false;

        Vector3 center = transform.TransformPoint(controller.center);
        float feetY = center.y - controller.height * 0.5f;
        float expectedGroundY = feetY - controller.skinWidth;
        float groundY = expectedGroundY;
        if (Physics.Raycast(center, Vector3.down, out RaycastHit groundHit,
                controller.height * 0.5f + StepGroundSearch, groundLayers, QueryTriggerInteraction.Ignore))
        {
            groundY = groundHit.point.y;
        }

        float supportY = Mathf.Max(groundY, expectedGroundY);
        if (!TryFindRiserFace(center, supportY, direction, planarDistance,
                out Vector3 treadSamplePoint, out float advance))
            return false;

        float maximumRise = MaxStepHeight;
        float walkableNormalY = Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad);
        Vector3 treadOrigin = new Vector3(treadSamplePoint.x, supportY + maximumRise + StepLiftMargin,
            treadSamplePoint.z);
        float treadSearch = maximumRise + StepLiftMargin + StepMargin;
        if (!Physics.Raycast(treadOrigin, Vector3.down, out RaycastHit tread, treadSearch, groundLayers,
                QueryTriggerInteraction.Ignore) || tread.normal.y < walkableNormalY)
        {
            return false;
        }

        float treadRise = tread.point.y - supportY;
        if (treadRise < MinimumStepRise)
            return false;

        if (treadRise > maximumRise + StepHeightTolerance)
            return false;

        float clearanceRadius = Mathf.Max(0.01f, controller.radius - controller.skinWidth * 2f);
        float halfSegment = Mathf.Max(0f, controller.height * 0.5f - controller.radius);
        Vector3 bottom = center + Vector3.down * halfSegment;
        Vector3 top = center + Vector3.up * halfSegment;
        float lift = treadRise + StepSkinLift;
        if (Physics.CapsuleCast(bottom, top, clearanceRadius, Vector3.up, lift, groundLayers,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        Vector3 liftOffset = Vector3.up * (lift + StepLiftMargin);
        if (Physics.CapsuleCast(bottom + liftOffset, top + liftOffset, clearanceRadius, direction, advance,
                groundLayers, QueryTriggerInteraction.Ignore))
        {
            return false;
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

    private bool TryFindRiserFace(Vector3 center, float groundY, Vector3 direction, float planarDistance,
        out Vector3 treadSamplePoint, out float advanceDistance)
    {
        treadSamplePoint = Vector3.zero;
        advanceDistance = 0f;
        float nearestPlaneDistance = float.MaxValue;
        Vector3 side = Vector3.Cross(Vector3.up, direction);
        float contactReach = controller.radius + planarDistance + StepProbeReach;
        float rayReach = contactReach / StepMinimumApproachCos;
        for (int i = -1; i <= 1; i++)
        {
            Vector3 origin = new Vector3(center.x, groundY + StepRayHeight, center.z) +
                             side * (controller.radius * StepRaySpread * i);
            if (!Physics.Raycast(origin, direction, out RaycastHit hit, rayReach, groundLayers,
                    QueryTriggerInteraction.Ignore) || hit.normal.y >= StepVerticalNormalY)
            {
                continue;
            }

            Vector3 faceNormal = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
            float approachCos = Vector3.Dot(direction, -faceNormal);
            float planeDistance = Vector3.Dot(hit.point - center, -faceNormal);
            if (approachCos <= 0.01f || planeDistance > contactReach || planeDistance >= nearestPlaneDistance)
                continue;

            nearestPlaneDistance = planeDistance;
            treadSamplePoint = hit.point - faceNormal * StepMargin;
            advanceDistance = Mathf.Max(0f, planeDistance - controller.radius) /
                Mathf.Max(StepMinimumApproachCos, approachCos) + StepMargin;
        }

        return nearestPlaneDistance < float.MaxValue;
    }

    private bool ShouldSnapToGround(bool jumpedThisTick, CollisionFlags collisions)
    {
        return coyoteTimeRemaining > 0f && !jumpedThisTick && verticalVelocity <= 0f &&
               jumpGroundingGrace <= 0f && stepUpGraceRemaining <= 0f &&
               (collisions & CollisionFlags.Below) == 0 && !foundWalkableGroundContact;
    }

    private bool TryProbeGroundBelow(float searchDistance, out float verticalGap, out Vector3 normal)
    {
        verticalGap = 0f;
        normal = Vector3.up;
        float radius = Mathf.Max(0.01f, controller.radius * 0.9f);
        Vector3 center = transform.TransformPoint(controller.center);
        Vector3 capsuleBottom = center + Vector3.down * (controller.height * 0.5f - controller.radius);
        Vector3 origin = capsuleBottom + Vector3.up * ProbeOriginLift;
        if (!Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, searchDistance,
                groundLayers, QueryTriggerInteraction.Ignore) || !IsWalkableGroundHit(hit))
        {
            return false;
        }

        verticalGap = hit.distance - ProbeOriginLift -
            (controller.radius - radius) / Mathf.Max(0.01f, hit.normal.y);
        normal = hit.normal;
        return true;
    }

    private bool TryMeasureStepDown(out float verticalGap)
    {
        return TryProbeGroundBelow(ProbeOriginLift + MaxStepHeight + 0.1f, out verticalGap, out _) &&
               verticalGap > GroundProbeContactTolerance && verticalGap <= MaxStepHeight;
    }

    private void SnapToGround()
    {
        if (!TryMeasureStepDown(out float verticalGap))
            return;

        if (stairChainRemaining <= 0f && verticalGap > SmallSnapDrop)
            return;

        controller.Move(Vector3.down * verticalGap);
        stairChainRemaining = StepGraceTime;
    }

    private bool IsCenterSupported(Vector3 center)
    {
        float halfHeight = controller.height * 0.5f;
        if (!Physics.Raycast(center, Vector3.down, out RaycastHit hit, halfHeight + LedgeSupportSearch,
                groundLayers, QueryTriggerInteraction.Ignore) || !IsWalkableGroundHit(hit))
        {
            return false;
        }

        float normalY = Mathf.Max(0.01f, hit.normal.y);
        float allowance = controller.radius * (1f / normalY - 1f) + controller.skinWidth / normalY +
                          GroundProbeContactTolerance;
        float gap = (center.y - halfHeight) - hit.point.y;
        return gap <= allowance;
    }

    private static Vector3 GetLedgeProbeOffset(int index, float radius)
    {
        return Quaternion.Euler(0f, index * 360f / LedgeProbeCount, 0f) * Vector3.forward * radius;
    }

    private bool HasGroundBelow(Vector3 point, float depthBelowFeet)
    {
        float reach = controller.height * 0.5f + depthBelowFeet;
        return Physics.Raycast(point, Vector3.down, out RaycastHit hit, reach, groundLayers,
            QueryTriggerInteraction.Ignore) && IsWalkableGroundHit(hit);
    }

    private bool TryGetLedgePush(Vector3 intendedDirection, out Vector3 push)
    {
        push = Vector3.zero;
        Vector3 center = transform.TransformPoint(controller.center);
        Vector3 awayFromSupport;
        if (IsCenterSupported(center))
        {
            Vector3 voidDirection = Vector3.zero;
            float supportedDrop = MaxStepHeight + controller.skinWidth + GroundProbeContactTolerance;
            for (int i = 0; i < LedgeProbeCount; i++)
            {
                Vector3 offset = GetLedgeProbeOffset(i, LedgeSupportInset);
                if (!HasGroundBelow(center + offset, supportedDrop))
                    voidDirection += offset;
            }

            if (voidDirection.sqrMagnitude <= 0.0001f)
            {
                ResetLedgeEscapeDirection();
                return false;
            }

            awayFromSupport = voidDirection.normalized;
        }
        else
        {
            Vector3 supportDirection = Vector3.zero;
                        for (int i = 0; i < LedgeProbeCount; i++)
            {
                Vector3 offset = GetLedgeProbeOffset(i, controller.radius);
                if (HasGroundBelow(center + offset, LedgeProbeMargin))
                    supportDirection += offset;
            }

            if (supportDirection.sqrMagnitude <= 0.0001f)
            {
                ResetLedgeEscapeDirection();
                return false;
            }

            awayFromSupport = -supportDirection.normalized;
        }

        bool hasEscapeDirection = ledgeEscapeDirection != Vector3.zero;
        if (Vector3.Dot(intendedDirection, awayFromSupport) < LedgeInwardIntent ||
            (hasEscapeDirection && Vector3.Dot(intendedDirection, ledgeEscapeDirection) < LedgeInwardIntent))
        {
            ResetLedgeEscapeDirection();
            return false;
        }

        if (!hasEscapeDirection)
            ledgeEscapeDirection = awayFromSupport;

        // Keep the world-space escape direction until support returns or the player steers inward.
        float outwardSpeed = Mathf.Max(0f, Vector3.Dot(planarVelocity, ledgeEscapeDirection));
        push = ledgeEscapeDirection * Mathf.Max(0f, LedgeEscapeSpeed - outwardSpeed);
        return true;
    }

    private void ResetLedgeEscapeDirection() => ledgeEscapeDirection = Vector3.zero;

    private bool TryGetGround(out Vector3 normal)
    {
        normal = Vector3.up;
        if (verticalVelocity > 0f || jumpGroundingGrace > 0f)
            return false;

        if (TryProbeGroundBelow(GroundProbeDistance + 0.1f, out float verticalGap, out Vector3 hitNormal) &&
            verticalGap <= GroundProbeContactTolerance + controller.skinWidth / Mathf.Max(0.01f, hitNormal.y))
        {
            normal = hitNormal;
            return true;
        }

        return false;
    }

    private bool IsWalkableGroundHit(RaycastHit hit)
    {
        return Vector3.Angle(hit.normal, Vector3.up) <= controller.slopeLimit;
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

        fallAmount = Mathf.Clamp01(fallAnimationElapsed / FullFallAnimationTime);
    }

    private void BeginLandingRecovery(bool hasMovementInput)
    {
        jumpAnimationActive = false;
        landingAnimationActive = true;
        landingRecoveryElapsed = 0f;
        landingRecoveryDuration = hasMovementInput
            ? MovingLandingRecoveryTime
            : IdleLandingRecoveryTime;
        jumpAnimationTime = JumpPhases.contact;
    }

    private float CalculateAirborneJumpPhase()
    {
        JumpAnimationPhases phases = JumpPhases;
        float launchSpeed = Mathf.Max(0.01f, jumpAnimationLaunchSpeed);
        if (verticalVelocity > 0f)
        {
            float ascentProgress = 1f - Mathf.Clamp01(verticalVelocity / launchSpeed);
            return Mathf.Lerp(phases.takeoff, phases.apex, ascentProgress);
        }

        float descentProgress = Mathf.Clamp01(-verticalVelocity / launchSpeed);
        return Mathf.Lerp(phases.apex, phases.contact - JumpContactPhaseLead, descentProgress);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (Vector3.Angle(hit.normal, Vector3.up) <= controller.slopeLimit)
        {
            foundWalkableGroundContact = true;
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
        animationGait = deltaTime > 0f
            ? Mathf.SmoothDamp(animationGait, gaitValue, ref animationBlendVelocity,
                AnimationBlendTime, Mathf.Infinity, deltaTime)
            : gaitValue;

        if (animator == null || !animator.isActiveAndEnabled)
            return;

        animator.SetFloat(MoveY, animationGait);
        animator.SetBool(Grounded, grounded);
        animator.SetBool(Jumping, jumpAnimationActive);
        animator.SetBool(SprintJump, sprintJumpAnimation);
        animator.SetFloat(JumpTime, jumpAnimationTime);
        float jumpPoseWeight = jumpAnimationActive
            ? Mathf.Lerp(JumpApexPoseWeight, 1f, Mathf.Clamp01(
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
            airbornePresentationElapsed = FallPresentationGrace;
            presentationGroundedLatch = false;
        }
    }

    public void Stop()
    {
        planarVelocity = Vector3.zero;
        externalVelocity = Vector3.zero;
        restedOnContact = false;
        ResetLedgeEscapeDirection();
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
        airbornePresentationElapsed = IsGrounded ? 0f : FallPresentationGrace;
        ResetFallAnimation();
        jumpAnimationActive = false;
        landingAnimationActive = false;
        sprintJumpAnimation = false;
        jumpRequested = false;
        presentationGroundedLatch = IsGrounded;
        UpdateAnimator(0f, IsGrounded);
    }

    private void ResetFallAnimation()
    {
        fallAnimationElapsed = 0f;
        fallAmount = 0f;
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
        airbornePresentationElapsed = FallPresentationGrace;
    }
}
