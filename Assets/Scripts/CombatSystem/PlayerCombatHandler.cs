using UnityEngine;

public class PlayerCombatHandler : MonoBehaviour
{
    [SerializeField] ScriptableObject weaponLocations;

    InventoryManager inventoryManager;

    Sword swordHandler;
    Axe axeHandler;
    Shield shieldHandler;
    Staff staffHandler;

    private void Start()
    {
        inventoryManager = FindAnyObjectByType<InventoryManager>();
        swordHandler = GetComponentInChildren<Sword>();
        axeHandler = GetComponentInChildren<Axe>();
        shieldHandler = GetComponentInChildren<Shield>();
        staffHandler = GetComponentInChildren<Staff>();
    }

    private void Update()
    {
        // right hand basic attack - Mouse0
        // left hand basic attack - Mouse1

        // right heavy attack - Ctrl + Mouse0

        // if left hand has shield (block) - Mouse1

        // ---------------------------------------------
        // Mage staff
        // Basic attck - Mouse0
        // Charged attack - Ctrl + Mouse1
        // Prepared spell - Mouse1
        // (Prepared spells will have cooldown but can be prepared to be used on hotkeys 1, 2, and 3)


        // if an attack animation is playing return

        if (Input.GetKeyDown(KeyCode.Mouse0) && !Input.GetKeyDown(KeyCode.LeftControl))
        {
            // if weapon is in right hand
                // right hand basic attack

            // else if staff is in hand
                // Basic magic attck
        }

        if (Input.GetKeyDown(KeyCode.Mouse0) && Input.GetKeyDown(KeyCode.LeftControl))
        {
            // if weapon is in right hand
                // right heavy attack

            // else if staff in hand 
                // charged magic attack ( longer held, larger the spell and more damage done)
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            // if weapon is in left hand
                // left hand basic attack
            
            // else if staff in hand
                // readied attack (between 1, 2, 3)
        }
    }
}