using System.Collections.Generic;
using UnityEngine;

public interface IGridReader
{
    GameObject GetSlotPrefab();

    int GetNewGridId();

    List<((int x, int y), int itemData)> GetGridItems(int id);
}

public interface IGridWriter
{
    void SetGridItems(int id, List<((int x, int y), int itemData)> items);
}

public class GridManager : MonoBehaviour, IGridWriter, IGridReader
{
    private IDictionary<int, List<((int x, int y), int itemData)>> _grids = new Dictionary<int, List<((int x, int y), int itemData)>>();

    [SerializeField] GameObject _slotPrefab;
    public GameObject GetSlotPrefab() => _slotPrefab;

    public int GetNewGridId()
    {
        _grids.Add(_grids.Count, new List<((int x, int y), int itemData)>());

        return _grids.Count - 1;
    }

    public List<((int x, int y), int itemData)> GetGridItems(int id)
    {
        return _grids[id];
    }

    public void SetGridItems(int id, List<((int x, int y), int itemData)> items)
    {
        // Will need to change info if moving grids
        _grids[id] = items;
    }
}