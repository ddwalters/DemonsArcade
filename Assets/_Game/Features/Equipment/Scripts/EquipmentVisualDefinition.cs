using System;
using UnityEngine;

[Flags]
public enum EquipmentBodyRegion
{
    None = 0,
    HeadHair = 1 << 0,
    Head = 1 << 1,
    Neck = 1 << 2,
    Torso = 1 << 3,
    UpperArms = 1 << 4,
    Forearms = 1 << 5,
    Hands = 1 << 6,
    Pelvis = 1 << 7,
    Thighs = 1 << 8,
    Calves = 1 << 9,
    Feet = 1 << 10
}

public enum EquipmentVisualAttachmentMode
{
    RigidBone,
    SkinnedRoot
}

[CreateAssetMenu(fileName = "EquipmentVisualDefinition", menuName = "Equipment/Visual Definition")]
public class EquipmentVisualDefinition : ScriptableObject
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private EquipmentVisualAttachmentMode attachmentMode = EquipmentVisualAttachmentMode.RigidBone;
    [SerializeField] private HumanBodyBones attachmentBone = HumanBodyBones.Head;

    [Header("Attachment Tuning")]
    [SerializeField] private Vector3 localPosition;
    [SerializeField] private Vector3 localEulerAngles;
    [SerializeField] private Vector3 localScale = Vector3.one;

    [Header("Body Visibility")]
    [SerializeField] private EquipmentBodyRegion hiddenBodyRegions;

    public GameObject Prefab => prefab;
    public EquipmentVisualAttachmentMode AttachmentMode => attachmentMode;
    public HumanBodyBones AttachmentBone => attachmentBone;
    public Vector3 LocalPosition => localPosition;
    public Quaternion LocalRotation => Quaternion.Euler(localEulerAngles);
    public Vector3 LocalScale => localScale;
    public EquipmentBodyRegion HiddenBodyRegions => hiddenBodyRegions;
}
