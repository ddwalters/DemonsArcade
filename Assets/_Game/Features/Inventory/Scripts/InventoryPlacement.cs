using System.Collections.Generic;
using UnityEngine;

// Grid coordinates always describe the top-left cell; rotation changes only the footprint.
public static class InventoryPlacement
{
    public static Vector2Int Size(ItemData definition, int rotation) => (rotation & 1) == 0
        ? new Vector2Int(definition.size.width, definition.size.height)
        : new Vector2Int(definition.size.height, definition.size.width);

    public static IEnumerable<Vector2Int> Cells(Vector2Int position, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                yield return position + new Vector2Int(x, y);
    }

    public static bool Fits(Vector2Int position, Vector2Int size, Vector2Int gridSize) =>
        size.x > 0 && size.y > 0 && position.x >= 0 && position.y >= 0 &&
        position.x + size.x <= gridSize.x && position.y + size.y <= gridSize.y;

    public static bool Overlaps(Vector2Int a, Vector2Int aSize, Vector2Int b, Vector2Int bSize) =>
        new RectInt(a, aSize).Overlaps(new RectInt(b, bSize));
}

// Values match the existing serialized grid IDs.
public enum EquipmentSlot
{
    Helmet = 1,
    Chestpiece = 2,
    Leggings = 3,
    Boots = 4,
    Necklace = 5,
    RightHand = 6,
    LeftHand = 7,
    RingLeft = 8,
    RingRight = 9
}
