using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class BakeRuntime : MonoBehaviour
{
    public NavMeshSurface navMesh;

    public void Bake()
    {
        navMesh.BuildNavMesh();
    }
}
