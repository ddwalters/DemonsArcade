using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour
{
    [Header("Touch Interactor")]
    [SerializeField] private Transform _interactionPoint;
    [SerializeField] private float _interactionPointRadius = 0.5f;
    [SerializeField] private LayerMask _interactableMask;
    [SerializeField] private GameObject interactableMessageObject;
    [SerializeField] private InteractionPromptUI _interactionPromptUI;

    private readonly Collider[] _colliders = new Collider[3];
    [SerializeField] private int _numFound;

    [Header("Ray Interactor")]
    [SerializeField] private Transform _rayInteractorSource;
    [SerializeField] private float _rayInteractRange;

    private IInteractable _interactable;

    void Start()
    {
        _interactionPromptUI = interactableMessageObject.GetComponent<InteractionPromptUI>();
    }

    void Update()
    {
        // touch interaction  -- for opening chest, doors, ect.
        _numFound = Physics.OverlapSphereNonAlloc(_interactionPoint.position, _interactionPointRadius, _colliders, _interactableMask);

        if (_numFound > 0)
        {
            _interactable = _colliders[0].GetComponent<IInteractable>();
            _interactionPromptUI.SetUp(_interactable.InteractionPrompt);

            if (_interactable != null && Input.GetKeyUp(KeyCode.E))
            {
                if (Input.GetKeyUp(KeyCode.E))
                    _interactable.Interact(this);
            }
        }
        else
        {
            if (_interactable != null)
                _interactable = null;

            _interactionPromptUI.Close();
        }

        //raycast interaction -- for collecting loot on da floor
        if (Input.GetKeyUp(KeyCode.E))
        {
            Ray ray = new Ray(_rayInteractorSource.position, _rayInteractorSource.forward);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, _rayInteractRange))
                if (hitInfo.collider.gameObject.TryGetComponent(out IInteractable interactableObj))
                    interactableObj.Interact(this);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_interactionPoint.position, _interactionPointRadius);
    }
}