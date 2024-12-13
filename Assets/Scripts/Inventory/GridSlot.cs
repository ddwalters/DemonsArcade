using UnityEngine;

public class GridSlot : MonoBehaviour
{
    (int x, int y) gridSlot;

    public void SetGridSlot(int x, int y)
    {
        gridSlot.y = y;
        gridSlot.x = x;
    }
}
