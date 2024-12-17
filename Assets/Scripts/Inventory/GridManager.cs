using System.Collections.Generic;
using UnityEngine;

public interface IGridReader
{
    GameObject GetSlotPrefab();

    int GetNewGridId();

    List<GridItem> GetGridItems(int id);
}

public interface IGridWriter
{
    void SetGridItems(int id, List<GridItem> items);
}

public class GridManager : MonoBehaviour, IGridWriter, IGridReader
{
    private IDictionary<int, List<GridItem>> _grids = new Dictionary<int, List<GridItem>>();

    [SerializeField] GameObject _slotPrefab;
    public GameObject GetSlotPrefab() => _slotPrefab;

    public int GetNewGridId()
    {
        _grids.Add(_grids.Count, new List<GridItem>());

        return _grids.Count - 1;
    }

    public List<GridItem> GetGridItems(int id)
    {
        return _grids[id];
    }

    public void SetGridItems(int id, List<GridItem> items)
    {
        // Will need to change info if moving grids
        _grids[id] = items;
    }
}