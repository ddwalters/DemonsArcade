using System.Collections.Generic;
using UnityEngine;

public interface IGridReader
{
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