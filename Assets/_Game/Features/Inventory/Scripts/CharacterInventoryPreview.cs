using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Renders an isolated copy of the player's character model for the inventory paper doll.
/// </summary>
public class CharacterInventoryPreview : MonoBehaviour
{
    private const int PreviewLayer = 31;
    private const int TextureLongEdge = 512;
    private const float CameraFieldOfView = 25f;

    private static readonly Vector3 PreviewWorldPosition = new(0f, -10000f, 0f);

    private static readonly HashSet<int> activePreviewSlots = new();
    private int previewSlot = -1;
    private RawImage target;
    private GameObject player;
    private PlayerEquipment equipment;
    private Texture originalTargetTexture;

    private GameObject previewRoot;
    private GameObject characterClone;
    private Camera previewCamera;
    private RenderTexture previewTexture;

    private IEnumerable<ItemData> previewEquipment;
    private bool refreshRequested;
    private bool renderRequested;

    /// <summary>
    /// Binds the paper-doll image and player whose current model and equipment should be shown.
    /// Calling this again replaces any previous binding and preview objects.
    /// </summary>
    public void Initialize(RawImage image, GameObject playerController, IEnumerable<ItemData> equipmentItems = null)
    {
        previewEquipment = equipmentItems;
        if (target == image && player == playerController)
        {
            if (isActiveAndEnabled)
            {
                SubscribeToEquipment();
                refreshRequested = true;
            }

            return;
        }

        UnsubscribeFromEquipment();
        ReleasePreviewObjects();

        target = image;
        player = playerController;
        originalTargetTexture = target != null ? target.texture : null;

        if (isActiveAndEnabled)
        {
            SubscribeToEquipment();
            refreshRequested = true;
        }
    }

    private void OnEnable()
    {
        if (target == null || player == null)
            return;

        originalTargetTexture = target.texture;
        SubscribeToEquipment();
        refreshRequested = true;
    }

    private void OnDisable()
    {
        UnsubscribeFromEquipment();
        ReleasePreviewObjects();
        refreshRequested = target != null && player != null;
    }

    private void OnDestroy()
    {
        UnsubscribeFromEquipment();
        ReleasePreviewObjects();
    }

    private void LateUpdate()
    {
        if (target == null || player == null || !IsTargetVisible())
            return;

        if (refreshRequested || previewRoot == null)
        {
            refreshRequested = false;
            RebuildPreview();
        }

        if (renderRequested && previewCamera != null && previewTexture != null)
        {
            RenderPreview();
            renderRequested = false;
        }
    }

    private void SubscribeToEquipment()
    {
        if (player == null)
            return;

        PlayerEquipment playerEquipment = player.GetComponent<PlayerEquipment>();
        if (playerEquipment == null)
            playerEquipment = player.GetComponentInChildren<PlayerEquipment>(true);

        if (equipment == playerEquipment)
            return;

        UnsubscribeFromEquipment();
        equipment = playerEquipment;
        if (equipment != null)
            equipment.EquipmentChanged += OnEquipmentChanged;
    }

    private void UnsubscribeFromEquipment()
    {
        if (equipment != null)
            equipment.EquipmentChanged -= OnEquipmentChanged;

        equipment = null;
    }

    private void OnEquipmentChanged(EquipmentTotals _)
    {
        // Rebuild in LateUpdate so callers can finish replacing their inventory UI this frame.
        refreshRequested = true;
    }

    private bool IsTargetVisible()
    {
        if (target == null || !target.isActiveAndEnabled || !target.gameObject.activeInHierarchy)
            return false;

        Rect rect = target.rectTransform.rect;
        if (rect.width <= 0f || rect.height <= 0f)
            return false;

        Canvas canvas = target.canvas;
        if (canvas == null || !canvas.isActiveAndEnabled || !canvas.gameObject.activeInHierarchy)
            return false;

        CanvasGroup[] canvasGroups = target.GetComponentsInParent<CanvasGroup>(true);
        foreach (CanvasGroup canvasGroup in canvasGroups)
        {
            if (canvasGroup.gameObject.activeInHierarchy && canvasGroup.alpha <= 0.001f)
                return false;
        }

        return !target.canvasRenderer.cull;
    }

    private void RebuildPreview()
    {
        ReleasePreviewObjects();

        if (target == null || player == null)
            return;

        Animator sourceAnimator = player.GetComponentInChildren<Animator>(true);
        if (sourceAnimator == null)
            return;

        CreatePreviewObjects(sourceAnimator);
    }

