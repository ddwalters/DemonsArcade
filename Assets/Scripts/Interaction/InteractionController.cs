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

    private IGridCreator gridCreator;
    private PlayerControls playerControls;
    private Camera _camera;

    private bool _interacting;

    private float _holdTimer = 0f;

    private void Awake()
    {
        playerControls = new PlayerControls();
        _camera = FindAnyObjectByType<Camera>();
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!_interacting)
        {
            _interacting = true;

            Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
            bool hitSomething = Physics.SphereCast(ray, raySphereRadius, out var hit, rayDistance, interactableLayer);
            Debug.DrawRay(ray.origin, ray.direction * rayDistance, hitSomething ? Color.green : Color.red);

            Debug.Log($"Interated: {hit.collider.name}");
            if (hitSomething)
            {
                Cursor.lockState = CursorLockMode.Locked;
                gridCreator = hit.collider.GetComponent<IGridCreator>();
                gridCreator.OpenGridMenu();
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;

            gridCreator.CloseGridMenu();

            gridCreator = gameObject.GetComponent<IGridCreator>();
        }

        _interacting = false;
    }
}
