using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemTooltip : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI ItemName;
    [SerializeField] TextMeshProUGUI ItemDescription;
    [SerializeField] EquipmentStatIconMapping[] statIcons = Array.Empty<EquipmentStatIconMapping>();
    private float maxTooltipWidth;
    private float HorizontalPadding => GetComponent<VerticalLayoutGroup>().padding.horizontal;
    [SerializeField] Vector2 cursorOffset = new(18f, 18f);

    [SerializeField] private Color positiveValueColor = new(0.95f, 0.96f, 0.98f, 1f);
    [SerializeField] private Color negativeValueColor = new(1f, 0.65f, 0.62f, 1f);
    private Vector2 statCellSize;
    private int maximumStatColumns;

    private readonly List<StatChipView> statChips = new();

    private RectTransform rootRectTransform;
    [SerializeField] private RectTransform statRowRectTransform;
    [SerializeField] private TextMeshProUGUI setNameText;
    [SerializeField] private LayoutElement statRowLayout;
    [SerializeField] private Graphic[] borderEdges;
    [SerializeField] private InventoryStatChip statChipPrefab;
    private Item currentOwner;
    private EquipmentTooltipViewModel currentViewModel;
    private float lastCanvasScaleFactor = -1f;
    private int activeStatCount;

    private sealed class StatChipView
    {
        public GameObject root;
        public Image icon;
        public TextMeshProUGUI value;
    }

    private void Awake()
    {
        InitializeLayout();
        if (currentViewModel == null)
            gameObject.SetActive(false);
    }

    private void Update()
    {
        if (currentViewModel == null)
        {
            gameObject.SetActive(false);
            return;
        }

        UpdateBorderThickness();
        PositionAtPointer();
    }

    private void OnDisable()
    {
        currentViewModel = null;
        currentOwner = null;
    }

    public void ShowToolTip(EquipmentTooltipViewModel viewModel, Color borderColor, Item owner = null)
    {
        if (viewModel == null)
        {
            HideToolTip();
            return;
        }

        InitializeLayout();
        currentViewModel = viewModel;
        currentOwner = owner;

        ApplyViewModel(viewModel);
        ApplyBorderColor(borderColor);
        gameObject.SetActive(true);

        transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rootRectTransform);
        PositionAtPointer();
    }

    public void ShowToolTip(string itemName, string itemStatsDescription, Color borderColor)
    {
        var viewModel = new EquipmentTooltipViewModel
        {
            ItemName = itemName,
            DetailText = itemStatsDescription
        };

        ShowToolTip(viewModel, borderColor);
    }

    public void HideToolTip(Item owner)
    {
        if (owner != null && currentOwner != owner)
            return;

        HideToolTip();
    }

    public void HideToolTip()
    {
        currentViewModel = null;
        currentOwner = null;
        gameObject.SetActive(false);
    }

    private void InitializeLayout()
    {
        if (rootRectTransform != null)
            return;

        rootRectTransform = (RectTransform)transform;
        maxTooltipWidth = rootRectTransform.rect.width;
        statCellSize = statRowRectTransform.GetComponent<GridLayoutGroup>().cellSize;
        maximumStatColumns = statRowRectTransform.GetComponent<GridLayoutGroup>().constraintCount;
    }

    private void ApplyViewModel(EquipmentTooltipViewModel viewModel)
    {
        if (ItemName != null)
            ItemName.text = viewModel.ItemName ?? string.Empty;

        var detailLines = new List<string>();
        if (!string.IsNullOrWhiteSpace(viewModel.DetailText))
            detailLines.Add(viewModel.DetailText);

        int mappedStatCount = 0;
        foreach (EquipmentTooltipStat stat in viewModel.Stats)
        {
            if (TryGetIcon(stat.Stat, out Sprite icon))
            {
                StatChipView chip = GetStatChip(mappedStatCount++);
                chip.icon.sprite = icon;
                chip.icon.color = GetStatIconColor(stat.Stat);
                chip.value.text = FormatPoints(stat.Value);
                chip.value.color = stat.Value < 0
                    ? negativeValueColor
                    : positiveValueColor;
                chip.root.SetActive(true);
            }
            else
            {
                detailLines.Add(stat.Label + " " + FormatPoints(stat.Value));
            }
        }

        for (int i = mappedStatCount; i < statChips.Count; i++)
            statChips[i].root.SetActive(false);

        activeStatCount = mappedStatCount;
        statRowRectTransform.gameObject.SetActive(mappedStatCount > 0);

        UpdateStatRowLayout(maxTooltipWidth - HorizontalPadding);

        if (ItemDescription != null)
        {
            ItemDescription.text = string.Join("\n", detailLines);
            ItemDescription.gameObject.SetActive(!string.IsNullOrWhiteSpace(ItemDescription.text));
        }

        if (setNameText != null)
        {
            setNameText.text = string.IsNullOrWhiteSpace(viewModel.SetName) ? string.Empty : viewModel.SetName;
            setNameText.gameObject.SetActive(!string.IsNullOrWhiteSpace(setNameText.text));
        }
    }

    private StatChipView GetStatChip(int index)
    {
        while (statChips.Count <= index)
            statChips.Add(CreateStatChip());

        return statChips[index];
    }

    private StatChipView CreateStatChip()
    {
        var chip = Instantiate(statChipPrefab, statRowRectTransform);
        return new StatChipView { root = chip.gameObject, icon = chip.Icon, value = chip.Value };
    }

    private bool TryGetIcon(EquipmentStat stat, out Sprite icon)
    {
        int passCount = stat == EquipmentStat.FireResistance ? 2 : 1;
        for (int pass = 0; pass < passCount; pass++)
        {
            EquipmentStat candidate = pass == 0 ? stat : EquipmentStat.HeatResistance;
            for (int i = 0; statIcons != null && i < statIcons.Length; i++)
            {
                if (statIcons[i].stat != candidate || statIcons[i].icon == null)
                    continue;

                icon = statIcons[i].icon;
                return true;
            }
        }

        icon = null;
        return false;
    }

    private Color GetStatIconColor(EquipmentStat stat)
    {
        foreach (var mapping in statIcons)
            if (mapping.stat == stat)
                return mapping.color;
        if (stat == EquipmentStat.FireResistance)
            foreach (var mapping in statIcons)
                if (mapping.stat == EquipmentStat.HeatResistance)
                    return mapping.color;
        return Color.white;
    }

    private void ApplyBorderColor(Color color)
    {
        Color edgeColor = Color.Lerp(color, Color.white, 0.35f);
        edgeColor.a = 0.95f;
        for (int i = 0; borderEdges != null && i < borderEdges.Length; i++)
            borderEdges[i].color = edgeColor;

        if (ItemName != null)
            ItemName.color = Color.Lerp(color, Color.white, 0.3f);
    }

    private void UpdateBorderThickness()
    {
        if (borderEdges == null || borderEdges.Length == 0)
            return;

        Canvas rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        float scaleFactor = rootCanvas != null ? Mathf.Max(rootCanvas.scaleFactor, 0.01f) : 1f;
        if (Mathf.Approximately(scaleFactor, lastCanvasScaleFactor))
            return;

        lastCanvasScaleFactor = scaleFactor;
        float thickness = 1f / scaleFactor;
        borderEdges[0].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, thickness);
        borderEdges[1].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, thickness);
        borderEdges[2].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, thickness);
        borderEdges[3].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, thickness);
    }

    private void PositionAtPointer()
    {
        Canvas rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        RectTransform parentRect = rootRectTransform.parent as RectTransform;
        if (rootCanvas == null || parentRect == null)
            return;

        Camera eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        Vector2 pointerPosition = MenuInput.PointerPosition + cursorOffset;
        Rect safeArea = Intersect(Screen.safeArea, rootCanvas.pixelRect);

        float scaleFactor = Mathf.Max(rootCanvas.scaleFactor, 0.01f);
        float targetWidth = Mathf.Min(maxTooltipWidth, safeArea.width / scaleFactor);
        rootRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        SetPreferredWidth(ItemName, targetWidth - HorizontalPadding);
        SetPreferredWidth(ItemDescription, targetWidth - HorizontalPadding);
        SetPreferredWidth(setNameText, targetWidth - HorizontalPadding);
        UpdateStatRowLayout(targetWidth - HorizontalPadding);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rootRectTransform);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, pointerPosition, eventCamera, out Vector2 localPoint))
        {
            Vector3 localPosition = rootRectTransform.localPosition;
            localPosition.x = localPoint.x;
            localPosition.y = localPoint.y;
            rootRectTransform.localPosition = localPosition;
        }

        GetScreenBounds(eventCamera, out Rect bounds);
        Vector2 correction = Vector2.zero;
        if (bounds.xMin < safeArea.xMin)
            correction.x = safeArea.xMin - bounds.xMin;
        else if (bounds.xMax > safeArea.xMax)
            correction.x = safeArea.xMax - bounds.xMax;

        if (bounds.yMin < safeArea.yMin)
            correction.y = safeArea.yMin - bounds.yMin;
        else if (bounds.yMax > safeArea.yMax)
            correction.y = safeArea.yMax - bounds.yMax;

        if (correction != Vector2.zero &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, pointerPosition + correction, eventCamera, out localPoint))
        {
            Vector3 localPosition = rootRectTransform.localPosition;
            localPosition.x = localPoint.x;
            localPosition.y = localPoint.y;
            rootRectTransform.localPosition = localPosition;
        }
    }

    private static void SetPreferredWidth(TextMeshProUGUI text, float width)
    {
        if (text == null)
            return;

        LayoutElement layoutElement = text.GetComponent<LayoutElement>();
        if (layoutElement != null)
            layoutElement.preferredWidth = Mathf.Max(24f, width);
    }

    private void UpdateStatRowLayout(float availableWidth)
    {
        float valueWidth = statChipPrefab.Value.rectTransform.rect.width;
        for (int i = 0; i < activeStatCount; i++)
        {
            TextMeshProUGUI value = statChips[i].value;
            valueWidth = Mathf.Max(valueWidth, value.GetPreferredValues(value.text).x + 4f);
        }

        for (int i = 0; i < activeStatCount; i++)
            statChips[i].value.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, valueWidth);

        var grid = statRowRectTransform.GetComponent<GridLayoutGroup>();
        float spacing = grid.spacing.x;
        float iconAndGap = statChipPrefab.Icon.rectTransform.rect.width +
            statChipPrefab.GetComponent<HorizontalLayoutGroup>().spacing;
        float cellWidth = Mathf.Max(statCellSize.x, iconAndGap + valueWidth);
        int columns = activeStatCount == 0
            ? 1
            : Mathf.Clamp(Mathf.FloorToInt((availableWidth + spacing) / (cellWidth + spacing)),
                1,
                Mathf.Min(activeStatCount, maximumStatColumns));
        int rows = activeStatCount == 0 ? 0 : Mathf.CeilToInt((float)activeStatCount / columns);

        grid.constraintCount = columns;
        grid.cellSize = new Vector2(cellWidth, statCellSize.y);
        statRowLayout.preferredWidth = columns * cellWidth + (columns - 1) * spacing;
        statRowLayout.preferredHeight = rows * statCellSize.y + Mathf.Max(0, rows - 1) * grid.spacing.y;
        statRowLayout.minHeight = statRowLayout.preferredHeight;
    }

    private void GetScreenBounds(Camera eventCamera, out Rect bounds)
    {
        Vector3[] corners = new Vector3[4];
        rootRectTransform.GetWorldCorners(corners);
        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]);
        bounds = Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
    }

    private static Rect Intersect(Rect first, Rect second)
    {
        float xMin = Mathf.Max(first.xMin, second.xMin);
        float yMin = Mathf.Max(first.yMin, second.yMin);
        float xMax = Mathf.Min(first.xMax, second.xMax);
        float yMax = Mathf.Min(first.yMax, second.yMax);
        return Rect.MinMaxRect(xMin, yMin, Mathf.Max(xMin, xMax), Mathf.Max(yMin, yMax));
    }

    private static string FormatPoints(int points) => points > 0 ? $"+{points}" : points.ToString();
}

[Serializable]
public struct EquipmentStatIconMapping
{
    public EquipmentStat stat;
    public Sprite icon;
    public Color color;
}
