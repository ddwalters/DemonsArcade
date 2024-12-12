using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Graphs;

public class Generator3D : MonoBehaviour 
{
    enum CellType
    {
        None,
        Room,
        Hallway,
        Doorway,
        Stairs,
        Stairway
    }

    class Room 
    {
        public BoundsInt bounds;

        public Room(Vector3Int location, Vector3Int size) {
            bounds = new BoundsInt(location, size);
        }

        public static bool Intersect(Room a, Room b) {
            return !((a.bounds.position.x >= (b.bounds.position.x + b.bounds.size.x)) || ((a.bounds.position.x + a.bounds.size.x) <= b.bounds.position.x)
                || (a.bounds.position.y >= (b.bounds.position.y + b.bounds.size.y)) || ((a.bounds.position.y + a.bounds.size.y) <= b.bounds.position.y)
                || (a.bounds.position.z >= (b.bounds.position.z + b.bounds.size.z)) || ((a.bounds.position.z + a.bounds.size.z) <= b.bounds.position.z));
        }
    }

    [SerializeField]
    GameObject parent;
    [SerializeField]
    Vector3Int size;
    [SerializeField]
    int roomCount;
    [SerializeField]
    Vector3Int roomMaxSize;
    [SerializeField]
    Vector3Int roomMinSize;
    [SerializeField]
    GameObject cubePrefab;
    [SerializeField]
    GameObject hallPrefab;
    [SerializeField]
    GameObject doorprefab;
    [SerializeField]
    GameObject stairprefab;
    [SerializeField]
    GameObject startprefab;
    [SerializeField]
    GameObject bossprefab;
    [SerializeField]
    Material redMaterial;
    [SerializeField]
    Material blueMaterial;
    [SerializeField]
    Material lightBlueMaterial;
    [SerializeField]
    Material greenMaterial;

    Hallway hallScript;
    Doorway doorScript;

    Grid3D<CellType> grid;
    public List<GameObject> roomList = new List<GameObject>();
    public List<GameObject> hallList = new List<GameObject>();
    public List<GameObject> doorList = new List<GameObject>();
    List<Vector3Int> doorPosList = new List<Vector3Int>();
    List<Room> rooms;
    Delaunay3D delaunay;
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
        grid = new Grid3D<CellType>(size, Vector3Int.zero);
        rooms = new List<Room>();

