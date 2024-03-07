using UnityEngine;

namespace Inventory
{
    public interface IInventoryItemData
    {
        public ItemStatsData ItemStats { get; }

        public int Width { get; }

        public int Height { get; }

        public bool[] OccupiedGrid { get; }

        public bool[] OccupiedGridRotated { get; }

        public Sprite Sprite { get; }

        public Orientation Orientation { get; }

        public void Rotate();
    }
}