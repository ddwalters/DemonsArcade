using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCurser : MonoBehaviour
{
    private IGridWriter gridWriter;

    private void Awake()
    {
        gridWriter = FindAnyObjectByType<GridManager>();
    }
}