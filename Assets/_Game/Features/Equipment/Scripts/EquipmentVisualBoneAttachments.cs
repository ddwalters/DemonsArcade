using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class EquipmentVisualBoneAttachment
{
    [SerializeField] private Transform visualPart;
    [SerializeField] private HumanBodyBones attachmentBone;
    [SerializeField] private HumanBodyBones endBone = HumanBodyBones.LastBone;
    [SerializeField] private bool fitToBoneSegment;
    [SerializeField] private Vector3 localPosition;
    [SerializeField] private Vector3 localEulerAngles;
    [SerializeField] private Vector3 localScale = Vector3.one;

    public Transform VisualPart => visualPart;
    public HumanBodyBones AttachmentBone => attachmentBone;
    public HumanBodyBones EndBone => endBone;
    public bool FitToBoneSegment => fitToBoneSegment;
    public Vector3 LocalPosition => localPosition;
    public Quaternion LocalRotation => Quaternion.Euler(localEulerAngles);
    public Vector3 LocalScale => localScale;

    public EquipmentVisualBoneAttachment(
        Transform part,
        HumanBodyBones bone,
        Vector3 position,
        Vector3 eulerAngles,
        Vector3 scale,
        bool fitSegment = false,
        HumanBodyBones segmentEndBone = HumanBodyBones.LastBone)
    {
        visualPart = part;
        attachmentBone = bone;
        endBone = segmentEndBone;
        fitToBoneSegment = fitSegment;
        localPosition = position;
        localEulerAngles = eulerAngles;
        localScale = scale;
    }
}

/// <summary>
/// Splits one equipment visual across humanoid bones while keeping each part owned by its visual root.
/// </summary>
public sealed class EquipmentVisualBoneAttachments : MonoBehaviour
{
    [SerializeField] private EquipmentVisualBoneAttachment[] attachments = Array.Empty<EquipmentVisualBoneAttachment>();

    private readonly List<GameObject> attachedParts = new();

    public IReadOnlyList<GameObject> AttachedParts => attachedParts;

    public void SetAttachments(params EquipmentVisualBoneAttachment[] boneAttachments)
    {
        attachments = boneAttachments != null
            ? (EquipmentVisualBoneAttachment[])boneAttachments.Clone()
            : Array.Empty<EquipmentVisualBoneAttachment>();
    }

    public bool TryAttachToRig(Animator rigAnimator, out string failure)
    {
        failure = null;
        attachedParts.Clear();

        if (rigAnimator == null || !rigAnimator.isHuman)
        {
            failure = "HumanoidParts attachments require a valid humanoid Animator";
            return false;
        }

        if (attachments == null || attachments.Length == 0)
        {
            failure = "the visual has no humanoid bone parts configured";
            return false;
        }

        var resolvedBones = new Transform[attachments.Length];
        var resolvedEndBones = new Transform[attachments.Length];
        for (int i = 0; i < attachments.Length; i++)
        {
            EquipmentVisualBoneAttachment attachment = attachments[i];
            if (attachment == null || attachment.VisualPart == null ||
                attachment.VisualPart == transform || !attachment.VisualPart.IsChildOf(transform))
            {
                failure = "each humanoid bone part must reference a child transform in the visual prefab";
                return false;
            }

            if (attachment.AttachmentBone == HumanBodyBones.LastBone)
            {
                failure = "a humanoid bone part uses the LastBone sentinel";
                return false;
            }

            try
            {
                resolvedBones[i] = rigAnimator.GetBoneTransform(attachment.AttachmentBone);
            }
            catch (Exception exception)
            {
                failure = $"{attachment.AttachmentBone} could not be resolved ({exception.Message})";
                return false;
            }

            if (resolvedBones[i] == null)
            {
                failure = $"the humanoid rig does not contain {attachment.AttachmentBone}";
                return false;
            }

            if (attachment.FitToBoneSegment)
            {
                if (attachment.EndBone == HumanBodyBones.LastBone)
                {
                    failure = $"{attachment.AttachmentBone} has no end bone configured for segment fitting";
                    return false;
                }

                try
                {
                    resolvedEndBones[i] = rigAnimator.GetBoneTransform(attachment.EndBone);
                }
                catch (Exception exception)
                {
                    failure = $"{attachment.EndBone} could not be resolved ({exception.Message})";
                    return false;
                }

                if (resolvedEndBones[i] == null)
                {
                    failure = $"the humanoid rig does not contain {attachment.EndBone}";
                    return false;
                }
            }

            for (int previousIndex = 0; previousIndex < i; previousIndex++)
            {
                if (attachments[previousIndex].VisualPart == attachment.VisualPart)
                {
                    failure = "a visual part is assigned to more than one humanoid bone";
                    return false;
                }
            }
        }

        try
        {
            for (int i = 0; i < attachments.Length; i++)
            {
                EquipmentVisualBoneAttachment attachment = attachments[i];
                Transform part = attachment.VisualPart;
                part.SetParent(resolvedBones[i], false);
                if (attachment.FitToBoneSegment)
                {
                    FitPartToBoneSegment(part, resolvedBones[i], resolvedEndBones[i], attachment.LocalScale);
                }
                else
                {
                    part.localPosition = attachment.LocalPosition;
                    part.localRotation = attachment.LocalRotation;
                    part.localScale = attachment.LocalScale;
                }

                attachedParts.Add(part.gameObject);
            }
        }
        catch (Exception exception)
        {
            RestorePartsToVisualRoot();
            failure = $"the visual parts could not be attached ({exception.Message})";
            return false;
        }

        return true;
    }

    private static void FitPartToBoneSegment(
        Transform part,
        Transform startBone,
        Transform endBone,
        Vector3 scaleRatios)
    {
        Vector3 localEndPosition = startBone.InverseTransformPoint(endBone.position);
        float localLength = localEndPosition.magnitude;
        float worldLength = Vector3.Distance(startBone.position, endBone.position);
        Vector3 lossyScale = startBone.lossyScale;

        if (localLength <= Mathf.Epsilon || worldLength <= Mathf.Epsilon)
            throw new InvalidOperationException("The configured humanoid segment has zero length.");

        part.localPosition = localEndPosition * 0.5f;
        part.localRotation = Quaternion.FromToRotation(Vector3.up, localEndPosition.normalized);
        part.localScale = new Vector3(
            worldLength * scaleRatios.x / Mathf.Max(Mathf.Abs(lossyScale.x), 0.0001f),
            localLength * Mathf.Max(scaleRatios.y, 1f),
            worldLength * scaleRatios.z / Mathf.Max(Mathf.Abs(lossyScale.z), 0.0001f));
    }

    public void RestorePartsToVisualRoot()
    {
        if (attachments == null)
            return;

        foreach (EquipmentVisualBoneAttachment attachment in attachments)
        {
            if (attachment?.VisualPart == null || attachment.VisualPart == transform)
                continue;

            attachment.VisualPart.SetParent(transform, false);
            attachment.VisualPart.localPosition = attachment.LocalPosition;
            attachment.VisualPart.localRotation = attachment.LocalRotation;
            attachment.VisualPart.localScale = attachment.LocalScale;
        }

        attachedParts.Clear();
    }
}
