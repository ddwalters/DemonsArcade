using UnityEngine;
using UnityEngine.EventSystems;

namespace Inventory
{
    public class GridInteractableManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private ItemGrid _grid;

        public GridInventoryController _controller;

        public void OnPointerEnter(PointerEventData eventData)
        {
            _controller.FocusGrid(_grid);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _controller.UnfocusGrid(_grid);
        }

        private void Start() => _controller = GridInventoryController.Instance;

        public GridInventoryController GetController()
        {
            return _controller;
        }
    }
}