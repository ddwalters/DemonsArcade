using UnityEngine;

/// <summary>Environmental exposure after equipment protection, in signed temperature points.</summary>
[DisallowMultipleComponent]
public class PlayerTemperature : MonoBehaviour
{
    [Tooltip("Negative is cold, positive is hot, and zero is moderate. Future environments set this value.")]
    [SerializeField] private float environmentalTemperature;
    [SerializeField, Min(0.01f)] private float responsePerSecond = 2f;

    private PlayerEquipment equipment;

    public float EnvironmentalTemperature => environmentalTemperature;
    public float CurrentTemperature { get; private set; }
    public float TargetTemperature => ApplyResistance(environmentalTemperature,
        equipment != null ? equipment.GetBonus(EquipmentStat.ColdResistance) : 0,
        equipment != null ? equipment.GetBonus(EquipmentStat.HeatResistance) : 0);

    private void Update()
    {
        if (equipment == null)
            equipment = GetComponent<PlayerEquipment>();

        CurrentTemperature = Mathf.MoveTowards(CurrentTemperature, TargetTemperature,
            responsePerSecond * Time.deltaTime);
    }

    public void SetEnvironmentalTemperature(float points)
    {
        if (!float.IsNaN(points) && !float.IsInfinity(points))
            environmentalTemperature = points;
    }

    public static float ApplyResistance(float environment, int coldResistance, int heatResistance)
    {
        if (environment < 0f)
            return -Mathf.Max(0f, -environment - coldResistance);
        if (environment > 0f)
            return Mathf.Max(0f, environment - heatResistance);

        return 0f;
    }
}
