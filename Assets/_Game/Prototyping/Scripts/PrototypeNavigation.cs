using Pathfinding;
using UnityEngine;

// Scan after the scene's colliders exist, before patrols request their first route.
[RequireComponent(typeof(AstarPath))]
[DefaultExecutionOrder(-10010)]
public class PrototypeNavigation : MonoBehaviour
{
    private AstarPath astar;

    private void Awake()
    {
        astar = GetComponent<AstarPath>();
        // This prototype creates its graph at runtime and does not use a baked startup archive.
        astar.data.SetData(null);
        astar.scanOnStartup = false;
    }

    private void Start()
    {
        astar.logPathResults = PathLog.OnlyErrors;
        GridGraph grid = astar.data.AddGraph(typeof(GridGraph)) as GridGraph;
        grid.center = Vector3.zero;
        grid.SetDimensions(120, 100, 0.3f);
        grid.maxClimb = 0.3f;
        grid.maxSlope = 50f;
        grid.erodeIterations = 1;
        // Only floors supply node heights; a lintel must not hide the floor below it.
        grid.collision.heightMask = LayerMask.GetMask("Ground");
        grid.collision.mask = LayerMask.GetMask("Obstacle");
        grid.collision.diameter = 2.2f;
        grid.collision.height = 1.8f;
        grid.collision.collisionOffset = 0.2f;
        grid.collision.fromHeight = 8f;
        Physics.SyncTransforms();
        astar.Scan();
    }
}
