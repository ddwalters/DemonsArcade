using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrateInteractable : InteractableBase
{
    public override void OnInteract()
    {
        base.OnInteract();

        Debug.Log("Open Crate Loot View");
    }   
}