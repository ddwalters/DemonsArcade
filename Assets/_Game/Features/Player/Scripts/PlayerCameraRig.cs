using UnityEngine;
using UnityEngine.Serialization;

[DefaultExecutionOrder(100)]
public class PlayerCameraRig : MonoBehaviour
{
    private const float ObstructionReturnTime = 0.22f;
    private const float CollisionRadius = 0.2f;
    private const float CollisionPadding = 0.05f;

    [SerializeField] private Camera viewCamera;
    [SerializeField] private Transform viewPivot;
    [Tooltip("Where the camera sits relative to the character: right, up, back.")]
    [SerializeField] private Vector3 thirdPersonOffset = new Vector3(0.45f, 0.2f, -3.25f);
    [SerializeField, Range(1f, 179f)] private float fieldOfView = 60f;
    [Tooltip("Layers that push the camera in so it doesn't clip through walls.")]
    [FormerlySerializedAs("collisionMask")]
    [SerializeField] private LayerMask collisionLayers = ~((1 << 2) | (1 << 5) | (1 << 9));

    private float distance;
    private float distanceVelocity;
    private float pitch;
    private float yaw;
    private int playerLayer;
    private StepSmoothing stepSmoothing;
    private Vector3 pivotRestLocalPosition;

    public Camera ViewCamera => viewCamera;

    private void Awake()
    {
        playerLayer = 1 << gameObject.layer;
        if (viewPivot != null)
            pivotRestLocalPosition = viewPivot.localPosition;

        ApplyFieldOfView();
        ResetView();
    }

    private void Start() => stepSmoothing = GetComponent<StepSmoothing>();

    private void OnValidate() => ApplyFieldOfView();

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

    public void ApplyFieldOfView()
    {
        if (viewCamera != null)
            viewCamera.fieldOfView = fieldOfView;
    }

    private void ResetPlacement()
    {
        distanceVelocity = 0f;
        PlaceCamera(0f, true);
    }

    private void LateUpdate() => PlaceCamera(Time.deltaTime, false);

    private void PlaceCamera(float deltaTime, bool immediate)
    {
        if (viewCamera == null || viewPivot == null)
            return;

        viewPivot.localPosition = pivotRestLocalPosition;
        if (stepSmoothing != null)
            viewPivot.position += Vector3.up * stepSmoothing.CameraOffset;

        // Keep orbit orientation in world space when movement turns the player root.
        viewPivot.rotation = Quaternion.Euler(pitch, yaw, 0f);

        float safeDistance = thirdPersonOffset.magnitude;
        Vector3 direction = safeDistance > 0.001f ? thirdPersonOffset / safeDistance : Vector3.zero;
        if (safeDistance > 0.001f)
        {
            if (Physics.CheckSphere(viewPivot.position, CollisionRadius, collisionLayers, QueryTriggerInteraction.Ignore))
                safeDistance = 0f;
            else if (Physics.SphereCast(viewPivot.position, CollisionRadius, viewPivot.TransformDirection(direction),
                out var hit, safeDistance, collisionLayers, QueryTriggerInteraction.Ignore))
                safeDistance = Mathf.Max(0f, hit.distance - CollisionPadding);
        }

        if (immediate || safeDistance < distance)
        {
            distance = safeDistance;
            distanceVelocity = 0f;
        }
        else if (deltaTime > 0f)
            distance = Mathf.SmoothDamp(distance, safeDistance, ref distanceVelocity,
                ObstructionReturnTime, Mathf.Infinity, deltaTime);

        viewCamera.transform.localPosition = direction * distance;
        viewCamera.transform.localRotation = Quaternion.identity;
        if (distance > 0.7f)
            viewCamera.cullingMask |= playerLayer;
        else
            viewCamera.cullingMask &= ~playerLayer;
    }
}
