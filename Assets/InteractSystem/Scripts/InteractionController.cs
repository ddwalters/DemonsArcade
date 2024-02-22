using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private InteractionInputData interactionInputData;
    [SerializeField] private InteractionData interactionData;

    [Header("UI")]
    [SerializeField] private InteractionUIPanel uiPanel;

    [Header("UI.2")]
    [SerializeField] private GameObject playerHud;
    [SerializeField] private GameObject playerGrid;
    [SerializeField] private GameObject LootGrid;
    [SerializeField] private GameObject MerchantGrid;

    [Space]
    [Header("Ray Settings")]
    [SerializeField] private float rayDistance;
    [SerializeField] private float raySphereRadius;
    [SerializeField] private LayerMask interactableLayer;

    private FirstPersonController playerController;

    private Camera _camera;

    private bool _interacting;

    private float _holdTimer = 0f;

    private void Awake()
    {
        _camera = FindAnyObjectByType<Camera>();
    }

    private void Start()
    {
        playerController = GetComponent<FirstPersonController>();

        ActivatePlayerHud();
    }

    private void Update()
    {
        CheckForInteractable();
        CheckForInteractableInput();
    }

    private void CheckForInteractable()
    {
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
        bool hitSomething = Physics.SphereCast(ray, raySphereRadius, out var hit, rayDistance, interactableLayer);

        if (hitSomething)
        {
            InteractableBase interactable = hit.transform.GetComponentInParent<InteractableBase>();

            if (interactable != null)
            {
                if (interactionData.IsEmpty())
                {
                    interactionData.Interactable = interactable;
                    uiPanel.SetToolTip(interactable.TooltipMessage);
                }
                else
                {
                    if (!interactionData.IsSameInteractable(interactable))
                    {
                        interactionData.Interactable = interactable;
                        uiPanel.SetToolTip(interactable.TooltipMessage);
                    }
                }
            }
        }
        else
        {
            uiPanel.ResetUI();
            interactionData.ResetData();
        }

        Debug.DrawRay(ray.origin, ray.direction * rayDistance, hitSomething ? Color.green : Color.red);
    }

    private void CheckForInteractableInput()
    {
        if (interactionData.IsEmpty())
            return;

        if (interactionInputData.InteractClicked)
        {
            _interacting = true;
            _holdTimer = 0f;
        }

        if (interactionInputData.InteractRelease)
        {
            _interacting = false;
            _holdTimer = 0f;
            uiPanel.UpdateProgressBar(_holdTimer);
        }

        if (_interacting)
        {
            if (!interactionData.Interactable.IsInteractable)
                return;

            if (interactionData.Interactable.HoldInteract)
            {
                _holdTimer += Time.deltaTime;

                float progressTimer = _holdTimer / interactionData.Interactable.HoldDuration;
                uiPanel.UpdateProgressBar(progressTimer);

                if (progressTimer > 1f)
                {
                    interactionData.Interact();
                    _interacting = false;
                }
            }
            else
            {
                interactionData.Interact();
                _interacting = false;
            }
        }
    }

    public void ActivatePlayerHud()
    {
        DeactivateHud();
        playerHud.GetComponent<CanvasGroup>().alpha = 1.0f;
        Cursor.lockState = CursorLockMode.Locked;
        playerController.playerCanMove = true;
        playerController.cameraCanMove = true;
        //activate player and enemy controllers
    }

    public void ToggleLootView()
    {
        if (playerController.playerCanMove != true)
        {
            ActivatePlayerHud();
            return;
        }

        DeactivateHud();
        playerGrid.GetComponent<CanvasGroup>().alpha = 1.0f;
        LootGrid.GetComponent<CanvasGroup>().alpha = 1.0f;
        Cursor.lockState = CursorLockMode.Confined;
        playerController.playerCanMove = false;
        playerController.cameraCanMove = false;
        //deactivate player and enemy controllers
    }

    public void ToggleMerchantView()
    {
        if (playerController.playerCanMove != true)
        {
            ActivatePlayerHud();
            return;
        }

        DeactivateHud();
        playerGrid.GetComponent<CanvasGroup>().alpha = 1.0f;
        MerchantGrid.GetComponent<CanvasGroup>().alpha = 1.0f;
        Cursor.lockState = CursorLockMode.Confined;
        playerController.playerCanMove = false;
        playerController.cameraCanMove = false;
        //deactivate player and enemy controllers
    }

    public void TogglePlayerInventory()
    {
        if (playerController.playerCanMove != true)
        {
            ActivatePlayerHud();
            return;
        }

        DeactivateHud();
        playerGrid.GetComponent<CanvasGroup>().alpha = 1.0f;
        Cursor.lockState = CursorLockMode.Confined;
        playerController.playerCanMove = false;
        playerController.cameraCanMove = false;
        //activate player and enemy controllers

    }

    public void DeactivateHud()
    {
        playerHud.GetComponent<CanvasGroup>().alpha = 0.0f;
        playerGrid.GetComponent<CanvasGroup>().alpha = 0.0f;   
        LootGrid.GetComponent<CanvasGroup>().alpha = 0.0f;
        MerchantGrid.GetComponent<CanvasGroup>().alpha = 0.0f;
    }
}
