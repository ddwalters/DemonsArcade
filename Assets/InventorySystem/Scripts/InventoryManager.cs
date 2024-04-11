using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static InventoryType;

public class InventoryManager : MonoBehaviour
{
    public InventoryTypeCollection inventoryTypeCollection;

    private Dictionary<int, GameObject> gridsInformation;

    private void Awake()
    {
        if (gridsInformation == null)
        {
            var statsData = FindAnyObjectByType<PlayerStatsManager>().GetPlayerStats();
            var newPlayerGrid = inventoryTypeCollection.AllPrefabs.FirstOrDefault(x => x.invType == statsData.inventoryType).prefab;
        
            gridsInformation = new Dictionary<int, GameObject>
            {
                { 0, newPlayerGrid }
            };
        }
    }

    /// <summary>
    /// Adds item to manager.
    /// </summary>
    /// <returns>The newly created id.</returns>
    /// <param name="grid">Grid to be added to the manager.</param>
    public int AddGrid(InvType inv)
    {
        var newGrid = inventoryTypeCollection.AllPrefabs.FirstOrDefault(x => x.invType == inv).prefab;
        gridsInformation.Add(gridsInformation.Count, newGrid);

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

    public GameObject GetGrid(int gridId)
    {
        return gridsInformation[gridId];
    }

    public void SetGrid(int gridId, GameObject updater)
    {
        gridsInformation[gridId] = updater;
    }
}