using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [SerializeField] private InteractionUIPanel uiPanel;
    [SerializeField] private float rayDistance;
    [SerializeField] private float raySphereRadius;
    [SerializeField] private LayerMask interactableLayer;
    private Camera playerCamera;
    private PlayerCameraRig cameraRig;
    private PlayerModeController mode;
    private InteractableBase target;
    private bool interacting;
    private float holdTimer;

    private void Start()
    {
        mode = PlayerModeController.ForPlayer(GetComponent<PlayerController>());
        mode.ModeChanged += OnModeChanged;
        GetNewComponents();
    }

    private void OnDestroy()
    {
        if (mode != null)
            mode.ModeChanged -= OnModeChanged;
    }

    private void OnDisable() => ResetTarget();
    private void OnModeChanged(PlayerMode next) => ResetTarget();

    public void GetNewComponents()
    {
        playerCamera = GetComponentInChildren<Camera>();
        cameraRig = GetComponent<PlayerCameraRig>();
        uiPanel = FindAnyObjectByType<InteractionUIPanel>();
        ResetTarget();
    }

    private void Update()
    {
        if (mode == null || !mode.AllowsGameplay || mode.ChangedFrame == Time.frameCount ||
            playerCamera == null || uiPanel == null)
        {
            ResetTarget();
            return;
        }
        SetTarget(FindTarget());
        ProcessInput(Input.GetKeyDown(KeyCode.E), Input.GetKeyUp(KeyCode.E), Time.deltaTime);
    }

    private InteractableBase FindTarget()
    {
        Vector3 eye = cameraRig != null ? cameraRig.EyePosition : playerCamera.transform.position;
        Ray ray = new(playerCamera.transform.position, playerCamera.transform.forward);
        float cameraDistance = Vector3.Distance(ray.origin, eye);
        if (!Physics.SphereCast(ray, raySphereRadius, out var hit, rayDistance + cameraDistance,
            interactableLayer, QueryTriggerInteraction.Collide))
            return null;

        var candidate = hit.transform.GetComponentInParent<InteractableBase>();
        if (candidate == null || Vector3.Distance(eye, hit.point) > rayDistance ||
            Vector3.Dot(hit.point - eye, ray.direction) < -raySphereRadius)
            return null;

        int obstructionMask = ~((1 << gameObject.layer) | (1 << 2) | (1 << 5));
        if (Physics.Linecast(eye, hit.point, out var obstruction, obstructionMask, QueryTriggerInteraction.Ignore) &&
            obstruction.transform.GetComponentInParent<InteractableBase>() != candidate)
            return null;

        return candidate;
    }

    private void SetTarget(InteractableBase next)
    {
        if (target == next)
            return;

        CancelHold();
        target = next;
        if (uiPanel != null)
        {
            uiPanel.ResetUI();
            if (target != null)
                uiPanel.SetToolTip(target.TooltipMessage);
        }
    }

    private void ProcessInput(bool pressed, bool released, float deltaTime)
    {
        if (released)
            CancelHold();

        if (target == null || !target.IsInteractable)
        {
            CancelHold();
            return;
        }

        uiPanel?.SetToolTip(target.TooltipMessage);
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
            uiPanel?.UpdateProgressBar(holdTimer / target.HoldDuration);
            if (holdTimer < target.HoldDuration)
                return;
        }

        var interacted = target;
        ResetTarget();
        interacted.OnInteract();
    }

    private void CancelHold()
    {
        interacting = false;
        holdTimer = 0f;
        uiPanel?.UpdateProgressBar(0f);
    }

    private void ResetTarget()
    {
        CancelHold();
        target = null;
        uiPanel?.ResetUI();
    }

    public void OpenLoot(int gridId) => mode.OpenLoot(gridId);
}
