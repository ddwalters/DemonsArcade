using UnityEngine;


namespace NodeTree
{
    public class NodeTreeTrigger : MonoBehaviour
    {
        [Header("Node Tree")]
        public NodeTreeGraphSO nodeTree;

        [Header("Decorum")]
        public Sprite icon;

        public void Interact()
        {
            if (DialogRunner.Instance != null)
                DialogRunner.Instance.StartDialog(nodeTree);
            else
                Debug.LogError("DialogRunner not found in scene. Add a DialogManager GameObject with a DialogRunner component.");
        }

        public void OnFocus()
        {
            // For sure:
            //  Call when user walks within a certain radius (), OR when the user is looking in the objects direction
            //  

            // I want to create a new gameobject with the sprite to aim towards the players camers, hovering over the top of the gameobject.
            // Questions:
            //  How should it work with both 2d and 3d. I thought floating sprite overhead, so I can animate it to float/ move and look towards the character.
            //  It will be a raycast hitting it, how can I succesfully make the gameobject/ sprite on the item when it's hit.

            // My thought it this script will always need both this script and the other 'Indicator' icon attatched to every interactable game object which seems tedius. 
            // I may rather just add the game object on hover, then deactivate or reactivate later if the player has hovered over it before.
        }

        public void OnUnfocus()
        {

        }
    }
}