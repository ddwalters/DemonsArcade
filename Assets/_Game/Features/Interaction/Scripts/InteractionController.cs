using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionController : MonoBehaviour
{
    private const int NearbyColliderCapacity = 64;

    [SerializeField, Min(0.1f)] private float interactionDistance = 2f;
    [SerializeField, Range(0f, 90f)] private float interactionHalfAngle = 60f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Head look")]
    [SerializeField, Range(0f, 90f)] private float headYawLimit = 35f;
    [SerializeField, Range(0f, 90f)] private float headPitchLimit = 20f;
    [SerializeField, Range(0f, 1f)] private float headLookWeight = 0.55f;
    [SerializeField, Min(0f)] private float headLookSmoothness = 8f;

    private readonly Collider[] nearbyColliders = new Collider[NearbyColliderCapacity];
    private Camera playerCamera;
    private PlayerCameraRig cameraRig;
    private CharacterController characterController;
    private Animator characterAnimator;
    private Transform headBone;
    private PlayerController playerController;
    private PlayerInput playerInput;
    private InteractionUIPanel uiPanel;
    private PlayerModeController mode;
    private InputAction interactAction;
    private InteractableBase target;
    private Collider targetCollider;
    private Quaternion smoothedHeadLookOffset = Quaternion.identity;
    private Quaternion lastAnimatedHeadRotation;
    private bool hasAppliedHeadLook;
    private bool interacting;
    private float holdTimer;

    private void Start()
    {
        GetNewComponents();
    }

    private void OnDestroy()
    {
        if (mode != null)
            mode.ModeChanged -= OnModeChanged;
    }

    private void OnDisable()
    {
        RestoreAnimatedHeadPose();
        smoothedHeadLookOffset = Quaternion.identity;
        ResetTarget();
    }

    private void OnModeChanged(PlayerMode next) => ResetTarget();

    public void GetNewComponents()
    {
        RestoreAnimatedHeadPose();

        if (mode != null)
            mode.ModeChanged -= OnModeChanged;

        playerController = GetComponent<PlayerController>();
        mode = PlayerModeController.ForPlayer(playerController);
        if (mode != null)
            mode.ModeChanged += OnModeChanged;

        cameraRig = GetComponent<PlayerCameraRig>();
        playerCamera = cameraRig != null ? cameraRig.ViewCamera : GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        characterAnimator = GetComponentInChildren<Animator>();
        headBone = characterAnimator != null && characterAnimator.isHuman
            ? characterAnimator.GetBoneTransform(HumanBodyBones.Head)
            : null;
        smoothedHeadLookOffset = Quaternion.identity;
        playerInput = GetComponent<PlayerInput>();
        interactAction = playerController != null
            ? playerController.GetNewControls().BasicActionMap.Interact
            : null;
        uiPanel = FindAnyObjectByType<InteractionUIPanel>();
        ResetTarget();
    }

    private void Update()
    {
        RestoreAnimatedHeadPose();

        if (mode == null || !mode.AllowsGameplay || mode.ChangedFrame == Time.frameCount ||
            playerCamera == null || uiPanel == null || interactAction == null)
        {
            ResetTarget();
            return;
        }

        InteractableBase nextTarget = FindTarget(out Collider nextTargetCollider);
        SetTarget(nextTarget, nextTargetCollider);

        if (target != null)
        {
            uiPanel.SetTargetName(target.DisplayName);
            uiPanel.SetToolTip(GetTargetTooltip(), GetInteractBindingDisplayString());
            uiPanel.SetTargetWorldPosition(GetTargetNamePosition(), playerCamera);
        }

        ProcessInput(interactAction.WasPressedThisFrame(), interactAction.WasReleasedThisFrame(), Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (headBone == null)
            return;

        Vector3 targetDirection = targetCollider != null && target != null
            ? targetCollider.bounds.center - headBone.position
            : Vector3.zero;
        Quaternion targetHeadOffset = Quaternion.identity;

        if (targetDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 localDirection = transform.InverseTransformDirection(targetDirection.normalized);
            float horizontalLength = new Vector2(localDirection.x, localDirection.z).magnitude;
            float yaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Atan2(localDirection.y, horizontalLength) * Mathf.Rad2Deg;
            yaw = Mathf.Clamp(yaw, -headYawLimit, headYawLimit) * headLookWeight;
            pitch = Mathf.Clamp(pitch, -headPitchLimit, headPitchLimit) * headLookWeight;
            targetHeadOffset = Quaternion.Euler(-pitch, yaw, 0f);
        }

        float blend = 1f - Mathf.Exp(-headLookSmoothness * Time.deltaTime);
        smoothedHeadLookOffset = Quaternion.Slerp(smoothedHeadLookOffset, targetHeadOffset, blend);

        lastAnimatedHeadRotation = headBone.localRotation;
        Quaternion worldOffset = transform.rotation * smoothedHeadLookOffset *
                                 Quaternion.Inverse(transform.rotation);
        headBone.rotation = worldOffset * headBone.rotation;
        hasAppliedHeadLook = true;
    }

    private void RestoreAnimatedHeadPose()
    {
        if (!hasAppliedHeadLook || headBone == null)
            return;

        headBone.localRotation = lastAnimatedHeadRotation;
        hasAppliedHeadLook = false;
    }

    private InteractableBase FindTarget(out Collider nearestCollider)
    {
        nearestCollider = null;
        Vector3 reachOrigin = characterController != null
            ? characterController.bounds.center
            : transform.position + Vector3.up;
        int colliderCount = Physics.OverlapSphereNonAlloc(reachOrigin, interactionDistance,
            nearbyColliders, interactableLayer, QueryTriggerInteraction.Collide);

        InteractableBase nearestTarget = null;
        float nearestDistanceSquared = interactionDistance * interactionDistance;
        int obstructionMask = ~((1 << gameObject.layer) | (1 << 2) | (1 << 5));
        Vector3 horizontalForward = transform.forward;
        horizontalForward.y = 0f;
        float halfAngle = Mathf.Clamp(interactionHalfAngle, 0f, 90f);
        float minimumFacingDot = Mathf.Cos(halfAngle * Mathf.Deg2Rad);

        for (int i = 0; i < colliderCount; i++)
        {
            Collider candidateCollider = nearbyColliders[i];
            if (candidateCollider == null)
                continue;

            InteractableBase candidate = candidateCollider.GetComponentInParent<InteractableBase>();
            if (candidate == null || !candidate.IsInteractable)
                continue;

            Vector3 closestPoint = candidateCollider.ClosestPoint(reachOrigin);
            float distanceSquared = (closestPoint - reachOrigin).sqrMagnitude;
            if (distanceSquared > interactionDistance * interactionDistance)
                continue;

            Vector3 horizontalTargetDirection = candidateCollider.bounds.center - reachOrigin;
            horizontalTargetDirection.y = 0f;
            if (horizontalForward.sqrMagnitude > 0.0001f && horizontalTargetDirection.sqrMagnitude > 0.0001f)
            {
                float facingDot = Vector3.Dot(horizontalForward.normalized,
                    horizontalTargetDirection.normalized);
                if (facingDot < minimumFacingDot)
                    continue;
            }

            if (IsObstructed(reachOrigin, candidateCollider.bounds.center, candidate, obstructionMask))
                continue;

            bool isCloser = distanceSquared < nearestDistanceSquared - 0.0001f;
            bool winsTie = Mathf.Abs(distanceSquared - nearestDistanceSquared) <= 0.0001f &&
                           (nearestTarget == null || (candidate == target && nearestTarget != target));
            if (!isCloser && !winsTie)
                continue;

            nearestTarget = candidate;
            nearestCollider = candidateCollider;
            nearestDistanceSquared = distanceSquared;
        }

        return nearestTarget;
    }

    private static bool IsObstructed(Vector3 origin, Vector3 targetPoint, InteractableBase candidate,
        int obstructionMask)
    {
        if ((targetPoint - origin).sqrMagnitude <= 0.0001f)
            return false;

        if (!Physics.Linecast(origin, targetPoint, out RaycastHit obstruction, obstructionMask,
                QueryTriggerInteraction.Ignore))
            return false;

        return obstruction.transform.GetComponentInParent<InteractableBase>() != candidate;
    }

    private Vector3 GetTargetNamePosition()
    {
        if (targetCollider == null)
            return target != null ? target.transform.position + Vector3.up : transform.position;

        Bounds bounds = targetCollider.bounds;
        return bounds.center + Vector3.up * (bounds.extents.y + 0.2f);
    }

    private string GetInteractBindingDisplayString()
    {
        NewControls controls = playerController != null ? playerController.GetNewControls() : null;
        if (interactAction == null || controls == null)
            return string.Empty;

        string bindingGroup = playerInput != null ? playerInput.currentControlScheme : null;

        if (string.IsNullOrEmpty(bindingGroup))
        {
            var activeControl = controls.BasicActionMap.Look.activeControl;
            if (activeControl == null)
                activeControl = controls.BasicActionMap.Movement.activeControl;

            if (activeControl != null)
                bindingGroup = GetBindingGroup(activeControl.device);
        }

        if (string.IsNullOrEmpty(bindingGroup))
        {
            if (Keyboard.current != null)
                bindingGroup = "Keyboard & Mouse";
            else if (Gamepad.current != null)
                bindingGroup = "Gamepad";
        }

        if (!string.IsNullOrEmpty(bindingGroup))
        {
            var activeControl = interactAction.activeControl;
            if (activeControl != null && GetBindingGroup(activeControl.device) == bindingGroup &&
                IsControlBoundForGroup(activeControl, bindingGroup))
                return activeControl.shortDisplayName;

            string selectedBinding = interactAction.GetBindingDisplayString(group: bindingGroup);
            if (!string.IsNullOrEmpty(selectedBinding))
                return selectedBinding;
        }

        return interactAction.GetBindingDisplayString();
    }

    private string GetTargetTooltip()
    {
        if (target is ItemInteractable)
            return "Pick up";

        return target.TooltipMessage;
    }

    private bool IsControlBoundForGroup(InputControl control, string bindingGroup)
    {
        foreach (var binding in interactAction.bindings)
        {
            if (binding.isComposite || binding.isPartOfComposite ||
                !HasBindingGroup(binding.groups, bindingGroup))
                continue;

            if (InputControlPath.Matches(binding.effectivePath, control))
                return true;
        }

        return false;
    }

    private static bool HasBindingGroup(string bindingGroups, string bindingGroup)
    {
        if (string.IsNullOrEmpty(bindingGroups))
            return true;

        string delimitedGroups = $";{bindingGroups};";
        return delimitedGroups.Contains($";{bindingGroup};");
    }

    private static string GetBindingGroup(InputDevice device)
    {
        if (device is Gamepad)
            return "Gamepad";

        if (device is Keyboard || device is Mouse)
            return "Keyboard & Mouse";

        return null;
    }

    private void SetTarget(InteractableBase next, Collider nextCollider)
    {
        if (target == next)
        {
            targetCollider = nextCollider;
            return;
        }

        CancelHold();
        target = next;
        targetCollider = nextCollider;

        if (uiPanel != null)
        {
            uiPanel.ResetUI();
            if (target != null)
            {
                uiPanel.SetTargetName(target.DisplayName);
                uiPanel.SetToolTip(GetTargetTooltip(), GetInteractBindingDisplayString());
            }
        }
    }

    private void ProcessInput(bool pressed, bool released, float deltaTime)
    {
        if (target == null || !target.IsInteractable)
        {
            CancelHold();
            return;
        }

        if (released)
        {
            bool wasHolding = interacting && target.HoldInteract && target.HoldDuration > 0f;
            CancelHold();
            if (pressed && wasHolding)
                return;
        }

        if (pressed)
        {
            interacting = true;
            holdTimer = 0f;
        }

        if (!interacting)
            return;

        if (target.HoldInteract && target.HoldDuration > 0f)
        {
            holdTimer += deltaTime;
            if (uiPanel != null)
                uiPanel.UpdateProgressBar(holdTimer / target.HoldDuration);
            if (holdTimer < target.HoldDuration)
                return;
        }

        InteractableBase interacted = target;
        ResetTarget();
        interacted.OnInteract();
    }

    private void CancelHold()
    {
        interacting = false;
        holdTimer = 0f;
        if (uiPanel != null)
            uiPanel.UpdateProgressBar(0f);
    }

    private void ResetTarget()
    {
        CancelHold();
        target = null;
        targetCollider = null;
        if (uiPanel != null)
            uiPanel.ResetUI();
    }

    public void OpenLoot(int gridId) => mode.OpenLoot(gridId);
}
