using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class doorInteractable : InteractableBase
{
    public bool isOpen = false;

    public bool opened = false;

    private MeshRenderer rend;
    private Collider coll;

    public override void OnInteract()
    {
        base.OnInteract();

        OpenDoor();
        opened = true;
    }

    private void Start()
    {
        rend = GetComponent<MeshRenderer>();
        coll = GetComponent<Collider>();
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

    public void CloseDoor()
    {
        rend.enabled = true;
        coll.enabled = true;
    }

    public void OpenDoor()
    {
        rend.enabled = false;
        coll.enabled = false;
    }

    public void Reopen()
    {
        if (opened == true)
            OpenDoor();
    }
}
