using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        Interactor interactor = new Interactor();

        interactor.OpenLootView();
    }
}
