using UnityEngine;

namespace Inventory
{
    /*
        All of these Interfaces could most likely be merged into one
        Good practice is to not give information that isn't needed but 
        having 3 interfaces for one thing doesn't seem swell either
     */

    public interface ICursorInput
    {
        public Vector2 CursorPosition { get; }
    }

    public interface IInteractInput
    {
        public bool Interact { get; set; }
    }

    public interface IUseInput
    {
        public bool Use { get; set; }
    }

    public class InventoryInputs : MonoBehaviour, IInteractInput, ICursorInput, IUseInput
    {
        private Vector2 cursorPosition;
        public Vector2 CursorPosition
        {
            get => cursorPosition;
            set => cursorPosition = value;
        }

        private bool interact;
        public bool Interact
        {
            get => interact;
            set => interact = value;
        }

        private bool use;
        public bool Use
        {
            get => use;
            set => use = value;
        }

        private void Update()
        {
            CursorPosition = Input.mousePosition;

            DragItem();
            RotateItem();
        }

        private void DragItem()
        {
            if (Input.GetMouseButtonDown(0))
            {
                interact = true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                interact = false;
            }
        }

        private void RotateItem()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                use = true;
            }

            if (Input.GetKeyUp(KeyCode.R))
            {
                use = false;
            }
        }
    }
}