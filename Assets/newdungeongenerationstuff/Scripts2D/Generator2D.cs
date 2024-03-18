using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Graphs;

public class Generator2D : MonoBehaviour
{
    enum CellType
    {
        None,
        Room,
        Hallway,
        Doorway
    }

    class Room
    {
        public RectInt bounds;

        public Room(Vector2Int location, Vector2Int size)
        {
            bounds = new RectInt(location, size);
        }

        public static bool Intersect(Room a, Room b)
        {
            return !((a.bounds.position.x >= (b.bounds.position.x + b.bounds.size.x)) || ((a.bounds.position.x + a.bounds.size.x) <= b.bounds.position.x)
                || (a.bounds.position.y >= (b.bounds.position.y + b.bounds.size.y)) || ((a.bounds.position.y + a.bounds.size.y) <= b.bounds.position.y));
        }
    }

    [SerializeField]
    GameObject parent;
    [SerializeField]
    Vector2Int size;
    [SerializeField]
    int roomCount;
    [SerializeField]
    Vector2Int roomMaxSize;
    [SerializeField]
    Vector2Int roomMinSize;
    [SerializeField]
    GameObject cubePrefab;
    [SerializeField]
    GameObject hallprefab;
    [SerializeField]
    GameObject doorprefab;
    [SerializeField]
    GameObject startprefab;
    [SerializeField]
    GameObject bossprefab;
    [SerializeField]
    Material redMaterial;
    [SerializeField]
    Material blueMaterial;

    Grid2D<CellType> grid;
    public List<GameObject> roomlist = new List<GameObject>();
    public List<GameObject> halllist = new List<GameObject>();
    List<Room> rooms;
    Delaunay2D delaunay;
    HashSet<Prim.Edge> selectedEdges;

    [SerializeField]
    public int seed; // Seed for the random number generator

    void Start()
    {
        if (seed == 0)
        {
            // Generate a random seed with 8 digits
            System.Random systemRandom = new System.Random();
            seed = systemRandom.Next(10000000, 99999999);
        }

        Generate();
    }

    void Generate()
    {
        Random.InitState(seed);
        grid = new Grid2D<CellType>(size, Vector2Int.zero);
        rooms = new List<Room>();

        PlaceRooms();
        PlaceBoss();
        Triangulate();
        CreateHallways();
        PathfindHallways();
    }

    int thisRoom = 0;

    void PlaceBoss()
    {
        bool isBoss = false;
        while(isBoss == false)
        {
            Vector2Int location = new Vector2Int(
                Random.Range(0, size.x),
                Random.Range(0, size.y)
            );

            Vector2Int roomSize = new Vector2Int(3, 3);

            bool add = true;
            Room newRoom = new Room(location, roomSize);
            Room buffer = new Room(location + new Vector2Int(-1, -1), roomSize + new Vector2Int(2, 2));

            foreach (var room in rooms)
            {
                if (Room.Intersect(room, buffer))
                {
                    add = false;
                    break;
                }
            }

            if (newRoom.bounds.xMin < 0 || newRoom.bounds.xMax >= size.x
                || newRoom.bounds.yMin < 0 || newRoom.bounds.yMax >= size.y)
            {
                add = false;
            }

            if (add)
            {
                isBoss = true;
                rooms.Add(newRoom);
                PlaceBossRoom(newRoom.bounds.position, newRoom.bounds.size);

                foreach (var pos in newRoom.bounds.allPositionsWithin)
                {
                    grid[pos] = CellType.Room;
                }
            }
        }
    }

    void PlaceRooms()
    {
        bool isStartRoom = false;
        for (int i = 0; i < roomCount; i++)
        {
            Vector2Int location = new Vector2Int(
                Random.Range(0, size.x),
                Random.Range(0, size.y)
            );

            Vector2Int roomSize;

            if (isStartRoom == false)
            {
                Debug.Log("start room init");
                roomSize = new Vector2Int(3,3);
                thisRoom = i;
            }
            else
            {
                roomSize = new Vector2Int(
                Random.Range(roomMinSize.x, roomMaxSize.x + 1),
                Random.Range(roomMinSize.y, roomMaxSize.y + 1)
            );
            }

            bool add = true;
            Room newRoom = new Room(location, roomSize);
            Room buffer = new Room(location + new Vector2Int(-1, -1), roomSize + new Vector2Int(2, 2));

            foreach (var room in rooms)
            {
                if (Room.Intersect(room, buffer))
                {
                    add = false;
                    break;
                }
            }

            if (newRoom.bounds.xMin < 0 || newRoom.bounds.xMax >= size.x
                || newRoom.bounds.yMin < 0 || newRoom.bounds.yMax >= size.y)
            {
                add = false;
            }

            if (add)
            {
                if (thisRoom == i)
                {
                    Debug.Log("StartRoom at: " + newRoom.bounds.position);
                    rooms.Add(newRoom);
                    isStartRoom = true;
                    PlaceStartRoom(newRoom.bounds.position, newRoom.bounds.size);
                }
                else
                {
                    rooms.Add(newRoom);
                    PlaceRoom(newRoom.bounds.position, newRoom.bounds.size);
                }

                foreach (var pos in newRoom.bounds.allPositionsWithin)
                {
                    grid[pos] = CellType.Room;
                }
            }
        }
    }