    private void CreatePreviewObjects(Animator sourceAnimator)
    {
        previewRoot = new GameObject("Inventory Character Preview");
        previewRoot.layer = PreviewLayer;
        previewSlot = 0;
        while (!activePreviewSlots.Add(previewSlot)) previewSlot++;
        previewRoot.transform.position = PreviewWorldPosition + Vector3.right * (previewSlot * 200f);
        previewRoot.SetActive(false);

        characterClone = Instantiate(sourceAnimator.gameObject, previewRoot.transform, false);
        characterClone.name = sourceAnimator.gameObject.name + " Inventory Preview";
        characterClone.transform.localPosition = Vector3.zero;
        characterClone.transform.localRotation = GetPreviewRotation(sourceAnimator.transform);
        characterClone.transform.localScale = sourceAnimator.transform.lossyScale;

        DisableCloneBehavioursAndPhysics(characterClone);
        if (previewEquipment != null)
        {
            var visuals = previewRoot.AddComponent<EquipmentVisualController>();
            visuals.BindAnimator(characterClone.GetComponent<Animator>());
            visuals.ApplyEquipment(previewEquipment);
        }
        SetLayerRecursively(characterClone.transform, PreviewLayer);
        CopyRendererPropertyBlocks(sourceAnimator.gameObject, characterClone);

        CreateRenderTexture();
        CreatePreviewCamera();
        CreatePreviewLight();

        previewRoot.SetActive(true);
        SetLayerRecursively(characterClone.transform, PreviewLayer);
        DisableCloneBehavioursAndPhysics(characterClone);
        FrameCharacter();

        if (target != null && previewTexture != null)
            target.texture = previewTexture;

        renderRequested = previewCamera != null && previewTexture != null;
    }

    private Quaternion GetPreviewRotation(Transform sourceModel)
    {
        if (player == null || !sourceModel.IsChildOf(player.transform))
            return sourceModel.rotation;

        return Quaternion.Inverse(player.transform.rotation) * sourceModel.rotation;
    }

