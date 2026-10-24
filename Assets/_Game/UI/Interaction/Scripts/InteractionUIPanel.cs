using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionUIPanel : MonoBehaviour
{
    [SerializeField] private float MaximumTooltipWidth = 320f;
    [SerializeField] private float BadgeHeight = 30f;
    [SerializeField] private float BadgePadding = 16f;
    [SerializeField] private float PromptGap = 8f;

    [Serializable]
    private struct NameLabelEntry
    {
        public InteractionWorldSpaceNameType nameType;
        public FloatingNameLabel prefab;
    }

    private bool isReset;
    private RectTransform panelRect;
    [SerializeField] private RectTransform promptRect;
    [SerializeField] private RectTransform keycapRect;
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private Image keycapImage;
    [SerializeField] private TextMeshProUGUI keycapText;
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

        promptRect.gameObject.SetActive(false);
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

}
