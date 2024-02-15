using UnityEngine;
using UnityEngine.EventSystems;

namespace Inventory
{
    public class GridInteractableManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private ItemGrid _grid;

        private GridInventoryController _controller;

        // TODO: @DW update this to not use new input method
        public void OnPointerEnter(PointerEventData eventData) => _controller.FocusGrid(_grid);

        // TODO: @DW update this to not use new input method
        public void OnPointerExit(PointerEventData eventData) => _controller.UnfocusGrid(_grid);

        private void Start() => _controller = GridInventoryController.Instance;
    }
}