    private void CreateRenderTexture()
    {
        float aspect = GetTargetAspect();
        int width = TextureLongEdge;
        int height = TextureLongEdge;

        if (aspect > 1f)
            height = Mathf.RoundToInt(TextureLongEdge / aspect);
        else
            width = Mathf.RoundToInt(TextureLongEdge * aspect);

        previewTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        {
            name = "Inventory Character Preview Texture",
            useMipMap = false,
            autoGenerateMips = false,
            antiAliasing = 1,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        previewTexture.Create();
    }

    private void CreatePreviewCamera()
    {
        GameObject cameraObject = new("Inventory Character Preview Camera");
        cameraObject.layer = PreviewLayer;
        cameraObject.transform.SetParent(previewRoot.transform, false);

        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.enabled = false;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = Color.clear;
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.fieldOfView = CameraFieldOfView;
        previewCamera.aspect = GetTargetAspect();
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 100f;
        previewCamera.allowHDR = false;
        previewCamera.allowMSAA = false;
        previewCamera.useOcclusionCulling = false;
        previewCamera.targetTexture = previewTexture;

        UniversalAdditionalCameraData additionalCameraData = previewCamera.GetUniversalAdditionalCameraData();
        additionalCameraData.renderType = CameraRenderType.Base;
        additionalCameraData.renderShadows = false;
        additionalCameraData.renderPostProcessing = false;
    }

    private void CreatePreviewLight()
    {
        GameObject lightObject = new("Inventory Character Preview Light");
        lightObject.layer = PreviewLayer;
        lightObject.transform.SetParent(previewRoot.transform, false);
        lightObject.transform.localPosition = new Vector3(1f, 3f, 3f);

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 12f;
        light.color = Color.white;
        light.intensity = 5f;
        light.shadows = LightShadows.None;
        light.cullingMask = 1 << PreviewLayer;
    }

    private void FrameCharacter()
    {
        if (previewCamera == null || characterClone == null)
            return;

        if (!TryGetCharacterBounds(characterClone, out Bounds bounds))
        {
            bounds = new Bounds(characterClone.transform.position + Vector3.up, new Vector3(0.6f, 2f, 0.4f));
        }

        float aspect = GetTargetAspect();
        previewCamera.aspect = aspect;
        float verticalExtent = Mathf.Max(bounds.extents.y, 0.05f);
        float horizontalExtent = Mathf.Max(bounds.extents.x / aspect, 0.05f);
        float distance = Mathf.Max(verticalExtent, horizontalExtent) /
            Mathf.Tan(previewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.12f;

        Vector3 forward = characterClone.transform.forward;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        previewCamera.transform.position = bounds.center + forward.normalized * distance;
        previewCamera.transform.rotation = Quaternion.LookRotation(bounds.center - previewCamera.transform.position,
            Vector3.up);
    }

    private static bool TryGetCharacterBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer is ParticleSystemRenderer)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private float GetTargetAspect()
    {
        if (target == null)
            return 0.75f;

        Rect rect = target.rectTransform.rect;
        return rect.width > 0f && rect.height > 0f
            ? Mathf.Clamp(rect.width / rect.height, 0.1f, 10f)
            : 0.75f;
    }

    private void RenderPreview()
    {
        if (RenderPipelineManager.currentPipeline == null)
        {
            previewCamera.Render();
            return;
        }

        var request = new UniversalRenderPipeline.SingleCameraRequest
        {
            destination = previewTexture
        };

        if (RenderPipeline.SupportsRenderRequest(previewCamera, request))
            RenderPipeline.SubmitRenderRequest(previewCamera, request);
    }

    private static void DisableCloneBehavioursAndPhysics(GameObject root)
    {
        foreach (Behaviour behaviour in root.GetComponentsInChildren<Behaviour>(true))
            behaviour.enabled = false;

        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        foreach (Collider2D collider in root.GetComponentsInChildren<Collider2D>(true))
            collider.enabled = false;

        foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = false;
        }

        foreach (Rigidbody2D body in root.GetComponentsInChildren<Rigidbody2D>(true))
            body.simulated = false;

        foreach (ParticleSystem particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
            SetLayerRecursively(child, layer);
    }

    private static void CopyRendererPropertyBlocks(GameObject sourceRoot, GameObject cloneRoot)
    {
        var clonesByKey = new Dictionary<string, Queue<Renderer>>();
        foreach (Renderer renderer in cloneRoot.GetComponentsInChildren<Renderer>(true))
        {
            string key = GetRendererKey(cloneRoot.transform, renderer);
            if (!clonesByKey.TryGetValue(key, out Queue<Renderer> renderers))
            {
                renderers = new Queue<Renderer>();
                clonesByKey.Add(key, renderers);
            }

            renderers.Enqueue(renderer);
        }

        var propertyBlock = new MaterialPropertyBlock();
        foreach (Renderer sourceRenderer in sourceRoot.GetComponentsInChildren<Renderer>(true))
        {
            string key = GetRendererKey(sourceRoot.transform, sourceRenderer);
            if (!clonesByKey.TryGetValue(key, out Queue<Renderer> renderers) || renderers.Count == 0)
                continue;

            Renderer cloneRenderer = renderers.Dequeue();
            propertyBlock.Clear();
            sourceRenderer.GetPropertyBlock(propertyBlock);
            cloneRenderer.SetPropertyBlock(propertyBlock);

            int materialCount = sourceRenderer.sharedMaterials.Length;
            for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
            {
                propertyBlock.Clear();
                sourceRenderer.GetPropertyBlock(propertyBlock, materialIndex);
                cloneRenderer.SetPropertyBlock(propertyBlock, materialIndex);
            }
        }
    }

    private static string GetRendererKey(Transform root, Renderer renderer)
    {
        string path = GetRelativePath(root, renderer.transform);
        Renderer[] renderersOnTransform = renderer.GetComponents<Renderer>();
        int ordinal = 0;
        foreach (Renderer candidate in renderersOnTransform)
        {
            if (candidate.GetType() != renderer.GetType())
                continue;

            if (candidate == renderer)
                break;

            ordinal++;
        }

        return path + "|" + renderer.GetType().FullName + "|" + ordinal;
    }

    private static string GetRelativePath(Transform root, Transform targetTransform)
    {
        if (targetTransform == root)
            return string.Empty;

        var pathParts = new List<string>();
        Transform current = targetTransform;
        while (current != null && current != root)
        {
            pathParts.Add(current.name + "[" + current.GetSiblingIndex() + "]");
            current = current.parent;
        }

        pathParts.Reverse();
        return string.Join("/", pathParts);
    }

    private void ReleasePreviewObjects()
    {
        if (target != null && previewTexture != null && target.texture == previewTexture)
            target.texture = originalTargetTexture;

        if (previewCamera != null)
            previewCamera.targetTexture = null;

        if (previewRoot != null)
        {
            previewRoot.SetActive(false);
            ReleaseObject(previewRoot);
        }

        if (previewTexture != null)
        {
            previewTexture.Release();
            ReleaseObject(previewTexture);
        }

        if (previewSlot >= 0) activePreviewSlots.Remove(previewSlot);
        previewSlot = -1;
        previewRoot = null;
        characterClone = null;
        previewCamera = null;
        previewTexture = null;
        renderRequested = false;
    }

    private static void ReleaseObject(Object value)
    {
        if (value == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(value);
        else
            Object.DestroyImmediate(value);
    }
}
