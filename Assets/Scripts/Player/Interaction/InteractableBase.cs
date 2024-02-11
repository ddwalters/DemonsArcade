using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("InteractableSettings")]
    public float holdDuration;
    public float HoldDuration => holdDuration;

    public bool holdInteract;
    public bool HoldInteract => holdInteract;


    public bool multipleUse;
    public bool MultipleUse => multipleUse;


    public bool isInteractable;
    public bool IsInteractable => isInteractable;

    public virtual void OnInteract()
    {
        Debug.Log("Interacted: " + gameObject.name);
        // might use this to have popup?
    }
}
