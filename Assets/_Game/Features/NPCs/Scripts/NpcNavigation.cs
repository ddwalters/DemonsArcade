using Pathfinding;
using UnityEngine;

// Scan after the scene's colliders exist, before patrols request their first route.
[RequireComponent(typeof(AstarPath))]
[DefaultExecutionOrder(-10010)]
public class NpcNavigation : MonoBehaviour
{
    [SerializeField] private Vector3 graphCenter = Vector3.zero;
    [SerializeField] private Vector2Int graphDimensions = new(120, 100);
    [SerializeField, Min(0.01f)] private float nodeSize = 0.3f;
    [SerializeField, Min(0f)] private float maxClimb = 0.3f;
    [SerializeField, Range(0f, 90f)] private float maxSlope = 50f;
    [SerializeField, Min(0)] private int erosionIterations = 1;
    private AstarPath astar;

    private void Awake()
    {
        astar = GetComponent<AstarPath>();
        // This component creates its graph at runtime and does not use a baked startup archive.
        astar.data.SetData(null);
        astar.scanOnStartup = false;
    }

    private void Start()
    {
        astar.logPathResults = PathLog.OnlyErrors;
        GridGraph grid = astar.data.AddGraph(typeof(GridGraph)) as GridGraph;
        grid.center = graphCenter;
        grid.SetDimensions(graphDimensions.x, graphDimensions.y, nodeSize);
        grid.maxClimb = maxClimb;
        grid.maxSlope = maxSlope;
        grid.erodeIterations = erosionIterations;
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
