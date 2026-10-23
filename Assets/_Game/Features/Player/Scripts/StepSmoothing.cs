using UnityEngine;

[DefaultExecutionOrder(50)]
[DisallowMultipleComponent]
public class StepSmoothing : MonoBehaviour
{
    private const float SettledOffset = 0.0005f;

    private CharacterMotor motor;
    private Transform modelRoot;
    private Vector3 modelRestLocalPosition;
    private float offsetVelocity;

    public float VerticalOffset { get; private set; }

    public void Initialize(CharacterMotor characterMotor, Animator animator)
    {
        if (motor != null)
            motor.SteppedVertically -= OnSteppedVertically;

        motor = characterMotor;
        motor.SteppedVertically += OnSteppedVertically;

        modelRoot = animator != null && animator.transform != transform ? animator.transform : null;
        if (modelRoot != null)
            modelRestLocalPosition = modelRoot.localPosition;

        ResetOffset();
    }

    public void ResetOffset()
    {
        VerticalOffset = 0f;
        offsetVelocity = 0f;
        ApplyToModel();
    }

    private void OnDestroy()
    {
        if (motor != null)
            motor.SteppedVertically -= OnSteppedVertically;
    }

    private void OnSteppedVertically(float stepDelta)
    {
        float limit = motor.MaxStepHeight;
        VerticalOffset = Mathf.Clamp(VerticalOffset - stepDelta, -limit, limit);
        ApplyToModel();
    }

    private void Update()
    {
        if (motor == null)
            return;

        VerticalOffset = Mathf.SmoothDamp(VerticalOffset, 0f, ref offsetVelocity,
            motor.Profile.StepSmoothTime);
        if (Mathf.Abs(VerticalOffset) < SettledOffset)
        {
            VerticalOffset = 0f;
            offsetVelocity = 0f;
        }

        ApplyToModel();
    }

    private void ApplyToModel()
    {
        if (modelRoot != null)
            modelRoot.localPosition = modelRestLocalPosition + Vector3.up * VerticalOffset;
    }
}
