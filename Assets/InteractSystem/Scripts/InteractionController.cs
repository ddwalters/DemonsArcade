using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    [SerializeField] private GameObject playerInventory;

    [Space]
    [Header("Ray Settings")]
    [SerializeField] private float rayDistance;

    [SerializeField] private float raySphereRadius;

    [SerializeField] private LayerMask interactableLayer;

    private Camera _camera;

    private bool _interacting;

    private float _holdTimer = 0f;

    private void Awake()
    {
        _camera = FindAnyObjectByType<Camera>();
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

    public void ActivatePlayerHub()
    {
        DeactivateHud();
        playerHud.SetActive(true);
        FirstPersonController playerController = GetComponent<FirstPersonController>();
        playerController.lockCursor = true;
        playerController.playerCanMove = true;
        //activate player and enemy controllers
    }

    public void ActivateLootView()
    {
        DeactivateHud();
        playerInventory.SetActive(true);
        FirstPersonController playerController = GetComponent<FirstPersonController>();
        playerController.lockCursor = false;
        playerController.playerCanMove = false;
        //deactivate player and enemy controllers
    }

    public void DeactivateHud()
    {
        playerHud.SetActive(false);
        playerInventory.SetActive(false);
    }
}
