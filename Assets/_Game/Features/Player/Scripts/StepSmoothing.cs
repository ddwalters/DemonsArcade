using UnityEngine;

[DefaultExecutionOrder(50)]
[DisallowMultipleComponent]
public class StepSmoothing : MonoBehaviour
{
    private const int SampleCount = 5;
    private const float BodyEaseTime = 0.05f;
    private const float MinimumSpeed = 0.3f;
    private const float FullEffectSpeed = 1f;
    private const float RayMargin = 0.1f;
    private const float HeightTolerance = 1.25f;

    private static readonly float[] SampleWeights = { 1f, 2f, 3f, 2f, 1f };

    private CharacterMotor motor;
    private CharacterController controller;
    private Transform modelRoot;
    private Vector3 modelRestLocalPosition;
    private bool hasHeights;
    private float bodyHeight;
    private float bodyVelocity;
    private float cameraHeight;
    private float cameraVelocity;

    public float CameraOffset { get; private set; }

    public void Initialize(CharacterMotor characterMotor, Animator animator)
    {
        motor = characterMotor;
        controller = motor.GetComponent<CharacterController>();

        modelRoot = animator != null && animator.transform != transform ? animator.transform : null;
        if (modelRoot != null)
            modelRestLocalPosition = modelRoot.localPosition;

        ResetOffset();
    }

    public void ResetOffset()
    {
        hasHeights = false;
        bodyVelocity = 0f;
        cameraVelocity = 0f;
        CameraOffset = 0f;
        ApplyToModel(0f);
    }

    private void Update()
    {
        if (motor == null)
            return;

        CharacterLocomotionProfile profile = motor.Profile;
        Vector3 center = motor.transform.TransformPoint(controller.center);
        float feetY = center.y - controller.height * 0.5f;
        Vector3 travel = motor.PlanarVelocity;
        travel.y = 0f;
        float speed = travel.magnitude;

        float targetHeight = feetY;
        if (motor.HasGroundSupport && speed > MinimumSpeed &&
            SampleGround(center, feetY, travel / speed, profile, out float windowAverage))
        {
            float effect = Mathf.Clamp01((speed - MinimumSpeed) / (FullEffectSpeed - MinimumSpeed));
            targetHeight = Mathf.Lerp(feetY, windowAverage + controller.skinWidth, effect);
        }

        float limit = motor.MaxStepHeight;
        targetHeight = Mathf.Clamp(targetHeight, feetY - limit, feetY + limit);
        if (!motor.HasGroundSupport || !hasHeights)
        {
            bodyHeight = feetY;
            cameraHeight = feetY;
            bodyVelocity = 0f;
            cameraVelocity = 0f;
            hasHeights = true;
        }
        else
        {
            bodyHeight = Mathf.SmoothDamp(bodyHeight, targetHeight, ref bodyVelocity, BodyEaseTime);
            cameraHeight = Mathf.SmoothDamp(cameraHeight, targetHeight, ref cameraVelocity,
                profile.CameraEaseTime);
        }

        bodyHeight = Mathf.Clamp(bodyHeight, feetY - limit, feetY + limit);
        cameraHeight = Mathf.Clamp(cameraHeight, feetY - limit, feetY + limit);
        CameraOffset = cameraHeight - feetY;
        ApplyToModel(bodyHeight - feetY);
    }

    private bool SampleGround(Vector3 center, float feetY, Vector3 direction,
        CharacterLocomotionProfile profile, out float windowAverage)
    {
        windowAverage = feetY;
        float reach = motor.MaxStepHeight;
        float distance = profile.StepSmoothingDistance;
        float originY = feetY + reach + RayMargin;
        float rayLength = reach * 2f + RayMargin * 2f;
        if (!TryGetGroundHeight(center, originY, rayLength, out float centerHeight))
            return false;

        float weightedSum = 0f;
        float weightTotal = 0f;
        float halfSpan = (SampleCount - 1) * 0.5f;
        for (int i = 0; i < SampleCount; i++)
        {
            Vector3 point = center + direction * (distance * (i - halfSpan) / halfSpan);
            float height = TryGetGroundHeight(point, originY, rayLength, out float sampled) &&
                           Mathf.Abs(sampled - centerHeight) <= reach * HeightTolerance
                ? sampled
                : centerHeight;
            weightedSum += height * SampleWeights[i];
            weightTotal += SampleWeights[i];
        }

        windowAverage = weightedSum / weightTotal;
        return true;
    }

    private bool TryGetGroundHeight(Vector3 point, float originY, float rayLength, out float height)
    {
        Vector3 origin = new Vector3(point.x, originY, point.z);
        if (motor.TryFindWalkableGround(origin, rayLength, out RaycastHit hit))
        {
            height = hit.point.y;
            return true;
        }

        height = 0f;
        return false;
    }

    private void ApplyToModel(float bodyOffset)
    {
        if (modelRoot != null)
            modelRoot.localPosition = modelRestLocalPosition + Vector3.up * bodyOffset;
    }
}
