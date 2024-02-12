using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("InteractableSettings")]
    [SerializeField] private float holdDuration;
    public float HoldDuration => holdDuration;

    [SerializeField] private bool holdInteract;
    public bool HoldInteract => holdInteract;


    [SerializeField] private bool multipleUse;
    public bool MultipleUse => multipleUse;

    [SerializeField] private bool isInteractable;
    public bool IsInteractable => isInteractable;

    [SerializeField] private string tooltipMessage = "Interact";
    public string TooltipMessage => tooltipMessage;

    public virtual void OnInteract()
    {
        Debug.Log("Interacted: " + gameObject.name);
        // might use this to have popup?
    }
}
