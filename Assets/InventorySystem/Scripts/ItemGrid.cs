using System.Collections.Generic;
using UnityEngine;

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

        [SerializeField] ItemToolTip _itemToolTip;
        PlayerStatsManager _playerStatsManager;

        private bool storeTypeInventory;

        private void Awake()
        {
            _inventoryItems = GetComponent<IGetInventoryItems>();
        }

        private void Start()
        {
            _playerStatsManager = GameObject.Find("PlayerCharacter").GetComponent<PlayerStatsManager>();
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
                itemObject.Initialize(item.ItemPreset.ItemData, item.ItemPreset.StatsData, _itemToolTip);

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
            itemObject.Initialize(newItem.ItemPreset.ItemData, newItem.ItemPreset.StatsData, _itemToolTip);

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
                itemObject.Initialize(item.ItemPreset.ItemData, item.ItemPreset.StatsData, _itemToolTip);

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
        public InventoryItem PurchaseItem(int x, int y) 
        {
            var item = _gridBase.GrabItem(x, y);
            var itemStats = item.GetItemStatsData();
            if (_playerStatsManager.GetCurrentGold() < itemStats.GetCost())
            {
                Debug.Log("Not enough money");
                return null;
            }

            // checking fit needs to be for the player grid size x and y
            for (int i = 0; i < _gridSize.y; i++)
            {
                for (int j = 0; j < _gridSize.x; j++)
                {
                    if (CheckItemFits(item, j, i))
                    {
                        if (_playerStatsManager.RemoveGold(itemStats.GetCost()))
                            return item;
                        else
                            return null; // gold was not removed
                    }
                }
            }

            return null;
        } 
        public bool CheckItemFits(InventoryItem item, int x, int y) => _gridBase.CheckItemFits(item, x, y);

        public void SellItem(InventoryItem item, int x, int y)
        {
            var itemStats = item.GetItemStatsData();
            _playerStatsManager.GainGold(itemStats.GetCost());

            PlaceItem(item, x, y);
        }

        public void PlaceItem(InventoryItem item, int x, int y)
        {
            item.RectTransform.SetParent(_rectTransform);
            item.Position = new Vector2Int(x, y);
            _gridBase.PlaceItem(item, x, y);
            var position = new Vector2(x * _gridSquareSize.x, -y * _gridSquareSize.y);
            item.RectTransform.localPosition = position;
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