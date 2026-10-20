using UnityEngine;

public class InteractionController : MonoBehaviour
{
    // Retain serialized references for existing prefabs; temporary state lives on this controller.
    [SerializeField] private InteractionInputData interactionInputData;
    [SerializeField] private InteractionData interactionData;
    [SerializeField] private InteractionUIPanel uiPanel;
    [SerializeField] private GameObject playerHud;
    [SerializeField] private GameObject playerBackpack;
    [SerializeField] private float rayDistance;
    [SerializeField] private float raySphereRadius;
    [SerializeField] private LayerMask interactableLayer;
    private Camera playerCamera;
    private PlayerModeController mode;
    private InteractableBase target;
    private bool interacting;
    private float holdTimer;

    private void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
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
        Ray ray = new(playerCamera.transform.position, playerCamera.transform.forward);
        InteractableBase candidate = Physics.SphereCast(ray, raySphereRadius, out var hit,
            rayDistance, interactableLayer) ? hit.transform.GetComponentInParent<InteractableBase>() : null;
        SetTarget(candidate);
        ProcessInput(Input.GetKeyDown(KeyCode.E), Input.GetKeyUp(KeyCode.E), Time.deltaTime);
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
        if (interactionData != null)
            interactionData.ResetData();
        if (interactionInputData != null)
            interactionInputData.ResetInput();
    }

    public void OpenLoot(int gridId) => mode.OpenLoot(gridId);
}
