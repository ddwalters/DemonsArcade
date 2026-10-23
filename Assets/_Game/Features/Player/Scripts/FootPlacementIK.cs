using UnityEngine;

[DisallowMultipleComponent]
public class FootPlacementIK : MonoBehaviour
{
    private const float MinimumIkWeight = 0.001f;
    private const float IkBlendTime = 0.08f;
    private const float HipBlendTime = 0.1f;
    private const float HipDropLimit = 0.25f;
    private const float DescendResponseScale = 2.5f;
    private const float DescentBendShare = 1f;
    private const float SteppingFootHeight = 0.035f;
    private const float MinimumTravelSpeedSquared = 0.04f;
    private const float FallbackSoleHeight = 0.08f;
    private const float MinimumSoleHeight = 0.001f;
    private const float MaxFootTiltDegrees = 30f;
    private const float KneeForwardDistance = 0.25f;
    private const float ProbeMargin = 0.1f;
    private const int LookAheadSampleCount = 3;

    private sealed class FootState
    {
        public AvatarIKGoal goal;
        public AvatarIKHint hint;
        public HumanBodyBones lowerLegBone;
        public float offset;
        public float offsetVelocity;
    }

    private readonly FootState leftFoot = new FootState
    {
        goal = AvatarIKGoal.LeftFoot,
        hint = AvatarIKHint.LeftKnee,
        lowerLegBone = HumanBodyBones.LeftLowerLeg
    };

    private readonly FootState rightFoot = new FootState
    {
        goal = AvatarIKGoal.RightFoot,
        hint = AvatarIKHint.RightKnee,
        lowerLegBone = HumanBodyBones.RightLowerLeg
    };

    private CharacterMotor motor;
    private Animator animator;
    private float ikWeight;
    private float ikWeightVelocity;
    private float hipOffset;
    private float hipVelocity;

    public void Initialize(CharacterMotor characterMotor, Animator characterAnimator)
    {
        motor = characterMotor;
        animator = characterAnimator;
        ikWeight = 0f;
        ikWeightVelocity = 0f;
        ResetOffsets();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (motor == null || animator == null || !animator.isHuman)
            return;

        CharacterLocomotionProfile profile = motor.Profile;
        float deltaTime = Time.deltaTime;
        float targetWeight = motor.HasGroundSupport && profile.FootPlacement ? 1f : 0f;
        if (deltaTime > 0f)
        {
            ikWeight = Mathf.SmoothDamp(ikWeight, targetWeight, ref ikWeightVelocity, IkBlendTime,
                Mathf.Infinity, deltaTime);
        }

        if (ikWeight < MinimumIkWeight)
        {
            ResetOffsets();
            ClearGoals(leftFoot);
            ClearGoals(rightFoot);
            return;
        }

        float floorY = animator.transform.position.y;
        Vector3 travel = motor.PlanarVelocity;
        travel.y = 0f;
        Vector3 travelDirection = travel.sqrMagnitude > MinimumTravelSpeedSquared
            ? travel.normalized
            : Vector3.zero;

        SolveFoot(leftFoot, floorY, travelDirection, profile, deltaTime);
        SolveFoot(rightFoot, floorY, travelDirection, profile, deltaTime);
        ApplyHips(profile, deltaTime);
    }

    private void SolveFoot(FootState foot, float floorY, Vector3 travelDirection,
        CharacterLocomotionProfile profile, float deltaTime)
    {
        float reach = motor.MaxStepHeight;
        Vector3 animatedPosition = animator.GetIKPosition(foot.goal);
        Quaternion animatedRotation = animator.GetIKRotation(foot.goal);
        float animatedSoleY = animatedPosition.y - GetSoleHeight(foot.goal);
        float footHeight = animatedSoleY - floorY;
        bool stepping = footHeight > SteppingFootHeight;

        float targetOffset = 0f;
        float groundHereY = floorY;
        Vector3 groundNormal = Vector3.up;
        if (TryGetGround(animatedPosition, floorY, footHeight, reach, out float hitY, out Vector3 normal))
        {
            groundHereY = hitY;
            targetOffset = hitY - floorY;
            groundNormal = normal;
        }

        if (stepping && travelDirection != Vector3.zero)
        {
            targetOffset = SolveSteppingOffset(animatedPosition, animatedSoleY, travelDirection, floorY,
                footHeight, reach, groundHereY, targetOffset, profile);
        }

        targetOffset = Mathf.Clamp(targetOffset, -reach, reach);
        if (deltaTime > 0f)
        {
            float responseTime = targetOffset < foot.offset
                ? profile.FootResponseTime * DescendResponseScale
                : profile.FootResponseTime;
            foot.offset = Mathf.SmoothDamp(foot.offset, targetOffset, ref foot.offsetVelocity,
                responseTime, Mathf.Infinity, deltaTime);
        }

        animator.SetIKPositionWeight(foot.goal, ikWeight);
        animator.SetIKPosition(foot.goal, animatedPosition + Vector3.up * foot.offset);

        float plantedWeight = 1f - Mathf.Clamp01(footHeight / (SteppingFootHeight * 2f));
        Quaternion tilt = Quaternion.RotateTowards(Quaternion.identity,
            Quaternion.FromToRotation(Vector3.up, groundNormal), MaxFootTiltDegrees);
        animator.SetIKRotationWeight(foot.goal, ikWeight * plantedWeight);
        animator.SetIKRotation(foot.goal, tilt * animatedRotation);

        ApplyKneeHint(foot, reach, profile);
    }

