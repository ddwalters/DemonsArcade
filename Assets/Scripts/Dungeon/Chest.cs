using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    [SerializeField] string _prompt;
    public string InteractionPrompt => _prompt;

    GameObject lootChestUI;
    GameObject backpackUI;

    void Start()
    {
        lootChestUI = GameObject.FindGameObjectWithTag("LootView");
        backpackUI = GameObject.FindGameObjectWithTag("InventoryView");

        backpackUI.SetActive(false);
        lootChestUI.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.E))
        {
            backpackUI.SetActive(false);
            lootChestUI.SetActive(false);
        }
    }

    public bool Interact(Interactor interactor)
    {
        backpackUI.SetActive(true);
        lootChestUI.SetActive(true);
        return true;
    }    
}