    void Triangulate()
    {
        List<Vertex> vertices = new List<Vertex>();

        foreach (var room in rooms)
        {
            vertices.Add(new Vertex<Room>((Vector2)room.bounds.position + ((Vector2)room.bounds.size) / 2, room));
        }

        delaunay = Delaunay2D.Triangulate(vertices);
    }

    void CreateHallways()
    {
        List<Prim.Edge> edges = new List<Prim.Edge>();

        foreach (var edge in delaunay.Edges)
        {
            edges.Add(new Prim.Edge(edge.U, edge.V));
        }

        List<Prim.Edge> mst = Prim.MinimumSpanningTree(edges, edges[0].U);

        selectedEdges = new HashSet<Prim.Edge>(mst);
        var remainingEdges = new HashSet<Prim.Edge>(edges);
        remainingEdges.ExceptWith(selectedEdges);

        foreach (var edge in remainingEdges)
        {
            if (Random.value < 0.125f)
            {
                selectedEdges.Add(edge);
            }
        }
    }

    void PathfindHallways()
    {
        DungeonPathfinder2D aStar = new DungeonPathfinder2D(size);

        foreach (var edge in selectedEdges)
        {
            var startRoom = (edge.U as Vertex<Room>).Item;
            var endRoom = (edge.V as Vertex<Room>).Item;

            var startPosf = startRoom.bounds.center;
            var endPosf = endRoom.bounds.center;
            var startPos = new Vector2Int((int)startPosf.x, (int)startPosf.y);
            var endPos = new Vector2Int((int)endPosf.x, (int)endPosf.y);

            var path = aStar.FindPath(startPos, endPos, (DungeonPathfinder2D.Node a, DungeonPathfinder2D.Node b) => {
                var pathCost = new DungeonPathfinder2D.PathCost();

                pathCost.cost = Vector2Int.Distance(b.Position, endPos);    //heuristic

                if (grid[b.Position] == CellType.Room)
                {
                    pathCost.cost += 10;
                }
                else if (grid[b.Position] == CellType.None)
                {
                    pathCost.cost += 5;
                }
                else if (grid[b.Position] == CellType.Hallway)
                {
                    pathCost.cost += 1;
                }
                else if (grid[b.Position] == CellType.Doorway)
                {
                    pathCost.cost += 1;
                }
                pathCost.traversable = true;

                return pathCost;
            });

            if (path != null)
            {
                int x = 0;

                for (int i = 0; i < path.Count; i++)
                {
                    int y = i + 1;
                    int z = i - 1;
                    var current = path[i];

                    if (grid[current] == CellType.None)
                    {
                        if (grid[path[z]] == CellType.Room || grid[path[y]] == CellType.Room)
                        {
                            x++;
                            grid[current] = CellType.Doorway;
                        }
                        else
                        {
                            grid[current] = CellType.Hallway;
                        }
                    }

                    if (i > 0)
                    {
                        var prev = path[i - 1];

                        var delta = current - prev;
                    }
                }

                foreach (var pos in path)
                {
                    if (grid[pos] == CellType.Hallway)
                    {
                        PlaceHallway(pos);
                    }
                    else if (grid[pos] == CellType.Doorway)
                    {
                        PlaceDoorway(pos);
                    }
                }
            }
        }
    }

    void PlaceRoom(Vector2Int location, Vector2Int size)
    {

        GameObject go = Instantiate(cubePrefab, new Vector3(location.x, 0, location.y), Quaternion.identity);

        roomlist.Add(go);
        go.GetComponent<Transform>().localScale = new Vector3(size.x, 1, size.y);
        go.GetComponent<MeshRenderer>().material = redMaterial;
    }

    void PlaceStartRoom(Vector2Int location, Vector2Int size)
    {
        Debug.Log("Placed start room at: " + location);

        GameObject go = Instantiate(startprefab, new Vector3(location.x, 0, location.y), Quaternion.identity);

        roomlist.Add(go);
        go.GetComponent<Transform>().localScale = new Vector3(size.x, 1, size.y);
        go.GetComponent<MeshRenderer>().material = redMaterial;
    }

    void PlaceBossRoom(Vector2Int location, Vector2Int size)
    {
        Debug.Log("Placed boss room at: " + location);

        GameObject go = Instantiate(bossprefab, new Vector3(location.x, 0, location.y), Quaternion.identity);

        roomlist.Add(go);
        go.GetComponent<Transform>().localScale = new Vector3(size.x, 1, size.y);
        go.GetComponent<MeshRenderer>().material = redMaterial;
    }

    void PlaceHallway(Vector2Int location)
    {

        GameObject go = Instantiate(hallprefab, new Vector3(location.x, 0, location.y), Quaternion.identity, parent.transform);
        halllist.Add(go);
        go.GetComponent<Transform>().localScale = new Vector3(1, 1, 1);
        go.GetComponent<MeshRenderer>().material = blueMaterial;
    }

    void PlaceDoorway(Vector2Int location)
    {
        GameObject go = Instantiate(doorprefab, new Vector3(location.x, 0, location.y), Quaternion.identity, parent.transform);
        halllist.Add(go);
        go.GetComponent<Transform>().localScale = new Vector3(1, 1, 1);
        go.GetComponent<MeshRenderer>().material = blueMaterial;
    }
}