using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private InteractionInputData interactionInputData;
    [SerializeField] private InteractionData interactionData;

    [Space]
    [Header("Ray Settings")]
    [SerializeField] private float rayDistance;
    [SerializeField] private float raySphereRadius;
    [SerializeField] private LayerMask interactableLayer;

    private IGridCreator _gridCreator;
    private IPauseMenu _pauseMenu;
    private IInventoryMenu _inventoryMenu;
    private PlayerInput _playerInput;
    private Camera _camera;

    private bool _interacting;

    private float _holdTimer = 0f;

    private void Awake()
    {
        _playerInput = FindAnyObjectByType<PlayerInput>();
        _camera = FindAnyObjectByType<Camera>();
        _pauseMenu = FindAnyObjectByType<PauseMenu>();
        _inventoryMenu = gameObject.GetComponent<InventoryManager>();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!_interacting)
        {
            _interacting = true;

            Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
            bool hitSomething = Physics.SphereCast(ray, raySphereRadius, out var hit, rayDistance, interactableLayer);
            Debug.DrawRay(ray.origin, ray.direction * rayDistance, hitSomething ? Color.green : Color.red);

            if (hitSomething)
            {
                Cursor.lockState = CursorLockMode.Confined;
                _gridCreator = hit.collider.GetComponentInParent<Grid>();
                if (_gridCreator != null)
                    _gridCreator.OpenGridMenu();
            }
            else
            {
                _interacting = false;
            }
        }
        else
        {
            _interacting = false;
            _gridCreator.CloseGridMenu();
            _gridCreator = null;
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (_interacting)
        {
            _interacting = false;
            _gridCreator.CloseGridMenu();
            _gridCreator = null;
        }

        if (_pauseMenu.GetIsPaused())
        {
            _playerInput.SwitchCurrentActionMap("Player");
            _pauseMenu.Resume();
        }
        else
        {
            _playerInput.SwitchCurrentActionMap("Default/UI");
            _pauseMenu.Options();
        }
    }

    public void OnInventory(InputAction.CallbackContext context)
    {
        if (_interacting)
        {
            _interacting = false;
            _gridCreator.CloseGridMenu();
            _gridCreator = null;
        }

        if (_inventoryMenu.GetIsOpen())
        {
            _playerInput.SwitchCurrentActionMap("Player");
            _inventoryMenu.Resume();
        }
        else
        {
            _playerInput.SwitchCurrentActionMap("Default/UI");
            _inventoryMenu.OpenInventory();
        }
    }
}
