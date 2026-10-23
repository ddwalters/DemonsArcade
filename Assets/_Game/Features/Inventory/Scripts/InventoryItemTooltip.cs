using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemTooltip : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI ItemName;
    [SerializeField] TextMeshProUGUI ItemDescription;
    [SerializeField] RectTransform backgroundRectTransform;
    [SerializeField] Graphic border;
    [SerializeField] EquipmentStatIconMapping[] statIcons = Array.Empty<EquipmentStatIconMapping>();
    [SerializeField, Min(120f)] float maxTooltipWidth = 244f;
    [SerializeField] Vector2 tooltipPadding = new(9f, 7f);
    [SerializeField] Vector2 cursorOffset = new(18f, 18f);

    private const float StatChipMinimumWidth = 52f;
    private const float StatChipHeight = 22f;
    private const float StatChipSpacing = 3f;
    private const int MaxStatColumns = 3;
    private const string BuffIconMaterialResourcePath = "TooltipBuffIcon";

    private readonly List<StatChipView> statChips = new();
    private static Material buffIconMaterial;

    private RectTransform rootRectTransform;
    private RectTransform statRowRectTransform;
    private TextMeshProUGUI setNameText;
    private LayoutElement statRowLayout;
    private RawImage[] borderEdges;
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
        rootRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rootRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rootRectTransform.pivot = new Vector2(0f, 1f);

        var verticalLayout = GetComponent<VerticalLayoutGroup>();
        if (verticalLayout == null)
            verticalLayout = gameObject.AddComponent<VerticalLayoutGroup>();

        verticalLayout.padding = new RectOffset(
            Mathf.RoundToInt(tooltipPadding.x),
            Mathf.RoundToInt(tooltipPadding.x),
            Mathf.RoundToInt(tooltipPadding.y),
            Mathf.RoundToInt(tooltipPadding.y));
        verticalLayout.spacing = 3f;
        verticalLayout.childAlignment = TextAnchor.UpperLeft;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = true;
        verticalLayout.childForceExpandWidth = false;
        verticalLayout.childForceExpandHeight = false;

        var sizeFitter = GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
            sizeFitter = gameObject.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        rootRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxTooltipWidth);

        if (backgroundRectTransform != null)
        {
            LayoutElement backgroundLayout = backgroundRectTransform.GetComponent<LayoutElement>();
            if (backgroundLayout == null)
                backgroundLayout = backgroundRectTransform.gameObject.AddComponent<LayoutElement>();
            backgroundLayout.ignoreLayout = true;

            backgroundRectTransform.anchorMin = Vector2.zero;
            backgroundRectTransform.anchorMax = Vector2.one;
            backgroundRectTransform.pivot = new Vector2(0.5f, 0.5f);
            backgroundRectTransform.anchoredPosition = Vector2.zero;
            backgroundRectTransform.sizeDelta = Vector2.zero;

            Graphic backgroundGraphic = backgroundRectTransform.GetComponent<Graphic>();
            if (backgroundGraphic != null)
            {
                backgroundGraphic.raycastTarget = false;
                backgroundGraphic.color = new Color(0.055f, 0.07f, 0.09f, 0.98f);
            }

            if (backgroundGraphic is RawImage backgroundRawImage)
                backgroundRawImage.texture = Texture2D.whiteTexture;

            CreateBorderEdges();
        }

        ConfigureText(ItemName, 16f, FontStyles.Bold, Color.white);
        ConfigureText(ItemDescription, 11f, FontStyles.Normal, new Color(0.82f, 0.85f, 0.89f, 1f));
        CreateStatRow();
        CreateSetNameText();
        OrderContent();
        DisableRaycastsInChildren();
        UpdateBorderThickness();
    }

    private void ConfigureText(TextMeshProUGUI text, float fontSize, FontStyles style, Color color)
    {
        if (text == null)
            return;

        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.margin = Vector4.zero;

        LayoutElement textLayout = EnsureLayoutElement(text.gameObject);
        textLayout.preferredWidth = maxTooltipWidth - tooltipPadding.x * 2f;
        textLayout.flexibleWidth = 0f;
    }

    private void CreateBorderEdges()
    {
        if (borderEdges != null || backgroundRectTransform == null)
            return;

        borderEdges = new RawImage[4];
        string[] names = { "BorderTop", "BorderBottom", "BorderLeft", "BorderRight" };
        for (int i = 0; i < borderEdges.Length; i++)
        {
            GameObject edgeObject = new GameObject(names[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            edgeObject.transform.SetParent(backgroundRectTransform, false);
            RawImage edge = edgeObject.GetComponent<RawImage>();
            edge.texture = Texture2D.whiteTexture;
            edge.raycastTarget = false;
            edge.maskable = false;
            borderEdges[i] = edge;
        }

        ConfigureEdge(borderEdges[0].rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
        ConfigureEdge(borderEdges[1].rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
        ConfigureEdge(borderEdges[2].rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f));
        ConfigureEdge(borderEdges[3].rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f));
    }

    private static void ConfigureEdge(RectTransform edge, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        edge.anchorMin = anchorMin;
        edge.anchorMax = anchorMax;
        edge.pivot = pivot;
        edge.anchoredPosition = Vector2.zero;
        edge.sizeDelta = Vector2.zero;
    }

    private void CreateStatRow()
    {
        if (statRowRectTransform != null)
            return;

        GameObject row = new GameObject("EquipmentStatChips", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
        statRowRectTransform = (RectTransform)row.transform;
        statRowRectTransform.SetParent(rootRectTransform, false);

        GridLayoutGroup grid = row.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(StatChipMinimumWidth, StatChipHeight);
        grid.spacing = new Vector2(StatChipSpacing, StatChipSpacing);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = MaxStatColumns;

        statRowLayout = row.GetComponent<LayoutElement>();
        statRowLayout.flexibleWidth = 0f;
        statRowLayout.minHeight = 0f;
    }

    private void CreateSetNameText()
    {
        if (setNameText != null)
            return;

        GameObject setLabel = new GameObject("EquipmentSetName", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        setNameText = setLabel.GetComponent<TextMeshProUGUI>();
        setNameText.font = ItemName != null ? ItemName.font : null;
        setNameText.fontSharedMaterial = ItemName != null ? ItemName.fontSharedMaterial : null;
        setNameText.fontSize = 12f;
        setNameText.color = new Color(0.67f, 0.71f, 0.76f, 1f);
        setNameText.textWrappingMode = TextWrappingModes.Normal;
        setNameText.raycastTarget = false;
        setNameText.margin = Vector4.zero;
        setNameText.transform.SetParent(rootRectTransform, false);

        LayoutElement setNameLayout = EnsureLayoutElement(setLabel);
        setNameLayout.preferredWidth = maxTooltipWidth - tooltipPadding.x * 2f;
        setNameLayout.flexibleWidth = 0f;
    }

    private void OrderContent()
    {
        if (backgroundRectTransform != null)
            backgroundRectTransform.SetSiblingIndex(0);

        if (ItemName != null)
            ItemName.transform.SetSiblingIndex(1);

        statRowRectTransform.SetSiblingIndex(2);

        if (ItemDescription != null)
            ItemDescription.transform.SetSiblingIndex(3);

        setNameText.transform.SetSiblingIndex(4);
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
                    ? new Color(1f, 0.65f, 0.62f, 1f)
                    : new Color(0.95f, 0.96f, 0.98f, 1f);
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

        UpdateStatRowLayout(maxTooltipWidth - tooltipPadding.x * 2f);

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
        GameObject chipObject = new GameObject("StatChip", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        chipObject.transform.SetParent(statRowRectTransform, false);

        HorizontalLayoutGroup chipLayout = chipObject.GetComponent<HorizontalLayoutGroup>();
        chipLayout.spacing = 1f;
        chipLayout.childAlignment = TextAnchor.MiddleCenter;
        chipLayout.childControlWidth = false;
        chipLayout.childControlHeight = false;
        chipLayout.childForceExpandWidth = false;
        chipLayout.childForceExpandHeight = false;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform iconRect = (RectTransform)iconObject.transform;
        iconRect.SetParent(chipObject.transform, false);
        iconRect.sizeDelta = new Vector2(18f, 18f);
        Image icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        if (buffIconMaterial == null)
            buffIconMaterial = Resources.Load<Material>(BuffIconMaterialResourcePath);
        icon.material = buffIconMaterial;

        GameObject valueObject = new GameObject("Value", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform valueRect = (RectTransform)valueObject.transform;
        valueRect.SetParent(chipObject.transform, false);
        valueRect.sizeDelta = new Vector2(31f, StatChipHeight);
        TextMeshProUGUI value = valueObject.GetComponent<TextMeshProUGUI>();
        value.font = ItemDescription != null ? ItemDescription.font : ItemName != null ? ItemName.font : null;
        value.fontSharedMaterial = ItemDescription != null
            ? ItemDescription.fontSharedMaterial
            : ItemName != null ? ItemName.fontSharedMaterial : null;
        value.fontSize = 12f;
        value.fontStyle = FontStyles.Bold;
        value.alignment = TextAlignmentOptions.MidlineLeft;
        value.margin = Vector4.zero;
        value.textWrappingMode = TextWrappingModes.NoWrap;
        value.raycastTarget = false;

        return new StatChipView
        {
            root = chipObject,
            icon = icon,
            value = value
        };
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

    private static Color GetStatIconColor(EquipmentStat stat)
    {
        switch (stat)
        {
            case EquipmentStat.Defense:
                return new Color(0.96f, 0.79f, 0.35f, 1f);
            case EquipmentStat.Attack:
                return new Color(0.34f, 0.86f, 0.44f, 1f);
            case EquipmentStat.ColdResistance:
                return new Color(0.35f, 0.72f, 1f, 1f);
            case EquipmentStat.HeatResistance:
                return new Color(1f, 0.55f, 0.29f, 1f);
            case EquipmentStat.FireResistance:
                return new Color(1f, 0.33f, 0.27f, 1f);
            case EquipmentStat.ShockResistance:
                return new Color(1f, 0.85f, 0.3f, 1f);
            case EquipmentStat.SlipResistance:
                return new Color(0.4f, 0.86f, 0.82f, 1f);
            case EquipmentStat.StaminaRecovery:
                return new Color(0.78f, 0.91f, 0.32f, 1f);
            default:
                return Color.white;
        }
    }

    private void ApplyBorderColor(Color color)
    {
        Color edgeColor = Color.Lerp(color, Color.white, 0.35f);
        edgeColor.a = 0.95f;
        for (int i = 0; borderEdges != null && i < borderEdges.Length; i++)
            borderEdges[i].color = edgeColor;

        if (ItemName != null)
            ItemName.color = Color.Lerp(color, Color.white, 0.3f);

        if (border != null)
        {
            border.raycastTarget = false;
            if (border is RawImage rawImage)
                rawImage.texture = Texture2D.whiteTexture;
        }
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
        SetPreferredWidth(ItemName, targetWidth - tooltipPadding.x * 2f);
        SetPreferredWidth(ItemDescription, targetWidth - tooltipPadding.x * 2f);
        SetPreferredWidth(setNameText, targetWidth - tooltipPadding.x * 2f);
        UpdateStatRowLayout(targetWidth - tooltipPadding.x * 2f);

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
        float valueWidth = 31f;
        for (int i = 0; i < activeStatCount; i++)
        {
            TextMeshProUGUI value = statChips[i].value;
            valueWidth = Mathf.Max(valueWidth, value.GetPreferredValues(value.text).x + 4f);
        }

        for (int i = 0; i < activeStatCount; i++)
            statChips[i].value.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, valueWidth);

        float cellWidth = Mathf.Max(StatChipMinimumWidth, 18f + 2f + valueWidth);
        int columns = activeStatCount == 0
            ? 1
            : Mathf.Clamp(Mathf.FloorToInt((availableWidth + StatChipSpacing) / (cellWidth + StatChipSpacing)),
                1,
                Mathf.Min(activeStatCount, MaxStatColumns));
        int rows = activeStatCount == 0 ? 0 : Mathf.CeilToInt((float)activeStatCount / columns);

        GridLayoutGroup grid = statRowRectTransform.GetComponent<GridLayoutGroup>();
        grid.constraintCount = columns;
        grid.cellSize = new Vector2(cellWidth, StatChipHeight);
        statRowLayout.preferredWidth = columns * cellWidth + (columns - 1) * StatChipSpacing;
        statRowLayout.preferredHeight = rows * StatChipHeight + Mathf.Max(0, rows - 1) * StatChipSpacing;
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

    private void DisableRaycastsInChildren()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;
    }

    private static LayoutElement EnsureLayoutElement(GameObject target)
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = target.AddComponent<LayoutElement>();

        return layoutElement;
    }

    private static string FormatPoints(int points) => points > 0 ? $"+{points}" : points.ToString();
}

[Serializable]
public struct EquipmentStatIconMapping
{
    public EquipmentStat stat;
    public Sprite icon;
}
