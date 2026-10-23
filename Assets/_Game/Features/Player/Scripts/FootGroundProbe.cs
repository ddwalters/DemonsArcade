using UnityEngine;

public sealed class FootGroundProbe
{
    private const float ProbeMargin = 0.1f;
    private const float MinimumWalkableNormalY = 0.5f;
    private const int EdgeSearchIterations = 4;

    private readonly LayerMask groundMask;

    public FootGroundProbe(LayerMask groundMask)
    {
        this.groundMask = groundMask;
    }

    public bool TryGetGround(Vector3 point, float floorY, float footHeight, float reach,
        out float groundY, out Vector3 normal)
    {
        float swingAllowance = Mathf.Max(0f, footHeight);
        Vector3 origin = new Vector3(point.x, floorY + reach + swingAllowance + ProbeMargin, point.z);
        float distance = reach * 2f + swingAllowance + ProbeMargin * 2f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, groundMask,
                QueryTriggerInteraction.Ignore) && hit.normal.y >= MinimumWalkableNormalY)
        {
            groundY = hit.point.y;
            normal = hit.normal;
            return true;
        }

        groundY = floorY;
        normal = Vector3.up;
        return false;
    }

    public Vector3 FindEdge(Vector3 lowerPoint, float lowerY, Vector3 higherPoint, float higherY,
        float floorY, float footHeight, float reach)
    {
        float threshold = (lowerY + higherY) * 0.5f;
        Vector3 low = lowerPoint;
        Vector3 high = higherPoint;
        for (int i = 0; i < EdgeSearchIterations; i++)
        {
            Vector3 middle = (low + high) * 0.5f;
            bool isHigh = TryGetGround(middle, floorY, footHeight, reach, out float middleY, out _) &&
                          middleY >= threshold;
            if (isHigh)
                high = middle;
            else
                low = middle;
        }

        return (low + high) * 0.5f;
    }
}
