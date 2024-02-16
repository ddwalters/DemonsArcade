using UnityEngine;

namespace Inventory
{
    [CreateAssetMenu(fileName = "InventorySystem", menuName = "InventorySystem/ItemData")]
    public class ItemDataScriptableObject : ScriptableObject, IGetItemData
    {
        public IInventoryItemData ItemData => new ItemData(_itemData);

        [SerializeField] private ItemData _itemData;
    }
}