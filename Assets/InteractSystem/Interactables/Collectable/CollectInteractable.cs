using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectInteractable : InteractableBase
{
    public override void OnInteract()
    {
        base.OnInteract();

        Debug.Log("+1 mushroom");
        Destroy(gameObject);
    }   
}
