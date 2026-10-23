using UnityEngine;

[CreateAssetMenu(fileName = "LocomotionProfile", menuName = "Character/Locomotion Profile")]
public class CharacterLocomotionProfile : ScriptableObject
{
    [Header("Stepping")]
    [Tooltip("Tallest step the character walks up. Anything taller needs a jump.")]
    [SerializeField, Min(0.05f)] private float maxStepHeight = 0.45f;
    [Tooltip("How far ahead and behind the ground is averaged so stairs read as a smooth ramp for the body and camera. Larger is smoother but starts easing earlier.")]
    [SerializeField, Min(0.05f)] private float stepSmoothingDistance = 0.35f;
    [Tooltip("Extra seconds of smoothing for the camera only. Raise this if the camera still bobs on stairs.")]
    [SerializeField, Min(0.01f)] private float cameraEaseTime = 0.1f;

    [Header("Feet")]
    [Tooltip("Place each foot on the ground under it. Turn off to use the plain animation.")]
    [SerializeField] private bool footPlacement = true;
    [Tooltip("How far ahead a stepping foot looks for the next step.")]
    [SerializeField, Min(0f)] private float lookAheadDistance = 0.5f;
    [Tooltip("Gap kept over the highest step ahead while a foot is stepping. Raises the knee.")]
    [SerializeField, Min(0f)] private float footClearance = 0.18f;
    [Tooltip("Seconds a foot takes to reach its height. Lower is snappier.")]
    [SerializeField, Min(0.01f)] private float footResponseTime = 0.04f;
    [Tooltip("How much of the lower foot's drop the hips follow. 0 keeps the hips level, 1 follows fully.")]
    [SerializeField, Range(0f, 1f)] private float hipFollow = 0.5f;
    [Tooltip("How strongly the knees are pushed forward while stepping.")]
    [SerializeField, Range(0f, 1f)] private float kneeForward = 0.8f;

    public float MaxStepHeight => maxStepHeight;
    public float StepSmoothingDistance => stepSmoothingDistance;
    public float CameraEaseTime => cameraEaseTime;
    public bool FootPlacement => footPlacement;
    public float LookAheadDistance => lookAheadDistance;
    public float FootClearance => footClearance;
    public float FootResponseTime => footResponseTime;
    public float HipFollow => hipFollow;
    public float KneeForward => kneeForward;

    public static CharacterLocomotionProfile CreateDefault()
    {
        CharacterLocomotionProfile profile = CreateInstance<CharacterLocomotionProfile>();
        profile.hideFlags = HideFlags.HideAndDontSave;
        return profile;
    }
}
