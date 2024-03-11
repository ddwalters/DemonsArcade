using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Inventory
{
    public enum Orientation
    {
        Up = 0,
        Right = 90,
        Down = 180,
        Left = 270
    }

    [Serializable]
    public struct InventoryItemToPlace
    {
        public ItemDataScriptableObject ItemPreset;
        public Vector2Int Position;
    }

    public class InventoryItem : MonoBehaviour, IInventoryItemData, IPointerEnterHandler, IPointerExitHandler
    {
        public int Width => _itemData.Width;
        public int Height => _itemData.Height;
        public bool[] OccupiedGrid => _itemData.OccupiedGrid;
        public bool[] OccupiedGridRotated => _itemData.OccupiedGridRotated;
        public Sprite Sprite => _itemData.Sprite;
        public Orientation Orientation => _itemData.Orientation;

        public RectTransform RectTransform;
        public Vector2Int Position;

        [SerializeField] private Image _image;

        IInventoryItemData _itemData;

        ItemStatsData _itemStatsData;

        ItemToolTip _itemToolTip;

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
            gameObject.AddComponent<Collider2D>();

            //_itemToolTip = GameObject.Find("ItemHoverToolTip").GetComponent<ItemToolTip>();
        }

        public void Initialize(IInventoryItemData itemData, ItemStatsData statsData, ItemToolTip itemToolTip)
        {
            _itemData = itemData;
            _itemStatsData = statsData;
            _itemToolTip = itemToolTip;
            RectTransform.sizeDelta = new Vector2(_itemData.Width * 32, _itemData.Height * 32);
            _image.sprite = _itemData.Sprite;
        }

        public bool[] GetGridRow(int row)
        {
            var gridRow = new bool[Width];
            for (var i = 0; i < Width; i++)
            {
                gridRow[i] = OccupiedGridRotated[row * Width + i];
            }

            return gridRow;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _itemToolTip.ShowToolTip(_itemStatsData.GetItemName(), _itemStatsData.CreateItemDescriptionText());
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            _itemToolTip.HideToolTip();
        }

        public void Rotate()
        {
            _itemData.Rotate();
            RectTransform.eulerAngles = new Vector3(0, 0, -(int)_itemData.Orientation);
            RectTransform.pivot = GetPivotFromOrientation(_itemData.Orientation);
        }

        private static Vector2 GetPivotFromOrientation(Orientation orientation)
        {
            switch (orientation)
            {
                case Orientation.Up:
                    return new Vector2(0, 1);
                case Orientation.Right:
                    return new Vector2(0, 0);
                case Orientation.Down:
                    return new Vector2(1, 0);
                case Orientation.Left:
                    return new Vector2(1, 1);
                default:
                    throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null);
            }
        }
    }
}

