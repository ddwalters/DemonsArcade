using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private InteractionInputData interactionInputData;
    [SerializeField] private InteractionData interactionData;

    [Header("UI")]
    [SerializeField] private InteractionUIPanel uiPanel;
    [SerializeField] private GameObject playerHud;
    [SerializeField] private GameObject playerBackpack;

    [Space]
    [Header("Ray Settings")]
    [SerializeField] private float rayDistance;
    [SerializeField] private float raySphereRadius;
    [SerializeField] private LayerMask interactableLayer;

    private Camera _camera;

    private bool _interacting;

    private bool _lootingInteracted;

    private float _holdTimer = 0f;

    private void Awake()
    {
        _camera = FindAnyObjectByType<Camera>();
        playerBackpack = GameObject.Find("Inventory");
        playerHud = GameObject.Find("PlayerHud");
        uiPanel = FindAnyObjectByType<InteractionUIPanel>();
    }

    public void GetNewComponents()
    {
        playerBackpack = GameObject.Find("Inventory");
        playerHud = GameObject.Find("PlayerHud");
        uiPanel = FindAnyObjectByType<InteractionUIPanel>();
    }

    private void Update()
    {
        interactionInputData.InteractClicked = Input.GetKeyDown(KeyCode.E);
        interactionInputData.InteractRelease = Input.GetKeyUp(KeyCode.E);

        if (uiPanel == null)
            return;
        
        CheckForInteractable();
        CheckForInteractableInput();
    }

    private void CheckForInteractable()
    {
        if (_lootingInteracted)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                var grids = FindObjectsByType<InventoryGrid>(FindObjectsSortMode.None);
                foreach (var grid in grids)
                    grid.CloseGrid();

                Cursor.lockState = CursorLockMode.Locked;
                _lootingInteracted = false;
            }

            return;
        }

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
        if (interactionData.IsEmpty() || _lootingInteracted)
        {
            _holdTimer = 0f;
            uiPanel.UpdateProgressBar(_holdTimer);
            return;
        }

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

    public void SetLootingInteracted()
    {
        _lootingInteracted = true;
    }
}
