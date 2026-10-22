using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionUIPanel : MonoBehaviour
{
    private const float MaximumTooltipWidth = 320f;
    private const float BadgeHeight = 30f;
    private const float BadgePadding = 16f;
    private const float PromptGap = 8f;
    private const float ProgressIndicatorOffset = 24f;
    private const float HudPromptVerticalFraction = 0.35f;

    [Serializable]
    private struct NameLabelEntry
    {
        public InteractionWorldSpaceNameType nameType;
        public FloatingNameLabel prefab;
    }

    private bool isReset;
    private RectTransform panelRect;
    private RectTransform promptRect;
    private RectTransform keycapRect;
    private RectTransform tooltipRect;
    private Image keycapImage;
    private TextMeshProUGUI keycapText;
    private FloatingNameLabel activeNameLabel;
    private bool missingNameLabelReported;
    private string currentTooltip;
    private string currentBinding;
    private string currentTargetName;
    private InteractionWorldSpaceNameType currentTargetNameType;
    private readonly Dictionary<InteractionWorldSpaceNameType, FloatingNameLabel> nameLabelInstances = new();

    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private NameLabelEntry[] nameLabels;

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
    }

    private void OnDisable()
    {
        ResetUI();
    }

    private void OnDestroy()
    {
        foreach (FloatingNameLabel label in nameLabelInstances.Values)
        {
            if (label != null)
                Destroy(label.gameObject);
        }

        nameLabelInstances.Clear();
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

    public void SetTargetName(string targetName, InteractionWorldSpaceNameType nameType)
    {
        targetName ??= string.Empty;
        if (currentTargetName == targetName && currentTargetNameType == nameType && activeNameLabel != null)
            return;

        if (activeNameLabel != null)
            activeNameLabel.Hide();

        activeNameLabel = null;
        currentTargetName = targetName;
        currentTargetNameType = nameType;
        if (string.IsNullOrWhiteSpace(currentTargetName))
            return;

        activeNameLabel = GetNameLabel(nameType);
        if (activeNameLabel == null)
            return;

        activeNameLabel.SetName(currentTargetName);
        activeNameLabel.Hide();
        isReset = false;
    }

    public void SetTargetWorldPosition(Vector3 worldPosition, Camera viewCamera)
    {
        if (activeNameLabel == null || viewCamera == null)
            return;

        activeNameLabel.Show(worldPosition, viewCamera);
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
        if (activeNameLabel != null)
            activeNameLabel.Hide();

        activeNameLabel = null;
        currentTooltip = null;
        currentBinding = null;
        currentTargetName = null;
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

    private FloatingNameLabel GetNameLabel(InteractionWorldSpaceNameType nameType)
    {
        if (nameLabelInstances.TryGetValue(nameType, out FloatingNameLabel existing) && existing != null)
            return existing;

        FloatingNameLabel prefab = FindNameLabelPrefab(nameType);
        if (prefab == null)
            prefab = FindNameLabelPrefab(InteractionWorldSpaceNameType.Default);

        if (prefab == null)
        {
            if (!missingNameLabelReported)
            {
                Debug.LogWarning("InteractionUIPanel has no name label prefab assigned.", this);
                missingNameLabelReported = true;
            }

            return null;
        }

        FloatingNameLabel instance = Instantiate(prefab);
        instance.gameObject.SetActive(false);
        nameLabelInstances[nameType] = instance;
        return instance;
    }

    private FloatingNameLabel FindNameLabelPrefab(InteractionWorldSpaceNameType nameType)
    {
        if (nameLabels == null)
            return null;

        foreach (NameLabelEntry entry in nameLabels)
        {
            if (entry.nameType == nameType && entry.prefab != null)
                return entry.prefab;
        }

        return null;
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
