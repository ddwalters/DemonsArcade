using System;
using UnityEngine;

[Serializable]
public sealed class ItemInstance
{
    public ItemData Definition;
    public Vector2Int Position;
    public int Rotation;

    public ItemInstance(ItemData definition, Vector2Int position = default, int rotation = 0)
    {
        Definition = definition;
        Position = position;
        Rotation = rotation;
    }
}
