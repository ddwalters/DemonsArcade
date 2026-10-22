using UnityEngine;

// Third-person orbit camera independent of the animated head and body root motion.
[DefaultExecutionOrder(100)]
public class PlayerCameraRig : MonoBehaviour
{
    public const string ThirdPersonFovKey = "Settings.ThirdPersonFov";
    public const float DefaultFov = 80f;
    public const float MinFov = 60f;
    public const float MaxFov = 110f;

    [SerializeField] private Camera viewCamera;
    [SerializeField] private Transform viewPivot;
    [SerializeField] private Vector3 thirdPersonOffset = new Vector3(0.45f, 0.2f, -3.25f);
    [SerializeField, Min(0.01f)] private float transitionTime = 0.18f;
    [SerializeField, Min(0.01f)] private float obstructionReturnTime = 0.22f;
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.2f;
    [SerializeField, Min(0f)] private float collisionPadding = 0.05f;
    [SerializeField] private LayerMask collisionMask = ~((1 << 2) | (1 << 5) | (1 << 9));

    private Vector3 offset;
    private Vector3 offsetVelocity;
    private float distance;
    private float distanceVelocity;
    private float pitch;
    private float yaw;
    private int playerLayer;

    public Camera ViewCamera => viewCamera;
    public Vector3 EyePosition => viewPivot.position;
    public bool IsThirdPerson => true;
    public float Pitch => pitch;
    public Ray AimRay => new Ray(viewCamera.transform.position, viewCamera.transform.forward);

    private void Awake()
    {
        playerLayer = 1 << gameObject.layer;
        ApplyFieldOfView();
        ResetView();
    }

    public void Look(Vector2 delta)
    {
        yaw = Mathf.Repeat(yaw + delta.x, 360f);
        pitch = Mathf.Clamp(pitch - delta.y, -60f, 70f);
    }

    public Vector3 GetMovementDirection(Vector2 input) =>
        Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y);

    public void ResetView()
    {
        pitch = 0f;
        yaw = transform.eulerAngles.y;
        ResetPlacement();
    }

    public static float GetSavedFieldOfView() => Mathf.Clamp(
        PlayerPrefs.GetFloat(ThirdPersonFovKey, DefaultFov), MinFov, MaxFov);

    public static void SaveFieldOfView(float value) => PlayerPrefs.SetFloat(
        ThirdPersonFovKey, Mathf.Clamp(value, MinFov, MaxFov));

    public void ApplyFieldOfView() => viewCamera.fieldOfView = GetSavedFieldOfView();

    private void ResetPlacement()
    {
        offset = thirdPersonOffset;
        offsetVelocity = Vector3.zero;
        distanceVelocity = 0f;
        PlaceCamera(0f, true);
    }

    private void LateUpdate() => PlaceCamera(Time.deltaTime, false);

    private void PlaceCamera(float deltaTime, bool immediate)
    {
        if (viewCamera == null || viewPivot == null)
            return;

        // Keep orbit orientation in world space when movement turns the player root.
        viewPivot.rotation = Quaternion.Euler(pitch, yaw, 0f);

        if (!immediate && deltaTime > 0f)
            offset = Vector3.SmoothDamp(offset, thirdPersonOffset, ref offsetVelocity,
                transitionTime, Mathf.Infinity, deltaTime);

        float safeDistance = offset.magnitude;
        Vector3 direction = safeDistance > 0.001f ? offset / safeDistance : Vector3.zero;
        if (safeDistance > 0.001f)
        {
            if (Physics.CheckSphere(viewPivot.position, collisionRadius, collisionMask, QueryTriggerInteraction.Ignore))
                safeDistance = 0f;
            else if (Physics.SphereCast(viewPivot.position, collisionRadius, viewPivot.TransformDirection(direction),
                out var hit, safeDistance, collisionMask, QueryTriggerInteraction.Ignore))
                safeDistance = Mathf.Max(0f, hit.distance - collisionPadding);
        }

        if (immediate || safeDistance < distance)
        {
            distance = safeDistance;
            distanceVelocity = 0f;
        }
        else if (deltaTime > 0f)
            distance = Mathf.SmoothDamp(distance, safeDistance, ref distanceVelocity,
                obstructionReturnTime, Mathf.Infinity, deltaTime);

        viewCamera.transform.localPosition = direction * distance;
        viewCamera.transform.localRotation = Quaternion.identity;
        if (distance > 0.7f)
            viewCamera.cullingMask |= playerLayer;
        else
            viewCamera.cullingMask &= ~playerLayer;
    }
}