        Generate();
    }

    void Generate()
    {
        Random.InitState(seed);
        grid = new Grid3D<CellType>(size, Vector3Int.zero);
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
        while (isBoss == false)
        {
            Vector3Int location = new Vector3Int(
                Random.Range(0, size.x),
                size.y -1,
                Random.Range(0, size.z)
            );

            Vector3Int roomSize = new Vector3Int(3, 2, 3);

            bool add = true;
            Room newRoom = new Room(location, roomSize);
            Room buffer = new Room(location + new Vector3Int(-1, -1, -1), roomSize + new Vector3Int(2, 2, 2));

            foreach (var room in rooms)
            {
                if (Room.Intersect(room, buffer))
                {
                    add = false;
                    break;
                }
            }

            if (newRoom.bounds.xMin < 0 || newRoom.bounds.xMax >= size.x
                || newRoom.bounds.zMin < 0 || newRoom.bounds.zMax >= size.z)
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
            Vector3Int location = new Vector3Int(
                Random.Range(0, size.x),
                Random.Range(0, size.y),
                Random.Range(0, size.z)
            );

            Vector3Int roomSize;

            if (isStartRoom == false)
            {
                roomSize = new Vector3Int(3, 2, 3);
                thisRoom = i;
            }
            else
            {
                roomSize = new Vector3Int(
                Random.Range(roomMinSize.x, roomMaxSize.x + 1),
                Random.Range(roomMinSize.y, roomMaxSize.y + 1),
                Random.Range(roomMinSize.z, roomMaxSize.z + 1)
            );
            }

            bool add = true;
            Room newRoom = new Room(location, roomSize);
            Room buffer = new Room(location + new Vector3Int(-1, -1, -1), roomSize + new Vector3Int(2, 2, 2));

            foreach (var room in rooms) {
                if (Room.Intersect(room, buffer)) {
                    add = false;
                    break;
                }
            }

            if (newRoom.bounds.xMin < 0 || newRoom.bounds.xMax >= size.x
                || newRoom.bounds.yMin < 0 || newRoom.bounds.yMax >= size.y
                || newRoom.bounds.zMin < 0 || newRoom.bounds.zMax >= size.z) {
                add = false;
            }

            if (add)
            {
                if (thisRoom == i)
                {
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

    void Triangulate() {
        List<Vertex> vertices = new List<Vertex>();

        foreach (var room in rooms) {
            vertices.Add(new Vertex<Room>((Vector3)room.bounds.position + ((Vector3)room.bounds.size) / 2, room));
        }

        delaunay = Delaunay3D.Triangulate(vertices);
    }

    void CreateHallways() {
        List<Prim.Edge> edges = new List<Prim.Edge>();

        foreach (var edge in delaunay.Edges) {
            edges.Add(new Prim.Edge(edge.U, edge.V));
        }

        List<Prim.Edge> minimumSpanningTree = Prim.MinimumSpanningTree(edges, edges[0].U);

        selectedEdges = new HashSet<Prim.Edge>(minimumSpanningTree);
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

    void PathfindHallways() {
        DungeonPathfinder3D aStar = new DungeonPathfinder3D(size);

        foreach (var edge in selectedEdges) {
            var startRoom = (edge.U as Vertex<Room>).Item;
            var endRoom = (edge.V as Vertex<Room>).Item;

            var startPosf = startRoom.bounds.center;
            var endPosf = endRoom.bounds.center;
            var startPos = new Vector3Int((int)startPosf.x, (int)startRoom.bounds.min.y, (int)startPosf.z);
            var endPos = new Vector3Int((int)endPosf.x, (int)endRoom.bounds.min.y, (int)endPosf.z);

            var path = aStar.FindPath(startPos, endPos, (DungeonPathfinder3D.Node a, DungeonPathfinder3D.Node b) => {
                var pathCost = new DungeonPathfinder3D.PathCost();

                var delta = b.Position - a.Position;

                if (delta.y == 0) {
                    //flat hallway
                    pathCost.cost = Vector3Int.Distance(b.Position, endPos);    //heuristic

                    if (grid[b.Position] == CellType.Stairs || grid[b.Position] == CellType.Stairway) 
                    {
                        return pathCost;
                    } 
                    else if (grid[b.Position] == CellType.Room) 
                    {
                        pathCost.cost += 5;
                    } 
                    else if (grid[b.Position] == CellType.None) 
                    {
                        pathCost.cost += 1;
                    }
                    else if (grid[b.Position] == CellType.Doorway)
                    {
                        pathCost.cost += 1;
                    }

                    pathCost.traversable = true;
                } else {
                    //staircase
                    if ((grid[a.Position] != CellType.None && grid[a.Position] != CellType.Hallway)
                        || (grid[b.Position] != CellType.None && grid[b.Position] != CellType.Hallway)) return pathCost;

                    pathCost.cost = 100 + Vector3Int.Distance(b.Position, endPos);    //base cost + heuristic

                    int xDir = Mathf.Clamp(delta.x, -1, 1);
                    int zDir = Mathf.Clamp(delta.z, -1, 1);
                    Vector3Int verticalOffset = new Vector3Int(0, delta.y, 0);
                    Vector3Int horizontalOffset = new Vector3Int(xDir, 0, zDir);

                    if (!grid.InBounds(a.Position + verticalOffset)
                        || !grid.InBounds(a.Position + horizontalOffset)
                        || !grid.InBounds(a.Position + verticalOffset + horizontalOffset)) {
                        return pathCost;
                    }

                    if (grid[a.Position + horizontalOffset] != CellType.None
                        || grid[a.Position + horizontalOffset * 2] != CellType.None
                        || grid[a.Position + verticalOffset + horizontalOffset] != CellType.None
                        || grid[a.Position + verticalOffset + horizontalOffset * 2] != CellType.None) {
                        return pathCost;
                    }

                    pathCost.traversable = true;
                    pathCost.isStairs = true;
                }

                return pathCost;
            });

            if (path != null) {
                int x = 0;
                for (int i = 0; i < path.Count; i++) {
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

                    if (i > 0) {
                        var prev = path[i - 1];

                        var delta = current - prev;

                        if (delta.y != 0) {
                            int xDir = Mathf.Clamp(delta.x, -1, 1);
                            int zDir = Mathf.Clamp(delta.z, -1, 1);
                            int yDir = Mathf.Clamp(delta.y, -1, 1);
                            Vector3Int verticalOffset = new Vector3Int(0, delta.y, 0);
                            Vector3Int horizontalOffset = new Vector3Int(xDir, 0, zDir);

                            grid[prev + horizontalOffset] = CellType.Stairs;
                            grid[prev + horizontalOffset * 2] = CellType.Stairway;
                            grid[prev + verticalOffset + horizontalOffset] = CellType.Stairway;
                            grid[prev + verticalOffset + horizontalOffset * 2] = CellType.Stairs;

                            // Mark entry point as Doorway
                            if (grid.InBounds(prev) && grid[prev] == CellType.Hallway)
                            {
                                grid[prev] = CellType.Doorway;
                            }

                            // Mark exit point as Doorway
                            Vector3Int exitCell = prev + verticalOffset + horizontalOffset * 3;
                            if (grid.InBounds(exitCell) && grid[exitCell] == CellType.Hallway)
                            {
                                grid[exitCell] = CellType.Doorway;
                            }

                            Vector3Int location = prev + horizontalOffset;
                            if (xDir == 0 && yDir == 1 && zDir == -1)
                            {
                                PlaceStairs(new Vector3Int(location.x, location.y, location.z -1), new Vector3Int(0, 0, 0));
                            }
                            else if (xDir == 0 && yDir == -1 && zDir == -1)
                            {
                                PlaceStairs(new Vector3Int(location.x +1, location.y -1, location.z +1), new Vector3Int(0, 180, 0));
                            }
                            if (xDir == 0 && yDir == 1 && zDir == 1)
                            {
                                PlaceStairs(new Vector3Int(location.x +1, location.y, location.z +2), new Vector3Int(0, 180, 0));
                            }
                            else if (xDir == 0 && yDir == -1 && zDir == 1)
                            {
                                PlaceStairs(new Vector3Int(location.x, location.y -1, location.z), new Vector3Int(0, 0, 0));
                            }
                            if (xDir == -1 && yDir == 1 && zDir == 0)
                            {
                                PlaceStairs(new Vector3Int(location.x -1, location.y, location.z +1), new Vector3Int(0, 90, 0));
                            }
                            else if (xDir == -1 && yDir == -1 && zDir == 0)
                            {
                                PlaceStairs(new Vector3Int(location.x +1, location.y -1, location.z), new Vector3Int(0, -90, 0));
                            }
                            if (xDir == 1 && yDir == 1 && zDir == 0)
                            {
                                PlaceStairs(new Vector3Int(location.x +2, location.y, location.z), new Vector3Int(0, -90, 0));
                            }
                            else if (xDir == 1 && yDir == -1 && zDir == 0)
                            {
                                PlaceStairs(new Vector3Int(location.x, location.y -1, location.z + 1), new Vector3Int(0, 90, 0));
                            }
                        }

                        Debug.DrawLine(prev + new Vector3(0.5f, 0.5f, 0.5f), current + new Vector3(0.5f, 0.5f, 0.5f), Color.blue, 100, false);
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
        int p = 0;
        foreach (GameObject go in doorList)
        {
            var pos = doorPosList[p];
            DoorwayCheck(go,pos);
            p++;
        }
        for (int i = 1; i < hallList.Count; i += 3) // Process every 3rd hallway starting from index 1
        {
            GameObject hall = hallList[i];
            Hallway hallScript = hall.GetComponent<Hallway>();

            hallScript.placeTorches();
        }

        for (int i = 1; i < doorList.Count; i += 2) // Process every 2nd doorway starting from index 1
        {
            GameObject door = doorList[i];
            Doorway doorScript = door.GetComponent<Doorway>();

            doorScript.placeTorches();
        }

    }

    void PlaceRoom(Vector3Int location, Vector3Int size)
    {
        GameObject go = Instantiate(cubePrefab, new Vector3(location.x, location.y - 0.5f, location.z), Quaternion.identity);

        roomList.Add(go);
        go.GetComponent<Transform>().localScale = new Vector3(size.x, size.y, size.z);
        go.GetComponent<MeshRenderer>().material = redMaterial;
    }

    void PlaceStartRoom(Vector3Int location, Vector3Int size)
    {
        GameObject go = Instantiate(startprefab, new Vector3(location.x, location.y - 0.5f, location.z), Quaternion.identity);

        roomList.Add(go);
        go.GetComponent<Transform>().localScale = new Vector3(size.x, size.y, size.z);
        go.GetComponent<MeshRenderer>().material = redMaterial;

    }

    void PlaceBossRoom(Vector3Int location, Vector3Int size)
    {
        GameObject go = Instantiate(bossprefab, new Vector3(location.x, location.y - 0.5f, location.z), Quaternion.identity);

        roomList.Add(go);
        go.GetComponent<Transform>().localScale = new Vector3(size.x, size.y, size.z);
        go.GetComponent<MeshRenderer>().material = redMaterial;
    }

    void PlaceHallway(Vector3Int location)
    {
        // Instantiate the selected prefab
        GameObject go = Instantiate(hallPrefab, new Vector3(location.x, location.y, location.z), Quaternion.identity, parent.transform);
        hallList.Add(go);
        go.transform.localScale = new Vector3(1, 1, 1);
        go.GetComponent<MeshRenderer>().material = blueMaterial;

        var plusX = location + new Vector3Int(1, 0, 0);
        var minX = location + new Vector3Int(-1, 0, 0);
        var plusZ = location + new Vector3Int(0, 0, 1);
        var minZ = location + new Vector3Int(0, 0, -1);

        Hallway hallScript = go.GetComponent<Hallway>();

        var dir = plusX;
        if (grid.InBounds(dir) && (grid[dir] == CellType.Hallway || grid[dir] == CellType.Doorway))
        {
            hallScript.destroyPlusX();
        }
        dir = minX;
        if (grid.InBounds(dir) && (grid[dir] == CellType.Hallway || grid[dir] == CellType.Doorway))
        {
            hallScript.destroyMinX();
        }
        dir = plusZ;
        if (grid.InBounds(dir) && (grid[dir] == CellType.Hallway || grid[dir] == CellType.Doorway))
        {
            hallScript.destroyPlusZ();
        }
        dir = minZ;
        if (grid.InBounds(dir) && (grid[dir] == CellType.Hallway || grid[dir] == CellType.Doorway))
        {
            hallScript.destroyMinZ();
        }

    }
    
     void PlaceDoorway(Vector3Int location)
    {
        GameObject go = Instantiate(doorprefab, new Vector3(location.x, location.y, location.z), Quaternion.identity, parent.transform);
        doorList.Add(go);
        doorPosList.Add(location);

        go.GetComponent<Transform>().localScale = new Vector3(1, 1, 1);
        go.GetComponent<MeshRenderer>().material = blueMaterial;

        var plusX = location + new Vector3Int(1, 0, 0);
        var minX = location + new Vector3Int(-1, 0, 0);
        var plusZ = location + new Vector3Int(0, 0, 1);
        var minZ = location + new Vector3Int(0, 0, -1);

        Doorway doorScript = go.GetComponent<Doorway>();

        var dir = plusX;
        if (grid.InBounds(dir) && (grid[dir] == CellType.Hallway || grid[dir] == CellType.Doorway || grid[dir] == CellType.Stairs))
        {
            doorScript.destroyPlusX();
        }
        else if (grid.InBounds(dir) && grid[dir] == CellType.Room)
        {
            if (grid[dir + new Vector3Int(0, -1, 0)] == CellType.Room)
            {
                //Debug.Log("Room Below");
            }
            else
            {
                //Debug.Log(dir + new Vector3Int(0, -1, 0));
                doorScript.destroyPlusX();
            }
        }
        dir = minX;
        if (grid.InBounds(dir) && (grid[dir] == CellType.Hallway || grid[dir] == CellType.Doorway || grid[dir] == CellType.Stairs))
        {
            doorScript.destroyMinX();
        }
        else if (grid.InBounds(dir) && grid[dir] == CellType.Room)
        {
            if (grid[dir + new Vector3Int(0, -1, 0)] == CellType.Room)
            {
                //Debug.Log("Room Below");
            }
            else
            {
                //Debug.Log(dir + new Vector3Int(0, -1, 0));
                doorScript.destroyMinX();
            }
        }
        dir = plusZ;
        if (grid.InBounds(dir) && (grid[dir] == CellType.Hallway || grid[dir] == CellType.Doorway || grid[dir] == CellType.Stairs))
        {
            doorScript.destroyPlusZ();
        }
        else if (grid.InBounds(dir) && grid[dir] == CellType.Room)
        {
            if (grid[dir + new Vector3Int(0, -1, 0)] == CellType.Room)
            {
                //Debug.Log("Room Below");
            }
            else
            {
                //Debug.Log(dir + new Vector3Int(0, -1, 0));
                doorScript.destroyPlusZ();
            }
        }
        dir = minZ;
        if (grid.InBounds(dir) && (grid[dir] == CellType.Hallway || grid[dir] == CellType.Doorway || grid[dir] == CellType.Stairs))
        {
            doorScript.destroyMinZ();
        }
        else if (grid.InBounds(dir) && grid[dir] == CellType.Room)
        {
            if (grid[dir + new Vector3Int(0, -1, 0)] == CellType.Room)
            {
                //Debug.Log("Room Below");
            }
            else
            {
                //Debug.Log(dir + new Vector3Int(0, -1, 0));
                doorScript.destroyMinZ();
            }
        }
    }

    public void DoorwayCheck(GameObject go, Vector3Int location)
    {
        var plusX = location + new Vector3Int(1, 0, 0);
        var minX = location + new Vector3Int(-1, 0, 0);
        var plusZ = location + new Vector3Int(0, 0, 1);
        var minZ = location + new Vector3Int(0, 0, -1);

        Doorway doorScript = go.GetComponent<Doorway>();

        var dir = plusX;
        if (grid.InBounds(dir) && (grid[dir] == CellType.Hallway || grid[dir] == CellType.Doorway || grid[dir] == CellType.Stairs))
        {
            doorScript.destroyPlusX();
        }
        else if (grid.InBounds(dir) && grid[dir] == CellType.Room)
        {
            if (grid[dir + new Vector3Int(0, -1, 0)] == CellType.Room)
            {
                //Debug.Log("Room Below");
            }
            else
            {
                //Debug.Log(dir + new Vector3Int(0, -1, 0));
                doorScript.destroyPlusX();
            }
        }
        dir = minX;
        if (grid.InBounds(dir) && (grid[dir] == CellType.Hallway || grid[dir] == CellType.Doorway || grid[dir] == CellType.Stairs))
        {
            doorScript.destroyMinX();
        }
        else if (grid.InBounds(dir) && grid[dir] == CellType.Room)
        {
            if (grid[dir + new Vector3Int(0, -1, 0)] == CellType.Room)
            {
                //Debug.Log("Room Below");
            }
            else
            {
                //Debug.Log(dir + new Vector3Int(0, -1, 0));
                doorScript.destroyMinX();
            }
        }
        dir = plusZ;
        if (grid.InBounds(dir) && (grid[dir] == CellType.Hallway || grid[dir] == CellType.Doorway || grid[dir] == CellType.Stairs))
        {
            doorScript.destroyPlusZ();
        }
        else if (grid.InBounds(dir) && grid[dir] == CellType.Room)
        {
            if (grid[dir + new Vector3Int(0, -1, 0)] == CellType.Room)
            {
                //Debug.Log("Room Below");
            }
            else
            {
                //Debug.Log(dir + new Vector3Int(0, -1, 0));
                doorScript.destroyPlusZ();
            }
        }
        dir = minZ;
        if (grid.InBounds(dir) && (grid[dir] == CellType.Hallway || grid[dir] == CellType.Doorway || grid[dir] == CellType.Stairs))
        {
            doorScript.destroyMinZ();
        }
        else if (grid.InBounds(dir) && grid[dir] == CellType.Room)
        {
            if (grid[dir + new Vector3Int(0, -1, 0)] == CellType.Room)
            {
                //Debug.Log("Room Below");
            }
            else
            {
                //Debug.Log(dir + new Vector3Int(0, -1, 0));
                doorScript.destroyMinZ();
            }
        }
    }

    void PlaceStairs(Vector3Int location, Vector3Int rotation)
    {
        // Instantiate the stair prefab
        GameObject go = Instantiate(stairprefab, new Vector3(location.x, location.y, location.z), Quaternion.Euler(rotation), parent.transform);

        // Apply the green material
        go.GetComponent<MeshRenderer>().material = greenMaterial;
    }

}
