using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("InteractableSettings")]
    public float holdDuration; // may need to be public
    public float HoldDuration => holdDuration;

    public bool holdInteract;
    public bool HoldInteract => holdInteract;


    public bool multipleUse;
    public bool MultipleUse => multipleUse;


    public bool isInteractable;
    public bool IsInteractable => isInteractable;

    public void OnInteract()
    {
        Debug.Log("Interacted" + gameObject.name);
    }
}
