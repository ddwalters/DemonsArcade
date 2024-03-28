using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    private Dictionary<int, InventoryGrid> gridsInformation;

    private void Start()
    {
        if (gridsInformation == null)
            gridsInformation = new Dictionary<int, InventoryGrid>();
    }

    /// <summary>
    /// Adds item to manager.
    /// </summary>
    /// <returns>The newly created id.</returns>
    /// <param name="grid">Grid to be added to the manager.</param>
    public int AddGrid(InventoryGrid grid)
    {
        gridsInformation.Add(gridsInformation.Count - 1, grid);

        return gridsInformation.Count - 1;
    }

    /// <summary>
    /// Removes grid from manager.
    /// </summary>
    /// <returns>The newly created id.</returns>
    /// <param name="grid">Id of the grid being removed.</param>
    public void RemoveGrid(int gridId)
    {
        gridsInformation.Remove(gridId);
    }

    public InventoryGrid GetGrid(int gridId)
    {
        return gridsInformation[gridId];
    }
}