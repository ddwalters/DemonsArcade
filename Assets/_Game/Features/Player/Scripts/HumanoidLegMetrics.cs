using UnityEngine;

public static class HumanoidLegMetrics
{
    private const float FallbackAnkleRatio = 0.1f;

    public static bool TryMeasure(Animator animator, out float legLength)
    {
        legLength = 0f;
        if (animator == null || !animator.isHuman)
            return false;

        Transform upperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Transform lowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        Transform foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        if (upperLeg == null || lowerLeg == null || foot == null)
            return false;

        float limbLength = Vector3.Distance(upperLeg.position, lowerLeg.position) +
                           Vector3.Distance(lowerLeg.position, foot.position);
        float ankleHeight = animator.leftFeetBottomHeight;
        if (ankleHeight <= 0.001f)
            ankleHeight = limbLength * FallbackAnkleRatio;

        legLength = limbLength + ankleHeight;
        return true;
    }
}
