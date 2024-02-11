using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [Header("Data")]
    public InteractionInputData interactionInputData;

    public InteractionData interactionData;

    [Space]
    [Header("Ray Settings")]
    public float rayDistance;

    public float raySphereRadius;

    public LayerMask interactableLayer;

    private Camera _camera;

    private void Awake()
    {
        _camera = FindAnyObjectByType<Camera>();
    }

    private void Update()
    {
        //CheckForInteractable();
        CheckForInteractableInput();
    }

    private void CheckForInteractableInput()
    {
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
        RaycastHit hit;

        bool hitSomething = Physics.SphereCast(ray, raySphereRadius, out hit, rayDistance, interactableLayer);

        if (hitSomething)
        {
            InteractableBase interactable = hit.transform.GetComponent<InteractableBase>();

            if (interactable != null)
            {
                if (interactionData.IsEmpty())
                {
                    interactionData.Interactable = interactable;
                }
                else
                {
                    if (!interactionData.IsSameInteractable(interactable))
                        interactionData.Interactable = interactable;
                }
            }
        }
        else
        {
            interactionData.ResetData();
        }

        Debug.DrawRay(ray.origin, ray.direction * rayDistance, hitSomething ? Color.green : Color.red);
    }

    private void CheckForInteractable()
    {
        throw new NotImplementedException();
    }
}
