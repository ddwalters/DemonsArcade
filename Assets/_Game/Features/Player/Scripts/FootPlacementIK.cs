using UnityEngine;

[DisallowMultipleComponent]
public class FootPlacementIK : MonoBehaviour
{
    private const float MinimumIkWeight = 0.001f;
    private const float MinimumLookAheadSpeedSquared = 0.04f;
    private const float AnkleFallbackRatio = 0.1f;
    private const float MinimumSoleHeight = 0.001f;
    private const float DebugMarkerHeight = 0.1f;
    private const int LookAheadSampleCount = 3;

    private sealed class FootState
    {
        public AvatarIKGoal goal;
        public AvatarIKHint hint;
        public HumanBodyBones lowerLegBone;
        public float offset;
        public float offsetVelocity;
        public float shift;
        public float shiftVelocity;
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
    private FootGroundProbe probe;
    private float ikWeight;
    private float ikWeightVelocity;
    private float pelvisOffset;
    private float pelvisVelocity;

    public void Initialize(CharacterMotor characterMotor, Animator characterAnimator)
    {
        motor = characterMotor;
        animator = characterAnimator;
        probe = new FootGroundProbe(motor.GroundMask);
        ikWeight = 0f;
        ikWeightVelocity = 0f;
        ResetOffsets();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (motor == null || animator == null || probe == null || !animator.isHuman)
            return;

        CharacterLocomotionProfile profile = motor.Profile;
        float deltaTime = Time.deltaTime;
        float targetWeight = motor.IsGrounded && profile.FootIkEnabled ? 1f : 0f;
        if (deltaTime > 0f)
        {
            ikWeight = Mathf.SmoothDamp(ikWeight, targetWeight, ref ikWeightVelocity,
                profile.IkBlendTime, Mathf.Infinity, deltaTime);
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
        Vector3 travelDirection = travel.sqrMagnitude > MinimumLookAheadSpeedSquared
            ? travel.normalized
            : Vector3.zero;
        Vector3 forward = Vector3.ProjectOnPlane(motor.transform.forward, Vector3.up).normalized;

        SolveFoot(leftFoot, floorY, travelDirection, forward, profile, deltaTime);
        SolveFoot(rightFoot, floorY, travelDirection, forward, profile, deltaTime);
        ApplyPelvis(profile, deltaTime);
    }

    private void SolveFoot(FootState foot, float floorY, Vector3 travelDirection, Vector3 forward,
        CharacterLocomotionProfile profile, float deltaTime)
    {
        float legLength = motor.LegLength;
        float reach = motor.MaxStepHeight;
        Vector3 animatedPosition = animator.GetIKPosition(foot.goal);
        Quaternion animatedRotation = animator.GetIKRotation(foot.goal);
        float animatedSoleY = animatedPosition.y - GetSoleHeight(foot.goal);
        float restHeight = animatedSoleY - floorY;
        float swingHeight = profile.SwingHeight(legLength);
        bool swinging = restHeight > swingHeight;

        float targetOffset = 0f;
        float targetShift = 0f;
        float centerY = floorY;
        Vector3 groundNormal = Vector3.up;
        if (probe.TryGetGround(animatedPosition, floorY, restHeight, reach, out float hitY, out Vector3 normal))
        {
            centerY = hitY;
            targetOffset = hitY - floorY;
            groundNormal = normal;
        }

        if (swinging && travelDirection != Vector3.zero)
        {
            targetOffset = SolveSwingOffset(animatedPosition, animatedSoleY, travelDirection, floorY,
                restHeight, reach, centerY, targetOffset, profile);
        }
        else if (!swinging && forward != Vector3.zero)
        {
            SolvePlantedFootprint(animatedPosition, forward, floorY, restHeight, reach, centerY, profile,
                ref targetOffset, ref targetShift);
        }

        targetOffset = Mathf.Clamp(targetOffset, -reach, reach);
        if (profile.DrawDebug)
            Debug.DrawLine(animatedPosition, animatedPosition + Vector3.up * targetOffset, Color.green);

        if (deltaTime > 0f)
        {
            foot.offset = Mathf.SmoothDamp(foot.offset, targetOffset, ref foot.offsetVelocity,
                profile.FootBlendTime, Mathf.Infinity, deltaTime);
            foot.shift = Mathf.SmoothDamp(foot.shift, targetShift, ref foot.shiftVelocity,
                profile.FootBlendTime, Mathf.Infinity, deltaTime);
        }

        animator.SetIKPositionWeight(foot.goal, ikWeight);
        animator.SetIKPosition(foot.goal, animatedPosition + Vector3.up * foot.offset + forward * foot.shift);

        float plantedWeight = 1f - Mathf.Clamp01(restHeight / Mathf.Max(0.001f, swingHeight * 2f));
        Quaternion tilt = Quaternion.RotateTowards(Quaternion.identity,
            Quaternion.FromToRotation(Vector3.up, groundNormal), profile.MaxFootTiltDegrees);
        animator.SetIKRotationWeight(foot.goal, ikWeight * plantedWeight);
        animator.SetIKRotation(foot.goal, tilt * animatedRotation);

        ApplyKneeHint(foot, reach, profile);
    }

    private void SolvePlantedFootprint(Vector3 animatedPosition, Vector3 forward, float floorY,
        float restHeight, float reach, float centerY, CharacterLocomotionProfile profile,
        ref float targetOffset, ref float targetShift)
    {
        float legLength = motor.LegLength;
        float heelOffset = profile.HeelOffset(legLength);
        float toeOffset = profile.ToeOffset(legLength);
        Vector3 heelPoint = animatedPosition - forward * heelOffset;
        Vector3 toePoint = animatedPosition + forward * toeOffset;
        float heelY = probe.TryGetGround(heelPoint, floorY, restHeight, reach, out float sampledHeelY, out _)
            ? sampledHeelY
            : centerY;
        float toeY = probe.TryGetGround(toePoint, floorY, restHeight, reach, out float sampledToeY, out _)
            ? sampledToeY
            : centerY;

        targetOffset = Mathf.Max(centerY, Mathf.Max(heelY, toeY)) - floorY;
        if (profile.DrawDebug)
        {
            DrawMarker(heelPoint, heelY, Color.cyan);
            DrawMarker(toePoint, toeY, Color.cyan);
        }

        if (!profile.TreadSnapping || Mathf.Abs(toeY - heelY) <= profile.EdgeHeight(legLength))
            return;

        bool toeIsHigher = toeY > heelY;
        Vector3 edge = toeIsHigher
            ? probe.FindEdge(heelPoint, heelY, toePoint, toeY, floorY, restHeight, reach)
            : probe.FindEdge(toePoint, toeY, heelPoint, heelY, floorY, restHeight, reach);
        float edgeAlong = Vector3.Dot(edge - animatedPosition, forward);
        float margin = profile.EdgeMargin(legLength);
        float forwardShift = edgeAlong + heelOffset + margin;
        float backwardShift = edgeAlong - toeOffset - margin;
        bool stepForward = Mathf.Abs(forwardShift) <= Mathf.Abs(backwardShift);
        float chosenShift = stepForward ? forwardShift : backwardShift;
        if (Mathf.Abs(chosenShift) > profile.MaxFootShift(legLength))
            return;

        targetShift = chosenShift;
        targetOffset = (stepForward ? toeY : heelY) - floorY;
        if (profile.DrawDebug)
            DrawMarker(edge, Mathf.Max(heelY, toeY), Color.yellow);
    }

    private float SolveSwingOffset(Vector3 animatedPosition, float animatedSoleY, Vector3 travelDirection,
        float floorY, float restHeight, float reach, float centerY, float currentTargetOffset,
        CharacterLocomotionProfile profile)
    {
        float lookAhead = profile.FootLookAhead(motor.LegLength);
        float highestAheadY = float.NegativeInfinity;
        float farthestAheadY = float.NegativeInfinity;
        for (int i = 1; i <= LookAheadSampleCount; i++)
        {
            Vector3 aheadPoint = animatedPosition + travelDirection * (lookAhead * i / LookAheadSampleCount);
            if (!probe.TryGetGround(aheadPoint, floorY, restHeight, reach, out float aheadY, out _))
                continue;

            highestAheadY = Mathf.Max(highestAheadY, aheadY);
            farthestAheadY = aheadY;
            if (profile.DrawDebug)
                DrawMarker(aheadPoint, aheadY, Color.red);
        }

        if (farthestAheadY == float.NegativeInfinity)
            return currentTargetOffset;

        float targetOffset = farthestAheadY - floorY;
        if (highestAheadY > centerY)
        {
            float clearanceOffset = highestAheadY + profile.StepClearance(motor.LegLength) - animatedSoleY;
            targetOffset = Mathf.Max(targetOffset, clearanceOffset);
        }

        return targetOffset;
    }

    private void ApplyKneeHint(FootState foot, float reach, CharacterLocomotionProfile profile)
    {
        Transform lowerLeg = animator.GetBoneTransform(foot.lowerLegBone);
        float hintWeight = ikWeight * profile.KneeHintWeight *
                           Mathf.Clamp01(Mathf.Abs(foot.offset) / Mathf.Max(0.01f, reach * 0.5f));
        if (lowerLeg == null || hintWeight <= 0f)
        {
            animator.SetIKHintPositionWeight(foot.hint, 0f);
            return;
        }

        Vector3 forward = Vector3.ProjectOnPlane(motor.transform.forward, Vector3.up).normalized;
        Vector3 hintPosition = lowerLeg.position +
                               forward * (motor.LegLength * 0.25f) +
                               Vector3.up * (foot.offset * 0.5f);
        animator.SetIKHintPosition(foot.hint, hintPosition);
        animator.SetIKHintPositionWeight(foot.hint, hintWeight);
    }

    private void ApplyPelvis(CharacterLocomotionProfile profile, float deltaTime)
    {
        float lowestOffset = Mathf.Min(0f, Mathf.Min(leftFoot.offset, rightFoot.offset));
        float dropLimit = profile.PelvisDropLimit(motor.LegLength);
        float targetOffset = Mathf.Clamp(lowestOffset * profile.PelvisFollowRatio, -dropLimit, 0f);
        if (deltaTime > 0f)
        {
            pelvisOffset = Mathf.SmoothDamp(pelvisOffset, targetOffset, ref pelvisVelocity,
                profile.PelvisBlendTime, Mathf.Infinity, deltaTime);
        }

        animator.bodyPosition += Vector3.up * (pelvisOffset * ikWeight);
    }

    private float GetSoleHeight(AvatarIKGoal goal)
    {
        float height = goal == AvatarIKGoal.LeftFoot
            ? animator.leftFeetBottomHeight
            : animator.rightFeetBottomHeight;
        return height > MinimumSoleHeight ? height : motor.LegLength * AnkleFallbackRatio;
    }

    private static void DrawMarker(Vector3 point, float groundY, Color color)
    {
        Vector3 ground = new Vector3(point.x, groundY, point.z);
        Debug.DrawLine(ground, ground + Vector3.up * DebugMarkerHeight, color);
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
        leftFoot.shift = 0f;
        leftFoot.shiftVelocity = 0f;
        rightFoot.offset = 0f;
        rightFoot.offsetVelocity = 0f;
        rightFoot.shift = 0f;
        rightFoot.shiftVelocity = 0f;
        pelvisOffset = 0f;
        pelvisVelocity = 0f;
    }
}
