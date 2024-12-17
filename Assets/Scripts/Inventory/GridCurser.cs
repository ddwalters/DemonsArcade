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

    //Interaction controller has OnGrab for ui control.
    //This class has what grabbing does I.E.
    //Raycast
    // If slot nothing
    // If !item pick up and store on this object
    // once release, if over another grid slot-- place it
    // if not return it to it's previous position
}