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
    private PlayerControls _playerControls;
    private Camera _camera;

    private bool _interacting;

    private float _holdTimer = 0f;

    private void Awake()
    {
        _playerControls = new PlayerControls();
        _camera = FindAnyObjectByType<Camera>();
        _pauseMenu = FindAnyObjectByType<PauseMenu>();
    }

    private void OnEnable()
    {
        _playerControls.Enable();
    }

    private void OnDisable()
    {
        _playerControls.Disable();
    }

    private void Update()
    {
        if (_gridCreator == null)
        {
            _gridCreator = gameObject.GetComponent<IGridCreator>(); // @DW Player grid wont be stored on player, what if opening armor?
            if (_gridCreator == null)
                Debug.Log("Can't retrive player grid");
        }
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
            _gridCreator.CloseGridMenu();
            _interacting = false;
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (_interacting)
        {
            _interacting = false;
            _gridCreator.CloseGridMenu();
        }

        if (_pauseMenu.GetIsPaused())
            _pauseMenu.resume();
        else
            _pauseMenu.options();
    }
}