    private float SolveSteppingOffset(Vector3 animatedPosition, float animatedSoleY, Vector3 travelDirection,
        float floorY, float footHeight, float reach, float groundHereY, float currentTargetOffset,
        CharacterLocomotionProfile profile)
    {
        float highestAheadY = float.NegativeInfinity;
        float farthestAheadY = float.NegativeInfinity;
        for (int i = 1; i <= LookAheadSampleCount; i++)
        {
            Vector3 aheadPoint = animatedPosition +
                                 travelDirection * (profile.LookAheadDistance * i / LookAheadSampleCount);
            if (!TryGetGround(aheadPoint, floorY, footHeight, reach, out float aheadY, out _))
                continue;

            highestAheadY = Mathf.Max(highestAheadY, aheadY);
            farthestAheadY = aheadY;
        }

        if (farthestAheadY == float.NegativeInfinity)
            return currentTargetOffset;

        float targetOffset = farthestAheadY - floorY;
        if (highestAheadY > groundHereY)
            targetOffset = Mathf.Max(targetOffset, highestAheadY + profile.FootClearance - animatedSoleY);

        return targetOffset;
    }

    private bool TryGetGround(Vector3 point, float floorY, float footHeight, float reach,
        out float groundY, out Vector3 normal)
    {
        float footAllowance = Mathf.Max(0f, footHeight);
        Vector3 origin = new Vector3(point.x, floorY + reach + footAllowance + ProbeMargin, point.z);
        float distance = reach * 2f + footAllowance + ProbeMargin * 2f;
        if (motor.TryFindWalkableGround(origin, distance, out RaycastHit hit))
        {
            groundY = hit.point.y;
            normal = hit.normal;
            return true;
        }

        groundY = floorY;
        normal = Vector3.up;
        return false;
    }

    private void ApplyKneeHint(FootState foot, float reach, CharacterLocomotionProfile profile)
    {
        Transform lowerLeg = animator.GetBoneTransform(foot.lowerLegBone);
        float hintWeight = ikWeight * profile.KneeForward *
                           Mathf.Clamp01(Mathf.Abs(foot.offset) / Mathf.Max(0.01f, reach * 0.5f));
        if (lowerLeg == null || hintWeight <= 0f)
        {
            animator.SetIKHintPositionWeight(foot.hint, 0f);
            return;
        }

        Vector3 forward = Vector3.ProjectOnPlane(motor.transform.forward, Vector3.up).normalized;
        Vector3 hintPosition = lowerLeg.position + forward * KneeForwardDistance +
                               Vector3.up * (foot.offset * 0.5f);
        animator.SetIKHintPosition(foot.hint, hintPosition);
        animator.SetIKHintPositionWeight(foot.hint, hintWeight);
    }

    private void ApplyHips(CharacterLocomotionProfile profile, float deltaTime)
    {
        float lowestOffset = Mathf.Min(0f, Mathf.Min(leftFoot.offset, rightFoot.offset));
        float footDifference = Mathf.Abs(leftFoot.offset - rightFoot.offset);
        float bendDrop = lowestOffset < 0f ? footDifference * DescentBendShare : 0f;
        float targetOffset = Mathf.Clamp((lowestOffset - bendDrop) * profile.HipFollow, -HipDropLimit, 0f);
        if (deltaTime > 0f)
        {
            hipOffset = Mathf.SmoothDamp(hipOffset, targetOffset, ref hipVelocity, HipBlendTime,
                Mathf.Infinity, deltaTime);
        }

        animator.bodyPosition += Vector3.up * (hipOffset * ikWeight);
    }

    private float GetSoleHeight(AvatarIKGoal goal)
    {
        float height = goal == AvatarIKGoal.LeftFoot
            ? animator.leftFeetBottomHeight
            : animator.rightFeetBottomHeight;
        return height > MinimumSoleHeight ? height : FallbackSoleHeight;
    }

    private void ClearGoals(FootState foot)
    {
        animator.SetIKPositionWeight(foot.goal, 0f);
        animator.SetIKRotationWeight(foot.goal, 0f);
        animator.SetIKHintPositionWeight(foot.hint, 0f);
    }

    private void ResetOffsets()
    {
        leftFoot.offset = 0f;
        leftFoot.offsetVelocity = 0f;
        rightFoot.offset = 0f;
        rightFoot.offsetVelocity = 0f;
        hipOffset = 0f;
        hipVelocity = 0f;
    }
}
