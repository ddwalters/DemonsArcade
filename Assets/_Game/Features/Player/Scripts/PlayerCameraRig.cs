using UnityEngine;

// One gameplay camera, independent of the animated head and body root motion.
[DefaultExecutionOrder(100)]
public class PlayerCameraRig : MonoBehaviour
{
    public const string PreferenceKey = "Settings.ThirdPerson";
    public const string FirstPersonFovKey = "Settings.FirstPersonFov";
    public const string ThirdPersonFovKey = "Settings.ThirdPersonFov";
    public const float DefaultFov = 80f;
    public const float MinFov = 60f;
    public const float MaxFov = 110f;
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Transform viewPivot;
    [SerializeField] private Transform firstPersonRig;
    [SerializeField] private bool thirdPerson;

    [Header("Third person")]
    [SerializeField] private Vector3 thirdPersonOffset = new Vector3(0.45f, 0.2f, -3.25f);
    [SerializeField, Min(0.01f)] private float transitionTime = 0.18f;
    [SerializeField, Min(0.01f)] private float obstructionReturnTime = 0.22f;
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.2f;
    [SerializeField, Min(0f)] private float collisionPadding = 0.05f;
    [SerializeField] private LayerMask collisionMask = ~((1 << 2) | (1 << 5) | (1 << 9));

    private PlayerModeController mode;
    private Renderer[] firstPersonRenderers;
    private Vector3 offset;
    private Vector3 offsetVelocity;
    private float distance;
    private float distanceVelocity;
    private float pitch;
    private int playerLayer;

    public Camera ViewCamera => viewCamera;
    public Vector3 EyePosition => viewPivot.position;
    public bool IsThirdPerson => thirdPerson;
    public float Pitch => pitch;
    public Ray AimRay => new Ray(viewCamera.transform.position, viewCamera.transform.forward);

    private void Awake()
    {
        mode = GetComponent<PlayerModeController>();
        playerLayer = 1 << gameObject.layer;
        thirdPerson = PlayerPrefs.GetInt(PreferenceKey, thirdPerson ? 1 : 0) == 1;
        ApplyFieldOfView();
        RefreshFirstPersonRenderers();
        ResetView();
    }

    public void Look(Vector2 delta)
    {
        transform.Rotate(Vector3.up * delta.x);
        pitch = Mathf.Clamp(pitch - delta.y, thirdPerson ? -60f : -89f, thirdPerson ? 70f : 89f);
        viewPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void TogglePerspective()
    {
        if (mode != null && !mode.AllowsGameplay)
            return;

        SetThirdPerson(!thirdPerson);
        PlayerPrefs.Save();
    }

    public void SetThirdPerson(bool value, bool immediate = false)
    {
        thirdPerson = value;
        PlayerPrefs.SetInt(PreferenceKey, value ? 1 : 0);
        ApplyFieldOfView();
        pitch = Mathf.Clamp(pitch, thirdPerson ? -60f : -89f, thirdPerson ? 70f : 89f);
        viewPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        ApplyFirstPersonVisibility();
        if (immediate)
            ResetPlacement();
    }

    public void ResetView()
    {
        pitch = 0f;
        viewPivot.localRotation = Quaternion.identity;
        ResetPlacement();
    }

    public static float GetSavedFieldOfView(bool forThirdPerson) => Mathf.Clamp(
        PlayerPrefs.GetFloat(forThirdPerson ? ThirdPersonFovKey : FirstPersonFovKey, DefaultFov), MinFov, MaxFov);

    public static void SaveFieldOfView(bool forThirdPerson, float value) => PlayerPrefs.SetFloat(
        forThirdPerson ? ThirdPersonFovKey : FirstPersonFovKey, Mathf.Clamp(value, MinFov, MaxFov));

    public void ApplyFieldOfView() => viewCamera.fieldOfView = GetSavedFieldOfView(thirdPerson);

    private void ResetPlacement()
    {
        offset = thirdPerson ? thirdPersonOffset : Vector3.zero;
        offsetVelocity = Vector3.zero;
        distanceVelocity = 0f;
        PlaceCamera(0f, true);
    }

    private void LateUpdate() => PlaceCamera(Time.deltaTime, false);

    private void PlaceCamera(float deltaTime, bool immediate)
    {
        if (viewCamera == null || viewPivot == null)
            return;

        Vector3 target = thirdPerson ? thirdPersonOffset : Vector3.zero;
        if (!immediate && deltaTime > 0f)
            offset = Vector3.SmoothDamp(offset, target, ref offsetVelocity, transitionTime, Mathf.Infinity, deltaTime);

        float safeDistance = offset.magnitude;
        Vector3 direction = safeDistance > 0.001f ? offset / safeDistance : Vector3.zero;
        // Recheck the entire boom every frame. Pull in immediately; ease back only after clearance.
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
        // Hide the local body when a wall forces the camera into its head/torso.
        if (distance > 0.7f)
            viewCamera.cullingMask |= playerLayer;
        else
            viewCamera.cullingMask &= ~playerLayer;
        ApplyFirstPersonVisibility();
    }

    public void RefreshFirstPersonRenderers()
    {
        firstPersonRenderers = firstPersonRig != null
            ? firstPersonRig.GetComponentsInChildren<Renderer>(true)
            : System.Array.Empty<Renderer>();
        ApplyFirstPersonVisibility();
    }

    private void ApplyFirstPersonVisibility()
    {
        if (firstPersonRenderers == null)
            return;

        foreach (var renderer in firstPersonRenderers)
            if (renderer != null)
                renderer.forceRenderingOff = thirdPerson || distance > 0.7f;
    }
}
