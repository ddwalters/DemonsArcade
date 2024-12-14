using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestInteractable : InteractableBase
{
    [SerializeField] string ExtraChestOption;
    public override void OnInteract()
    {
        base.OnInteract();
    }
}
