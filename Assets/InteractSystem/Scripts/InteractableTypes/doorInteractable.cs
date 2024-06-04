using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class doorInteractable : InteractableBase
{
    public override void OnInteract()
    {
        base.OnInteract();

        openDoor();
    }

    public void closeDoor()
    {
        gameObject.SetActive(true);
    }

    public void openDoor()
    {
        gameObject.SetActive(false);
    }
}
