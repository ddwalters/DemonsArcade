using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class InteractionUIPanel : MonoBehaviour
{
    private const float MaximumTooltipWidth = 320f;
    private const float BadgeHeight = 30f;
    private const float BadgePadding = 16f;
    private const float PromptGap = 8f;
    private const float ProgressIndicatorOffset = 24f;
    private const float HudPromptVerticalFraction = 0.35f;
    private const float WorldNameFontSize = 36f;
    private const float WorldNameScale = 0.05f;
    private const float WorldNameMaximumWidth = 16f;
    private const float WorldNameHeight = 5f;

    private bool isReset;
    private RectTransform panelRect;
    private RectTransform promptRect;
    private RectTransform keycapRect;
    private RectTransform tooltipRect;
    private Image keycapImage;
    private TextMeshProUGUI keycapText;
    private TextMeshPro targetNameText;
    private Camera targetNameCamera;
    private string currentTooltip;
    private string currentBinding;
    private string currentTargetName;

    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI tooltipText;

    private void Awake()
    {
        panelRect = transform as RectTransform;

        if (progressBar != null)
        {
            progressBar.enabled = false;
            progressBar.raycastTarget = false;
        }

        Transform crosshair = transform.Find("Crosshair");
        if (crosshair != null)
            crosshair.gameObject.SetActive(false);

        CreatePromptElements();
        SetFixedPromptPosition();
    }

    private void LateUpdate()
    {
        SetFixedPromptPosition();
        if (targetNameText != null && targetNameText.gameObject.activeSelf && targetNameCamera != null)
            targetNameText.transform.rotation = targetNameCamera.transform.rotation;
    }

    private void OnDisable()
    {
        if (targetNameText != null)
            targetNameText.gameObject.SetActive(false);
        ResetUI();
    }

    private void OnDestroy()
    {
        if (targetNameText != null)
        {
            if (Application.isPlaying)
                Destroy(targetNameText.gameObject);
            else
                DestroyImmediate(targetNameText.gameObject);
        }
    }

    public void SetToolTip(string tooltip)
    {
        SetToolTip(tooltip, string.Empty);
    }

    public void SetToolTip(string tooltip, string bindingDisplay)
    {
        if (tooltipText == null)
            return;

        tooltip ??= string.Empty;
        bindingDisplay ??= string.Empty;
        if (currentTooltip != tooltip || currentBinding != bindingDisplay)
        {
            currentTooltip = tooltip;
            currentBinding = bindingDisplay;
            RefreshPromptLayout();
        }

        if (promptRect != null)
            promptRect.gameObject.SetActive(true);

        isReset = false;
    }

    public void SetTargetName(string targetName)
    {
        targetName ??= string.Empty;
        if (currentTargetName == targetName && targetNameText != null)
            return;

        currentTargetName = targetName;
        if (string.IsNullOrWhiteSpace(currentTargetName))
        {
            if (targetNameText != null)
                targetNameText.gameObject.SetActive(false);
            return;
        }

        CreateWorldNameLabel();
        if (targetNameText == null)
            return;

        targetNameText.SetText(currentTargetName);
        RefreshTargetNameLayout();
        targetNameText.gameObject.SetActive(false);
        isReset = false;
    }

    public void SetTargetWorldPosition(Vector3 worldPosition, Camera viewCamera)
    {
        if (targetNameText == null || string.IsNullOrWhiteSpace(currentTargetName) || viewCamera == null)
            return;

        targetNameCamera = viewCamera;
        targetNameText.transform.position = worldPosition;
        targetNameText.transform.rotation = viewCamera.transform.rotation;
        targetNameText.gameObject.SetActive(true);
    }

    public void UpdateProgressBar(float fillAmount)
    {
        fillAmount = Mathf.Clamp01(fillAmount);
        if (progressBar == null)
            return;

        progressBar.enabled = fillAmount > 0f;
        if (Mathf.Approximately(progressBar.fillAmount, fillAmount))
            return;

        progressBar.fillAmount = fillAmount;
        isReset = false;
    }

    public void ResetUI()
    {
        if (isReset)
            return;

        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
            progressBar.enabled = false;
        }

        if (tooltipText != null)
            tooltipText.SetText(string.Empty);

        if (promptRect != null)
            promptRect.gameObject.SetActive(false);
        if (targetNameText != null)
            targetNameText.gameObject.SetActive(false);

        currentTooltip = null;
        currentBinding = null;
        currentTargetName = null;
        targetNameCamera = null;
        isReset = true;
    }

    private void CreatePromptElements()
    {
        if (tooltipText == null)
            return;

        promptRect = CreateRectTransform("Interaction Prompt", transform);
        promptRect.anchorMin = new Vector2(0.5f, 0.5f);
        promptRect.anchorMax = new Vector2(0.5f, 0.5f);
        promptRect.pivot = new Vector2(0.5f, 0.5f);
        promptRect.sizeDelta = new Vector2(160f, BadgeHeight);

        GameObject keycapObject = new GameObject("Interaction Button Badge",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        keycapObject.transform.SetParent(promptRect, false);
        keycapRect = keycapObject.GetComponent<RectTransform>();
        keycapRect.anchorMin = new Vector2(0f, 0.5f);
        keycapRect.anchorMax = new Vector2(0f, 0.5f);
        keycapRect.pivot = new Vector2(0f, 0.5f);

        keycapImage = keycapObject.GetComponent<Image>();
        keycapImage.type = Image.Type.Simple;
        keycapImage.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        keycapImage.raycastTarget = false;

        GameObject keycapTextObject = new GameObject("Button Label",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        keycapTextObject.transform.SetParent(keycapRect, false);
        keycapText = keycapTextObject.GetComponent<TextMeshProUGUI>();
        keycapText.font = tooltipText.font;
        keycapText.fontSize = tooltipText.fontSize * 0.8f;
        keycapText.fontStyle = FontStyles.Bold;
        keycapText.alignment = TextAlignmentOptions.Center;
        keycapText.color = Color.white;
        keycapText.raycastTarget = false;

        RectTransform keycapTextRect = keycapText.rectTransform;
        keycapTextRect.anchorMin = Vector2.zero;
        keycapTextRect.anchorMax = Vector2.one;
        keycapTextRect.offsetMin = new Vector2(3f, 1f);
        keycapTextRect.offsetMax = new Vector2(-3f, -1f);

        tooltipRect = tooltipText.rectTransform;
        tooltipRect.SetParent(promptRect, false);
        tooltipRect.anchorMin = new Vector2(0f, 0.5f);
        tooltipRect.anchorMax = new Vector2(0f, 0.5f);
        tooltipRect.pivot = new Vector2(0f, 0.5f);
        tooltipText.alignment = TextAlignmentOptions.MidlineLeft;
        tooltipText.textWrappingMode = TextWrappingModes.NoWrap;
        tooltipText.raycastTarget = false;

        if (progressBar != null)
        {
            progressBar.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            progressBar.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            progressBar.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            progressBar.rectTransform.sizeDelta = new Vector2(18f, 18f);
            progressBar.rectTransform.SetAsFirstSibling();
        }

        promptRect.gameObject.SetActive(false);
    }

    private void RefreshPromptLayout()
    {
        bool showBinding = !string.IsNullOrWhiteSpace(currentBinding);
        if (keycapImage != null)
            keycapImage.enabled = showBinding;
        if (keycapText != null)
            keycapText.gameObject.SetActive(showBinding);

        if (showBinding)
            keycapText.SetText(currentBinding);

        tooltipText.SetText(currentTooltip);

        Vector2 tooltipSize = tooltipText.GetPreferredValues(currentTooltip, MaximumTooltipWidth, 100f);
        float tooltipWidth = Mathf.Clamp(tooltipSize.x, 32f, MaximumTooltipWidth);
        float tooltipHeight = Mathf.Max(BadgeHeight, tooltipSize.y);
        float badgeWidth = 0f;
        if (showBinding)
        {
            float preferredBadgeWidth = keycapText.GetPreferredValues(currentBinding, 200f, BadgeHeight).x;
            badgeWidth = Mathf.Clamp(preferredBadgeWidth + BadgePadding, 36f, 120f);
            keycapRect.sizeDelta = new Vector2(badgeWidth, BadgeHeight);
        }

        float gap = showBinding ? PromptGap : 0f;
        float totalWidth = badgeWidth + gap + tooltipWidth;
        float totalHeight = Mathf.Max(BadgeHeight, tooltipHeight);
        promptRect.sizeDelta = new Vector2(totalWidth, totalHeight);
        keycapRect.anchoredPosition = Vector2.zero;
        tooltipRect.anchoredPosition = new Vector2(badgeWidth + gap, 0f);
        tooltipRect.sizeDelta = new Vector2(tooltipWidth, tooltipHeight);
    }

    private void RefreshTargetNameLayout()
    {
        if (targetNameText == null)
            return;

        Vector2 textSize = targetNameText.GetPreferredValues(currentTargetName);
        float width = Mathf.Max(textSize.x, 2f);
        float height = Mathf.Max(textSize.y, WorldNameHeight);
        targetNameText.rectTransform.sizeDelta = new Vector2(width, height);
    }

    private void CreateWorldNameLabel()
    {
        if (targetNameText != null || tooltipText == null)
            return;

        GameObject nameObject = new GameObject("Interaction Target Name", typeof(TextMeshPro));
        nameObject.layer = 0;
        targetNameText = nameObject.GetComponent<TextMeshPro>();
        targetNameText.font = tooltipText.font;
        targetNameText.fontSize = WorldNameFontSize;
        targetNameText.fontStyle = FontStyles.Bold;
        targetNameText.alignment = TextAlignmentOptions.Center;
        targetNameText.color = Color.white;
        targetNameText.isOrthographic = false;
        targetNameText.textWrappingMode = TextWrappingModes.NoWrap;
        targetNameText.overflowMode = TextOverflowModes.Overflow;

        RectTransform nameRect = targetNameText.rectTransform;
        nameRect.anchorMin = new Vector2(0.5f, 0.5f);
        nameRect.anchorMax = new Vector2(0.5f, 0.5f);
        nameRect.pivot = new Vector2(0.5f, 0.5f);
        nameRect.sizeDelta = new Vector2(WorldNameMaximumWidth, WorldNameHeight);
        targetNameText.transform.localScale = Vector3.one * WorldNameScale;

        Renderer nameRenderer = targetNameText.GetComponent<Renderer>();
        if (nameRenderer != null)
        {
            nameRenderer.shadowCastingMode = ShadowCastingMode.Off;
            nameRenderer.receiveShadows = false;
        }

        targetNameText.gameObject.SetActive(false);
    }

    private void SetFixedPromptPosition()
    {
        if (panelRect == null || promptRect == null)
            return;

        Vector2 hudPosition = Vector2.down * panelRect.rect.height * HudPromptVerticalFraction;
        promptRect.anchoredPosition = hudPosition;
        if (progressBar != null)
            progressBar.rectTransform.anchoredPosition = hudPosition + Vector2.down * ProgressIndicatorOffset;
    }

    private static RectTransform CreateRectTransform(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }
}
