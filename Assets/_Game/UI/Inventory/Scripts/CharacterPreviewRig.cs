using UnityEngine;

public sealed class CharacterPreviewRig : MonoBehaviour
{
    [SerializeField] private Camera previewCamera;
    [SerializeField] private EquipmentVisualController equipmentVisuals;

    public Camera Camera => previewCamera;
    public EquipmentVisualController EquipmentVisuals => equipmentVisuals;
}
