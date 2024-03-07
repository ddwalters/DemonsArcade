using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Inventory
{
    public class ItemGrid : MonoBehaviour
    {
        [SerializeField] private Vector2Int _gridSquareSize = new(32, 32);
        [SerializeField] private Vector2Int _gridSize;
        [SerializeField] RectTransform _rectTransform;

        private Vector2 _positionOnGrid;
        private Vector2Int _gridPosition;

        private IGrid _gridBase;
        private IGetInventoryItems _inventoryItems;

        private bool storeTypeInventory;

        private void Awake()
        {
            _inventoryItems = GetComponent<IGetInventoryItems>();
        }

        private void Start()
        {
            InitializeGrid(_gridSize.x, _gridSize.y);
        }

        private void InitializeGrid(int x, int y)
        {
            storeTypeInventory = false;

            _rectTransform.sizeDelta = new Vector2(x * _gridSquareSize.x, y * _gridSquareSize.y);
            var gridData = new InventoryItem[x, y];
            var gridParameters = new GridParameters
            {
                GridSize = _gridSize,
                GridSquareSize = _gridSquareSize,
                GridInventoryData = gridData
            };
            _gridBase = new GridBase().InitializeGrid(gridParameters);

            if (_inventoryItems == null)
                return;

            var items = _inventoryItems.GetInventoryItems();
            var itemPrefab = _inventoryItems.GetItemPrefab();

            foreach (var item in items)
            {
                var itemObject = Instantiate(itemPrefab, _rectTransform);
                itemObject.Initialize(item.ItemPreset.ItemData);

                if (CheckItemFits(itemObject, item.Position.x, item.Position.y))
                {
                    PlaceItem(itemObject, item.Position.x, item.Position.y);
                    continue;
                }

                Destroy(itemObject.gameObject);
            }
        }

        public bool AddInventoryItem(InventoryItemToPlace newItem, InventoryItem prefab)
        {
            var items = _inventoryItems.GetInventoryItems() ?? new List<InventoryItemToPlace>();
            items.Add(newItem);

            var itemObject = Instantiate(prefab, _rectTransform);
            itemObject.Initialize(newItem.ItemPreset.ItemData);

            for (int i = 0; i < _gridSize.y; i++)
            {
                for (int j = 0; j < _gridSize.x; j++)
                {
                    if (CheckItemFits(itemObject, j, i))
                    {
                        PlaceItem(itemObject, j, i);

                        return true;
                    }
                }
            }

            return false;
        }

        public void SetInventory(List<InventoryItemToPlace> newItems, InventoryItem prefab, bool isMerchantInventory)
        {
            RemoveInventoryItems();
            storeTypeInventory = isMerchantInventory;

            foreach (var item in newItems)
            {
                bool itemPlaced = false;

                var itemObject = Instantiate(prefab, _rectTransform);
                itemObject.Initialize(item.ItemPreset.ItemData);

                for (int i = 0; i < _gridSize.y; i++)
                {
                    if (itemPlaced)
                        continue;

                    for (int j = 0; j < _gridSize.x; j++)
                    {
                        if (itemPlaced)
                            continue;

                        if (CheckItemFits(itemObject, j, i))
                        {
                            PlaceItem(itemObject, j, i);
                            itemPlaced = true;
                        }
                    }
                }
            }
        }

        public Vector2Int GetTiledGridPosition(Vector2 mousePosition)
        {
            _positionOnGrid.x = mousePosition.x - _rectTransform.position.x;
            _positionOnGrid.y = mousePosition.y - _rectTransform.position.y;

            _gridPosition.x = Mathf.FloorToInt(_positionOnGrid.x / _gridSquareSize.x);
            _gridPosition.y = -Mathf.FloorToInt(_positionOnGrid.y / _gridSquareSize.y) - 1;

            return _gridPosition;
        }

        public bool IsInventoryStoreType() => storeTypeInventory;

        public InventoryItem GrabItem(int x, int y) => _gridBase.GrabItem(x, y);

        public InventoryItem GetHoveredItem(int x, int y) => _gridBase.GetHoveredItem(x, y);

        public bool CheckItemFits(InventoryItem item, int x, int y) => _gridBase.CheckItemFits(item, x, y);

        public void PlaceItem(InventoryItem item, int x, int y)
        {
            item.RectTransform.SetParent(_rectTransform);
            item.Position = new Vector2Int(x, y);
            _gridBase.PlaceItem(item, x, y);
            var position = new Vector2(x * _gridSquareSize.x, -y * _gridSquareSize.y);
            item.RectTransform.localPosition = position;
        }

        public List<InventoryItem> GetUIItems()
        {
            var list = new List<InventoryItem>();

            for (int i = 0; i < _gridSize.y; i++)
            {
                for (int j = 0; j < _gridSize.x; j++)
                {
                    var item = _gridBase.GetItem(i, j);

                    if (!list.Contains(item) && item != null)
                    {
                        list.Add(item);
                        Debug.Log(item.name);
                    }
                }
            }

            return list;
        }

        public void RemoveLastInventoryItem()
        {
            if (_inventoryItems == null)
                return;

            var items = _inventoryItems.GetInventoryItems();
            items.RemoveAt(items.Count - 1);
        }

        public void RemoveInventoryItems()
        {
            for (var i = gameObject.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(gameObject.transform.GetChild(i).gameObject);
            }
        }
    }
}