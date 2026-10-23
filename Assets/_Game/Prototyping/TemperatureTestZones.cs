using UnityEngine;

/// <summary>Two walk-in temperature circles for testing protection without a weather system.</summary>
public class TemperatureTestZones : MonoBehaviour
{
    [SerializeField] private Transform coldCircle;
    [SerializeField] private Transform warmCircle;
    [SerializeField, Min(0.1f)] private float radius = 2f;
    [SerializeField] private float coldTemperature = -4f;
    [SerializeField] private float warmTemperature = 4f;

    private PlayerTemperature player;
    private bool insideZone;
    private float previousEnvironment;
    private float nextPlayerSearch;

    private void Update()
    {
        if (player == null)
        {
            insideZone = false;
            if (Time.unscaledTime < nextPlayerSearch)
                return;

            player = FindAnyObjectByType<PlayerTemperature>();
            nextPlayerSearch = Time.unscaledTime + 0.5f;
            if (player == null)
                return;
        }

        bool inCold = Contains(coldCircle, player.transform.position);
        bool inWarm = Contains(warmCircle, player.transform.position);
        if (inCold || inWarm)
        {
            if (!insideZone)
                previousEnvironment = player.EnvironmentalTemperature;

            insideZone = true;
            player.SetEnvironmentalTemperature(inCold ? coldTemperature : warmTemperature);
        }
        else
        {
            RestoreEnvironment();
        }
    }

    private bool Contains(Transform circle, Vector3 position)
    {
        if (circle == null || !circle.gameObject.activeInHierarchy)
            return false;

        Vector3 offset = position - circle.position;
        return Mathf.Abs(offset.y) <= 3f && offset.x * offset.x + offset.z * offset.z <= radius * radius;
    }

    private void OnDisable() => RestoreEnvironment();

    private void RestoreEnvironment()
    {
        if (insideZone && player != null)
            player.SetEnvironmentalTemperature(previousEnvironment);

        insideZone = false;
    }
}
