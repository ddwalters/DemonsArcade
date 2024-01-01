using System.Collections;
using System.Collections.Generic;
using UnityEngine;

interface IInteractable
{
    public void Interact();

    public void OpenLootView()
    {
        MouseLook mouse = new MouseLook();
        mouse.unlockMouse();
    }

    public void CloseLootView()
    {
        MouseLook mouse = new MouseLook();
        mouse.lockMouse();
    }
}

public class Interactor : MonoBehaviour
{
    [SerializeField] Transform InteractorSource;
    [SerializeField] float InteractRange;

    void Update()
    {
        if(Input.GetKeyUp(KeyCode.E))
        {
            Ray r = new Ray(InteractorSource.position, InteractorSource.forward);
            if (Physics.Raycast(r, out RaycastHit hitInfo, InteractRange) && 
                hitInfo.collider.gameObject.TryGetComponent(out IInteractable interactableObj))
            {
                interactableObj.Interact();
            }
        }
    }
}
