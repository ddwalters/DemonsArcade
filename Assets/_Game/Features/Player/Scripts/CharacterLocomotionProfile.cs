using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "CharacterLocomotionProfile", menuName = "Character/Locomotion Profile")]
public class CharacterLocomotionProfile : ScriptableObject
{
    [Header("Limits (ratios of the measured leg length)")]
    [Tooltip("Tallest step as a ratio of leg length. Used when Max Step Height Override is 0, so it scales with the character.")]
    [SerializeField, Range(0.05f, 1f)] private float maxStepHeightRatio = 0.55f;
    [Tooltip("Tallest step in meters. When above 0 it replaces the ratio. Set to 0 to scale with the leg length instead.")]
    [SerializeField, Min(0f)] private float maxStepHeightOverride = 0.45f;
    [Tooltip("Furthest the pelvis may drop to let a foot reach lower ground.")]
    [SerializeField, Range(0f, 1f)] private float pelvisDropLimitRatio = 0.3f;
    [Tooltip("How far ahead of a swinging foot the ground is checked for the next tread.")]
    [SerializeField, Range(0f, 1.5f)] private float footLookAheadRatio = 0.7f;
    [Tooltip("Foot height above the floor, in the animation, that counts as swinging rather than planted.")]
    [SerializeField, Range(0f, 0.2f)] private float swingHeightRatio = 0.04f;
    [Tooltip("Minimum gap kept between the sole and the highest tread ahead while a foot is swinging.")]
    [SerializeField, Range(0f, 0.4f)] private float stepClearanceRatio = 0.2f;

    [Header("Stepping")]
    [Tooltip("Step up in the motor (detect the riser, read the tread height, lift onto it) instead of using the CharacterController step offset, which is then set to a small value for tiny bumps only.")]
    [FormerlySerializedAs("controlsStepOffset")]
    [SerializeField] private bool useMotorStepUp = true;
    [Tooltip("Seconds the camera and model take to ease out a step. Lower is snappier.")]
    [SerializeField, Min(0.01f)] private float stepSmoothTime = 0.12f;

    [Header("Foot IK")]
    [Tooltip("Turn foot IK off entirely to compare against the plain animation.")]
    [SerializeField] private bool footIkEnabled = true;
    [Tooltip("Draw the IK foot targets, heel and toe samples and detected edges in the Scene view during play.")]
    [SerializeField] private bool drawDebug;
    [SerializeField, Min(0.01f)] private float ikBlendTime = 0.08f;
    [Tooltip("Seconds a foot takes to move to its new height or position. Lower reaches the tread sooner.")]
    [SerializeField, Min(0.01f)] private float footBlendTime = 0.04f;
    [SerializeField, Min(0.01f)] private float pelvisBlendTime = 0.1f;
    [Tooltip("How much of the lower foot's drop the pelvis follows. 0 keeps the pelvis, 1 follows fully.")]
    [SerializeField, Range(0f, 1f)] private float pelvisFollowRatio = 0.5f;
    [SerializeField, Range(0f, 80f)] private float maxFootTiltDegrees = 30f;
    [SerializeField, Range(0f, 1f)] private float kneeHintWeight = 0.8f;

    [Header("Foot placement")]
    [Tooltip("Slide a planted foot off any tread edge it straddles so the whole foot rests on one tread.")]
    [SerializeField] private bool treadSnapping;
    [Tooltip("Distance from the ankle back to the heel.")]
    [SerializeField, Range(0f, 0.3f)] private float heelOffsetRatio = 0.08f;
    [Tooltip("Distance from the ankle forward to the toe.")]
    [SerializeField, Range(0f, 0.6f)] private float toeOffsetRatio = 0.25f;
    [Tooltip("Furthest a foot may slide to get off an edge.")]
    [SerializeField, Range(0f, 0.6f)] private float maxFootShiftRatio = 0.3f;
    [Tooltip("Gap left between the foot and the tread edge after sliding.")]
    [SerializeField, Range(0f, 0.1f)] private float edgeMarginRatio = 0.03f;
    [Tooltip("Height difference between heel and toe that counts as straddling an edge.")]
    [SerializeField, Range(0.005f, 0.2f)] private float edgeHeightRatio = 0.03f;

    public bool UsesMotorStepUp => useMotorStepUp;
    public float StepSmoothTime => stepSmoothTime;
    public bool FootIkEnabled => footIkEnabled;
    public bool DrawDebug => drawDebug;
    public float IkBlendTime => ikBlendTime;
    public float FootBlendTime => footBlendTime;
    public float PelvisBlendTime => pelvisBlendTime;
    public float PelvisFollowRatio => pelvisFollowRatio;
    public float MaxFootTiltDegrees => maxFootTiltDegrees;
    public float KneeHintWeight => kneeHintWeight;
    public bool TreadSnapping => treadSnapping;

    public float MaxStepHeight(float legLength) =>
        maxStepHeightOverride > 0f ? maxStepHeightOverride : legLength * maxStepHeightRatio;

    public float PelvisDropLimit(float legLength) => legLength * pelvisDropLimitRatio;

    public float FootLookAhead(float legLength) => legLength * footLookAheadRatio;

    public float SwingHeight(float legLength) => legLength * swingHeightRatio;

    public float StepClearance(float legLength) => legLength * stepClearanceRatio;

    public float HeelOffset(float legLength) => legLength * heelOffsetRatio;

    public float ToeOffset(float legLength) => legLength * toeOffsetRatio;

    public float MaxFootShift(float legLength) => legLength * maxFootShiftRatio;

    public float EdgeMargin(float legLength) => legLength * edgeMarginRatio;

    public float EdgeHeight(float legLength) => legLength * edgeHeightRatio;

    public static CharacterLocomotionProfile CreateDefault()
    {
        CharacterLocomotionProfile profile = CreateInstance<CharacterLocomotionProfile>();
        profile.hideFlags = HideFlags.HideAndDontSave;
        return profile;
    }
}
