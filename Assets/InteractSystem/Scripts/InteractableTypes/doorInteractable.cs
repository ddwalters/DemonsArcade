using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class doorInteractable : InteractableBase
{
    public bool isOpen = false;

    public override void OnInteract()
    {
        base.OnInteract();

        OpenDoor();
    }

    public void LockDoor()
    {
        SetInteractable(false);
        SetToolTip("LOCKED");
    }

    public void UnlockDoor()
    {
        SetInteractable(true);
        SetToolTip("Open");
    }

    public void CloseDoor() => gameObject.SetActive(true);

    public void OpenDoor() => gameObject.SetActive(false);
}
