using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class EquipmentBodyRendererMapping
{
    [SerializeField] private Renderer renderer;
    [SerializeField] private EquipmentBodyRegion regions;

    public Renderer Renderer => renderer;
    public EquipmentBodyRegion Regions => regions;

    public EquipmentBodyRendererMapping()
    {
    }

    public EquipmentBodyRendererMapping(Renderer targetRenderer, EquipmentBodyRegion bodyRegions)
    {
        renderer = targetRenderer;
        regions = bodyRegions;
    }
}

/// <summary>
/// Applies optional equipment visuals to any humanoid character rig.
/// Body meshes are only hidden when they have been explicitly mapped here.
/// </summary>
public class EquipmentVisualController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private EquipmentBodyRendererMapping[] bodyRendererMappings = Array.Empty<EquipmentBodyRendererMapping>();

    private readonly List<GameObject> ownedVisuals = new();
    private readonly Dictionary<Renderer, bool> originalRendererStates = new();
    private readonly List<EquipmentVisualDefinition> reportedVisualIssues = new();
    private readonly List<EquipmentVisualRequest> previousRequests = new();

    private List<ItemData> lastRequestedItems;
    private Animator appliedAnimator;
    private EquipmentBodyRegion activeHiddenRegions;
    private bool hasAppliedRequest;

    public Animator BoundAnimator => ResolveAnimator();

    private void Awake()
    {
        ResolveAnimator();
    }

    private void OnEnable()
    {
        if (lastRequestedItems != null)
            ApplyEquipment(lastRequestedItems);
    }

    private void OnDisable()
    {
        DestroyOwnedVisuals();
        RestoreBodyRenderers();
        activeHiddenRegions = EquipmentBodyRegion.None;
        hasAppliedRequest = false;
    }

    private void OnDestroy()
    {
        DestroyOwnedVisuals();
        RestoreBodyRenderers();
    }

    /// <summary>
    /// Binds a rig explicitly. Passing null looks for an Animator under this object.
    /// Existing visuals are rebuilt against the new rig.
    /// </summary>
    public void BindAnimator(Animator rigAnimator)
    {
        Animator resolvedAnimator = rigAnimator != null ? rigAnimator : GetComponentInChildren<Animator>(true);
        if (animator == resolvedAnimator)
            return;

        DestroyOwnedVisuals();
        RestoreBodyRenderers();
        activeHiddenRegions = EquipmentBodyRegion.None;
        animator = resolvedAnimator;
        hasAppliedRequest = false;

        if (lastRequestedItems != null && isActiveAndEnabled)
            ApplyEquipment(lastRequestedItems);
    }

    public void ConfigureAnimator(Animator rigAnimator)
    {
        BindAnimator(rigAnimator);
    }

    /// <summary>
    /// Configures the explicit body renderers that equipment may hide.
    /// Empty mappings leave the character body untouched.
    /// </summary>
    public void SetBodyRendererMappings(params EquipmentBodyRendererMapping[] mappings)
    {
        RestoreBodyRenderers();

        if (mappings == null || mappings.Length == 0)
        {
            bodyRendererMappings = Array.Empty<EquipmentBodyRendererMapping>();
        }
        else
        {
            bodyRendererMappings = (EquipmentBodyRendererMapping[])mappings.Clone();
        }

        ApplyHiddenBodyRegions(activeHiddenRegions);
    }

    public void ConfigureBodyRegions(params EquipmentBodyRendererMapping[] mappings)
    {
        SetBodyRendererMappings(mappings);
    }

    public EquipmentBodyRendererMapping[] GetBodyRendererMappings()
    {
        return bodyRendererMappings == null
            ? Array.Empty<EquipmentBodyRendererMapping>()
            : (EquipmentBodyRendererMapping[])bodyRendererMappings.Clone();
    }

    /// <summary>
    /// Applies equipped items. Items without a visual definition or prefab are ignored.
    /// Calling this repeatedly with the same visual items does not create duplicate instances.
    /// </summary>
    public void ApplyEquipment(IEnumerable<ItemData> equippedItems)
    {
        List<ItemData> normalizedItems = NormalizeItems(equippedItems);
        lastRequestedItems = normalizedItems;

        if (!isActiveAndEnabled)
        {
            hasAppliedRequest = false;
            return;
        }

        Animator rigAnimator = ResolveAnimator();
        if (!ReferenceEquals(appliedAnimator, rigAnimator))
            hasAppliedRequest = false;

        List<EquipmentVisualRequest> requests = BuildRequests(normalizedItems);
        if (hasAppliedRequest && AreSameRequests(previousRequests, requests))
            return;

        DestroyOwnedVisuals();
        RestoreBodyRenderers();
        activeHiddenRegions = EquipmentBodyRegion.None;

        foreach (EquipmentVisualRequest request in requests)
        {
            if (!TryCreateVisual(request, rigAnimator, out GameObject visualInstance))
                continue;

            ownedVisuals.Add(visualInstance);
            activeHiddenRegions |= request.Definition.HiddenBodyRegions;
        }

        ApplyHiddenBodyRegions(activeHiddenRegions);

        previousRequests.Clear();
        previousRequests.AddRange(requests);
        appliedAnimator = rigAnimator;
        hasAppliedRequest = true;
    }

    /// <summary>
    /// Retries the most recently requested equipment after a rig or visual asset becomes available.
    /// </summary>
    public void RefreshEquipment()
    {
        hasAppliedRequest = false;
        if (lastRequestedItems != null)
            ApplyEquipment(lastRequestedItems);
    }

    public void ClearEquipment()
    {
        lastRequestedItems = null;
        previousRequests.Clear();
        hasAppliedRequest = true;
        activeHiddenRegions = EquipmentBodyRegion.None;
        DestroyOwnedVisuals();
        RestoreBodyRenderers();
    }

    private Animator ResolveAnimator()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        return animator;
    }

    private static List<ItemData> NormalizeItems(IEnumerable<ItemData> equippedItems)
    {
        var items = new List<ItemData>();
        if (equippedItems == null)
            return items;

        foreach (ItemData item in equippedItems)
        {
            if (item != null)
                items.Add(item);
        }

        return items;
    }

    private static List<EquipmentVisualRequest> BuildRequests(List<ItemData> items)
    {
        var requests = new List<EquipmentVisualRequest>();
        foreach (ItemData item in items)
        {
            EquipmentVisualDefinition definition = item.itemStats != null
                ? item.itemStats.GetEquipmentVisual()
                : null;

            if (definition == null || definition.Prefab == null)
                continue;

            requests.Add(new EquipmentVisualRequest(item, definition, definition.Prefab));
        }

        return requests;
    }

    private static bool AreSameRequests(
        List<EquipmentVisualRequest> previous,
        List<EquipmentVisualRequest> current)
    {
        if (previous.Count != current.Count)
            return false;

        var matchedPrevious = new bool[previous.Count];
        foreach (EquipmentVisualRequest currentRequest in current)
        {
            bool foundMatch = false;
            for (int i = 0; i < previous.Count; i++)
            {
                if (matchedPrevious[i])
                    continue;

                EquipmentVisualRequest previousRequest = previous[i];
                if (!ReferenceEquals(previousRequest.Item, currentRequest.Item) ||
                    !ReferenceEquals(previousRequest.Definition, currentRequest.Definition) ||
                    !ReferenceEquals(previousRequest.Prefab, currentRequest.Prefab))
                {
                    continue;
                }

                matchedPrevious[i] = true;
                foundMatch = true;
                break;
            }

            if (!foundMatch)
                return false;
        }

        return true;
    }

    private bool TryCreateVisual(
        EquipmentVisualRequest request,
        Animator rigAnimator,
        out GameObject visualInstance)
    {
        visualInstance = null;
        if (rigAnimator == null)
        {
            ReportVisualIssue(request.Definition, "no Animator is bound to this character");
            return false;
        }

        Transform attachment;
        switch (request.Definition.AttachmentMode)
        {
            case EquipmentVisualAttachmentMode.RigidBone:
                if (!rigAnimator.isHuman || request.Definition.AttachmentBone == HumanBodyBones.LastBone)
                {
                    ReportVisualIssue(request.Definition,
                        "RigidBone attachments require a valid humanoid Animator");
                    return false;
                }

                try
                {
                    attachment = rigAnimator.GetBoneTransform(request.Definition.AttachmentBone);
                }
                catch (Exception exception)
                {
                    ReportVisualIssue(request.Definition,
                        $"the configured humanoid bone could not be resolved ({exception.Message})");
                    return false;
                }

                if (attachment == null)
                {
                    ReportVisualIssue(request.Definition,
                        $"the humanoid rig does not contain {request.Definition.AttachmentBone}");
                    return false;
                }
                break;

            case EquipmentVisualAttachmentMode.SkinnedRoot:
                attachment = rigAnimator.transform;
                break;

            default:
                ReportVisualIssue(request.Definition, "the attachment mode is not supported");
                return false;
        }

        GameObject stagingRoot = new GameObject("EquipmentVisualStaging");
        stagingRoot.hideFlags = HideFlags.HideAndDontSave;
        stagingRoot.SetActive(false);

        try
        {
            visualInstance = Instantiate(request.Prefab, stagingRoot.transform, false);
            if (visualInstance == null)
            {
                ReportVisualIssue(request.Definition, "the visual prefab could not be instantiated");
                DestroyOwnedObject(stagingRoot);
                return false;
            }

            visualInstance.SetActive(false);
            DisableVisualPhysicsAndAnimators(visualInstance);

            if (request.Definition.AttachmentMode == EquipmentVisualAttachmentMode.RigidBone &&
                !HasRenderableContent(visualInstance))
            {
                ReportVisualIssue(request.Definition, "the rigid visual prefab has no enabled Renderer with geometry");
                DestroyOwnedObject(visualInstance);
                DestroyOwnedObject(stagingRoot);
                visualInstance = null;
                return false;
            }

            if (request.Definition.AttachmentMode == EquipmentVisualAttachmentMode.SkinnedRoot &&
                !TryRebindSkinnedMeshes(visualInstance, rigAnimator.transform, out string rebindFailure))
            {
                ReportVisualIssue(request.Definition, rebindFailure);
                DestroyOwnedObject(visualInstance);
                DestroyOwnedObject(stagingRoot);
                visualInstance = null;
                return false;
            }

            Transform visualTransform = visualInstance.transform;
            visualTransform.SetParent(attachment, false);
            visualTransform.localPosition = request.Definition.LocalPosition;
            visualTransform.localRotation = request.Definition.LocalRotation;
            visualTransform.localScale = request.Definition.LocalScale;

            DestroyOwnedObject(stagingRoot);
            visualInstance.SetActive(true);
            return true;
        }
        catch (Exception exception)
        {
            ReportVisualIssue(request.Definition, $"visual setup failed ({exception.Message})");
            if (visualInstance != null)
                DestroyOwnedObject(visualInstance);
            DestroyOwnedObject(stagingRoot);
            visualInstance = null;
            return false;
        }
    }

    private static void DisableVisualPhysicsAndAnimators(GameObject visualRoot)
    {
        foreach (Animator visualAnimator in visualRoot.GetComponentsInChildren<Animator>(true))
            visualAnimator.enabled = false;

        foreach (Animation legacyAnimation in visualRoot.GetComponentsInChildren<Animation>(true))
            legacyAnimation.enabled = false;

        foreach (Collider visualCollider in visualRoot.GetComponentsInChildren<Collider>(true))
            visualCollider.enabled = false;

        foreach (Collider2D visualCollider in visualRoot.GetComponentsInChildren<Collider2D>(true))
            visualCollider.enabled = false;

        foreach (Rigidbody visualBody in visualRoot.GetComponentsInChildren<Rigidbody>(true))
        {
            visualBody.isKinematic = true;
            visualBody.useGravity = false;
            visualBody.detectCollisions = false;
        }

        foreach (Rigidbody2D visualBody in visualRoot.GetComponentsInChildren<Rigidbody2D>(true))
            visualBody.simulated = false;
    }

    private static bool TryRebindSkinnedMeshes(
        GameObject visualRoot,
        Transform rigRoot,
        out string failure)
    {
        failure = null;
        SkinnedMeshRenderer[] renderers = visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (renderers.Length == 0)
        {
            failure = "SkinnedRoot requires at least one SkinnedMeshRenderer";
            return false;
        }

        var sourceBones = new List<Transform>();
        bool hasEnabledSkinnedMesh = false;
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            if (renderer.sharedMesh == null)
            {
                failure = $"SkinnedMeshRenderer '{renderer.name}' has no mesh";
                return false;
            }

            Transform[] bones = renderer.bones;
            if (bones == null || bones.Length == 0 || renderer.rootBone == null)
            {
                failure = $"SkinnedMeshRenderer '{renderer.name}' is missing its root bone or bone list";
                return false;
            }

            Matrix4x4[] bindposes = renderer.sharedMesh.bindposes;
            if (bindposes == null || bindposes.Length != bones.Length)
            {
                failure = $"SkinnedMeshRenderer '{renderer.name}' has {bones.Length} bones but " +
                    $"{(bindposes != null ? bindposes.Length : 0)} bindposes";
                return false;
            }

            if (renderer.enabled && IsRendererHierarchyActive(renderer.transform, visualRoot.transform))
                hasEnabledSkinnedMesh = true;

            sourceBones.Add(renderer.rootBone);
            foreach (Transform bone in bones)
            {
                if (bone == null)
                {
                    failure = $"SkinnedMeshRenderer '{renderer.name}' has a missing bone reference";
                    return false;
                }

                sourceBones.Add(bone);
            }
        }

        if (!hasEnabledSkinnedMesh)
        {
            failure = "SkinnedRoot has no enabled SkinnedMeshRenderer in the visual prefab";
            return false;
        }

        Transform sourceSkeletonRoot = FindCommonAncestor(visualRoot.transform, sourceBones);
        if (sourceSkeletonRoot == null)
        {
            failure = "the prefab bones do not share a skeleton beneath the visual prefab root";
            return false;
        }

        Transform[] rigTransforms = rigRoot.GetComponentsInChildren<Transform>(true);
        var boneMap = new Dictionary<Transform, Transform>();
        foreach (Transform sourceBone in sourceBones)
        {
            if (boneMap.ContainsKey(sourceBone))
                continue;

            List<string> sourcePath = BuildHierarchyPath(sourceSkeletonRoot, sourceBone, visualRoot.transform);
            if (sourcePath.Count == 0)
            {
                failure = $"bone '{sourceBone.name}' has no usable hierarchy path";
                return false;
            }

            Transform match = null;
            int matchCount = 0;
            foreach (Transform rigTransform in rigTransforms)
            {
                if (!IsAtOrBelow(rigTransform, rigRoot) || !PathEndsAtTransform(sourcePath, rigTransform))
                    continue;

                match = rigTransform;
                matchCount++;
                if (matchCount > 1)
                    break;
            }

            if (matchCount != 1)
            {
                failure = matchCount == 0
                    ? $"rig bone hierarchy for '{sourceBone.name}' could not be matched"
                    : $"rig bone hierarchy for '{sourceBone.name}' matched more than one transform";
                return false;
            }

            boneMap.Add(sourceBone, match);
        }

        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            Transform[] sourceRendererBones = renderer.bones;
            var remappedBones = new Transform[sourceRendererBones.Length];
            for (int i = 0; i < sourceRendererBones.Length; i++)
                remappedBones[i] = boneMap[sourceRendererBones[i]];

            // The shared mesh and its bindposes stay untouched; only its rig references change.
            renderer.bones = remappedBones;
            renderer.rootBone = boneMap[renderer.rootBone];
        }

        return true;
    }

    private static bool HasRenderableContent(GameObject visualRoot)
    {
        foreach (Renderer renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.enabled || !IsRendererHierarchyActive(renderer.transform, visualRoot.transform))
                continue;

            if (renderer is SkinnedMeshRenderer skinnedRenderer)
            {
                if (skinnedRenderer.sharedMesh != null)
                    return true;

                continue;
            }

            if (renderer is MeshRenderer)
            {
                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                    return true;

                continue;
            }

            if (renderer is SpriteRenderer spriteRenderer)
            {
                if (spriteRenderer.sprite != null)
                    return true;

                continue;
            }

            return true;
        }

        return false;
    }

    private static bool IsRendererHierarchyActive(Transform rendererTransform, Transform visualRoot)
    {
        for (Transform current = rendererTransform; current != null && current != visualRoot; current = current.parent)
        {
            if (!current.gameObject.activeSelf)
                return false;
        }

        return IsAtOrBelow(rendererTransform, visualRoot);
    }

    private static Transform FindCommonAncestor(Transform visualRoot, List<Transform> bones)
    {
        if (bones.Count == 0)
            return null;

        Transform firstBone = bones[0];
        for (Transform candidate = firstBone; candidate != null; candidate = candidate.parent)
        {
            if (!IsAtOrBelow(candidate, visualRoot))
                continue;

            bool containsEveryBone = true;
            foreach (Transform bone in bones)
            {
                if (!IsAtOrBelow(bone, candidate))
                {
                    containsEveryBone = false;
                    break;
                }
            }

            if (containsEveryBone)
                return candidate;

            if (candidate == visualRoot)
                break;
        }

        return null;
    }

    private static List<string> BuildHierarchyPath(Transform sourceRoot, Transform bone, Transform visualRoot)
    {
        var path = new List<string>();
        for (Transform current = bone; current != null; current = current.parent)
        {
            if (current == visualRoot)
                break;

            path.Add(current.name);
            if (current == sourceRoot)
                break;
        }

        path.Reverse();
        return path;
    }

    private static bool PathEndsAtTransform(List<string> path, Transform candidate)
    {
        Transform current = candidate;
        for (int i = path.Count - 1; i >= 0; i--)
        {
            if (current == null || current.name != path[i])
                return false;

            current = current.parent;
        }

        return true;
    }

    private static bool IsAtOrBelow(Transform transform, Transform ancestor)
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            if (current == ancestor)
                return true;
        }

        return false;
    }

    private void ApplyHiddenBodyRegions(EquipmentBodyRegion regions)
    {
        var renderersToHide = new HashSet<Renderer>();
        if (bodyRendererMappings != null)
        {
            foreach (EquipmentBodyRendererMapping mapping in bodyRendererMappings)
            {
                if (mapping == null || mapping.Renderer == null ||
                    (mapping.Regions & regions) == EquipmentBodyRegion.None)
                {
                    continue;
                }

                renderersToHide.Add(mapping.Renderer);
            }
        }

        var previouslyHidden = new List<Renderer>(originalRendererStates.Keys);
        foreach (Renderer renderer in previouslyHidden)
        {
            if (renderer == null || renderersToHide.Contains(renderer))
                continue;

            renderer.enabled = originalRendererStates[renderer];
            originalRendererStates.Remove(renderer);
        }

        foreach (Renderer renderer in renderersToHide)
        {
            if (!originalRendererStates.ContainsKey(renderer))
                originalRendererStates.Add(renderer, renderer.enabled);

            renderer.enabled = false;
        }
    }

    private void RestoreBodyRenderers()
    {
        foreach (KeyValuePair<Renderer, bool> rendererState in originalRendererStates)
        {
            if (rendererState.Key != null)
                rendererState.Key.enabled = rendererState.Value;
        }

        originalRendererStates.Clear();
    }

    private void DestroyOwnedVisuals()
    {
        foreach (GameObject visual in ownedVisuals)
        {
            if (visual == null)
                continue;

            visual.SetActive(false);
            DestroyOwnedObject(visual);
        }

        ownedVisuals.Clear();
    }

    private void ReportVisualIssue(EquipmentVisualDefinition definition, string reason)
    {
        foreach (EquipmentVisualDefinition reportedDefinition in reportedVisualIssues)
        {
            if (ReferenceEquals(reportedDefinition, definition))
                return;
        }

        reportedVisualIssues.Add(definition);

        string definitionName = definition != null ? definition.name : "<missing definition>";
        Debug.LogWarning(
            $"Equipment visual '{definitionName}' on '{name}' was skipped: {reason}.",
            this);
    }

    private static void DestroyOwnedObject(GameObject ownedObject)
    {
        if (ownedObject == null)
            return;

        if (Application.isPlaying)
            Destroy(ownedObject);
        else
            DestroyImmediate(ownedObject);
    }

    private readonly struct EquipmentVisualRequest
    {
        public readonly ItemData Item;
        public readonly EquipmentVisualDefinition Definition;
        public readonly GameObject Prefab;

        public EquipmentVisualRequest(ItemData item, EquipmentVisualDefinition definition, GameObject prefab)
        {
            Item = item;
            Definition = definition;
            Prefab = prefab;
        }
    }
}
