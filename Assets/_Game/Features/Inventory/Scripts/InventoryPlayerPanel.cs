using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the player's inventory card around the existing inventory prefab grids.
/// The grid objects stay intact so their pointer, item, and tooltip behavior remains unchanged.
/// </summary>
public class InventoryPlayerPanel : MonoBehaviour
{
    private const float SlotSize = 72f;

    private static readonly Color CardColor = new(0.055f, 0.082f, 0.12f, 0.98f);
    private static readonly Color SurfaceColor = new(0.085f, 0.12f, 0.17f, 0.96f);
    private static readonly Color MutedColor = new(0.48f, 0.57f, 0.66f, 1f);
    private static readonly Color WarmWhite = new(0.91f, 0.88f, 0.81f, 1f);
    private static readonly Color AccentColor = new(0.77f, 0.62f, 0.36f, 1f);

    private readonly Dictionary<int, RectTransform> slotParents = new();
    private readonly Dictionary<int, TextMeshProUGUI> slotLabels = new();
    private readonly List<InventoryGrid> equipmentGrids = new();

    private RectTransform panelRect;
    private RectTransform headerTitleRect;
    private RectTransform activeTabRect;
    private RectTransform previewFrameRect;
    private RectTransform xpPanelRect;
    private RectTransform weaponsTitleRect;
    private RectTransform bagTitleRect;
    private RectTransform bagDividerRect;
    private RectTransform bagViewportRect;
    private RectTransform bagContentRect;
    private RectTransform scrollbarRect;
    private RectTransform portraitFrameRect;
    private RectTransform activePortraitRect;
    private RectTransform characterNameRect;
    private RectTransform xpFillRect;
    private InventoryGrid bagGrid;
    private RawImage previewTarget;
    private RawImage activePortrait;
    private Image xpFill;
    private TextMeshProUGUI characterNameLabel;
    private TextMeshProUGUI goldLabel;
    private TextMeshProUGUI levelLabel;
    private TextMeshProUGUI xpLabel;
    private TextMeshProUGUI portraitFallbackLabel;
    private ScrollRect bagScrollRect;
    private PlayerController player;
    private PlayerStatsManager playerStats;
    private InventoryManager inventoryManager;
    private bool splitView;

    public IReadOnlyList<InventoryGrid> EquipmentGrids => equipmentGrids;
    public RawImage PreviewTarget => previewTarget;
    public Vector2 PreferredSize => splitView ? new Vector2(620f, 840f) : new Vector2(1000f, 840f);

    public void Initialize(GameObject bagRoot, InventoryGrid bagGrid,
        IEnumerable<InventoryGrid> sourceEquipmentGrids, InventoryManager manager, PlayerController playerController)
    {
        panelRect = (RectTransform)transform;
        inventoryManager = manager;
        player = playerController;
        playerStats = player != null ? player.GetComponent<PlayerStatsManager>() : null;
        this.bagGrid = bagGrid;

        equipmentGrids.Clear();
        equipmentGrids.AddRange(sourceEquipmentGrids.Where(grid => grid != null && grid != bagGrid));
        AddRingGridsFromAmuletTemplate();

        CreateCardBackground();
        CreateHeader();
        CreateEquipmentArea();
        CreateBagArea(bagRoot, bagGrid);
        CreateProgressArea();
        CreateWeaponArea();
        CreatePreview();

        CharacterInventoryPreview preview = GetComponent<CharacterInventoryPreview>();
        if (preview == null)
            preview = gameObject.AddComponent<CharacterInventoryPreview>();
        preview.Initialize(previewTarget, player);

        ApplyResponsiveLayout(false);
        RefreshHeaderAndProgress();
    }

    public void ApplyResponsiveLayout(bool useSplitView)
    {
        splitView = useSplitView;
        if (panelRect == null)
            return;

        panelRect.sizeDelta = PreferredSize;
        bool compact = splitView;

        SetRect(headerTitleRect, new Vector2(compact ? -225f : -395f, 372f),
            new Vector2(compact ? 132f : 180f, 36f));
        SetRect(activeTabRect, new Vector2(0f, 372f), new Vector2(compact ? 192f : 220f, 52f));
        SetRect(goldLabel != null ? goldLabel.rectTransform : null,
            new Vector2(compact ? 225f : 410f, 372f), new Vector2(compact ? 132f : 150f, 36f));
        SetRect(portraitFrameRect, new Vector2(compact ? -70f : -80f, 0f), new Vector2(40f, 40f));
        SetRect(characterNameRect, new Vector2(compact ? 15f : 24f, 0f),
            new Vector2(compact ? 112f : 138f, 36f));

        SetRect(previewFrameRect, new Vector2(0f, 116f),
            compact ? new Vector2(250f, 270f) : new Vector2(448f, 304f));

        float leftX = compact ? -248f : -382f;
        float rightX = compact ? 248f : 382f;
        float firstArmorY = compact ? 214f : 226f;
        float rowGap = compact ? 102f : 100f;
        PositionSlot(1, leftX, firstArmorY);
        PositionSlot(2, leftX, firstArmorY - rowGap);
        PositionSlot(3, leftX, firstArmorY - rowGap * 2f);
        PositionSlot(4, leftX, firstArmorY - rowGap * 3f);

        PositionSlot(5, rightX, compact ? 166f : 190f);
        PositionSlot(8, rightX, compact ? 66f : 90f);
        PositionSlot(9, rightX, compact ? -34f : -10f);

        SetRect(xpPanelRect, new Vector2(0f, -76f), new Vector2(compact ? 290f : 390f, 44f));
        SetRect(weaponsTitleRect, new Vector2(compact ? -156f : -220f, -146f),
            new Vector2(compact ? 70f : 90f, 22f));
        PositionSlot(6, compact ? -64f : -66f, -140f);
        PositionSlot(7, compact ? 4f : 14f, -140f);

        SetRect(bagTitleRect, new Vector2(compact ? -225f : -395f, -222f),
            new Vector2(compact ? 160f : 200f, 28f));
        SetRect(bagDividerRect, new Vector2(0f, -192f), new Vector2(compact ? 580f : 960f, 1f));
        SetRect(bagViewportRect, new Vector2(0f, -326f),
            new Vector2(compact ? 520f : 900f, compact ? 160f : 180f));
        SetRect(scrollbarRect, new Vector2(compact ? 279f : 469f, -326f), new Vector2(8f, compact ? 160f : 180f));

        if (bagContentRect != null && bagGrid != null)
            bagContentRect.sizeDelta = new Vector2(bagGrid.gridSize.x * SlotSize, bagGrid.gridSize.y * SlotSize);

        LayoutEquipmentSlots(compact);
        LayoutLabels(compact);
        if (bagScrollRect != null)
            bagScrollRect.verticalNormalizedPosition = 1f;
    }

    private void AddRingGridsFromAmuletTemplate()
    {
        InventoryGrid amulet = equipmentGrids.FirstOrDefault(grid => grid.id == (int)EquipmentSlot.Necklace);
        if (amulet == null || inventoryManager == null)
            return;

        for (int id = (int)EquipmentSlot.RingLeft; id <= (int)EquipmentSlot.RingRight; id++)
        {
            if (equipmentGrids.Any(grid => grid.id == id))
                continue;

            // Ring grids are data-backed and created by InventoryManager. Clone the existing
            // amulet slot artwork so the ring positions use the same established slot style.
            inventoryManager.GetItems(id);
            GameObject ringObject = Instantiate(amulet.gameObject, amulet.transform.parent);
            ringObject.name = id == (int)EquipmentSlot.RingLeft ? "LeftRingSlot" : "RightRingSlot";
            InventoryGrid ringGrid = ringObject.GetComponentInChildren<InventoryGrid>(true);
            ringGrid.id = id;
            ringGrid.gridSize = Vector2Int.one;
            equipmentGrids.Add(ringGrid);
        }
    }

    private void CreateCardBackground()
    {
        Image background = GetComponent<Image>();
        if (background == null)
            background = gameObject.AddComponent<Image>();
        background.color = CardColor;
        background.raycastTarget = false;

        Shadow shadow = gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(0f, -8f);
        shadow.useGraphicAlpha = true;
    }

    private void CreateHeader()
    {
        headerTitleRect = CreateText("InventoryTitle", "INVENTORY", 15f, AccentColor,
            TextAlignmentOptions.Left, transform).rectTransform;

        RectTransform tab = CreateRect("ActivePlayerTab", transform, new Vector2(220f, 52f), new Vector2(0f, 372f));
        activeTabRect = tab;
        Image tabBackground = tab.gameObject.AddComponent<Image>();
        tabBackground.color = SurfaceColor;
        tabBackground.raycastTarget = false;

        RectTransform portraitFrame = CreateRect("PortraitFrame", tab, new Vector2(40f, 40f), new Vector2(-82f, 0f));
        portraitFrameRect = portraitFrame;
        Image portraitBackground = portraitFrame.gameObject.AddComponent<Image>();
        portraitBackground.color = new Color(0.14f, 0.19f, 0.24f, 1f);
        portraitBackground.raycastTarget = false;
        activePortraitRect = CreateRect("PlayerPortrait", portraitFrame, new Vector2(36f, 36f), Vector2.zero);
        activePortrait = activePortraitRect.gameObject.AddComponent<RawImage>();
        activePortrait.raycastTarget = false;
        portraitFallbackLabel = CreateText("PortraitFallback", "P", 19f, WarmWhite,
            TextAlignmentOptions.Center, portraitFrame);
        portraitFallbackLabel.rectTransform.sizeDelta = new Vector2(40f, 40f);

        characterNameLabel = CreateText("CharacterName", GetCharacterName(), 17f, WarmWhite,
            TextAlignmentOptions.Left, tab);
        characterNameRect = characterNameLabel.rectTransform;
        characterNameLabel.overflowMode = TextOverflowModes.Ellipsis;

        goldLabel = CreateText("GoldValue", string.Empty, 16f, WarmWhite,
            TextAlignmentOptions.Right, transform);
        goldLabel.raycastTarget = false;
    }

    private void CreateEquipmentArea()
    {
        CreateSlot(1, "HELMET");
        CreateSlot(2, "CHEST");
        CreateSlot(3, "LEGS");
        CreateSlot(4, "BOOTS");
        CreateSlot(5, "AMULET");
        CreateSlot(8, "RING");
        CreateSlot(9, "RING");
        CreateSlot(6, "RIGHT HAND");
        CreateSlot(7, "LEFT HAND");

        foreach (InventoryGrid grid in equipmentGrids)
        {
            if (!slotParents.TryGetValue(grid.id, out RectTransform slotParent))
                continue;

            RectTransform gridRoot = grid.transform as RectTransform;
            gridRoot.SetParent(slotParent, false);
            gridRoot.anchorMin = new Vector2(0.5f, 0.5f);
            gridRoot.anchorMax = new Vector2(0.5f, 0.5f);
            gridRoot.pivot = new Vector2(0.5f, 0.5f);
            gridRoot.anchoredPosition = Vector2.zero;
            gridRoot.sizeDelta = new Vector2(SlotSize, SlotSize);
            gridRoot.localScale = Vector3.one;

            RectTransform gridRect = grid.rectTransform;
            gridRect.anchorMin = new Vector2(0f, 1f);
            gridRect.anchorMax = new Vector2(0f, 1f);
            gridRect.pivot = new Vector2(0f, 1f);
            gridRect.anchoredPosition = Vector2.zero;
            gridRect.localScale = Vector3.one;
            gridRect.sizeDelta = new Vector2(SlotSize, SlotSize);
        }
    }

    private void CreateSlot(int id, string title)
    {
        RectTransform slotParent = CreateRect("EquipmentSlot" + id, transform,
            new Vector2(SlotSize, SlotSize), Vector2.zero);
        slotParents[id] = slotParent;

        TextMeshProUGUI label = CreateText("EquipmentLabel" + id, title, 10f, MutedColor,
            TextAlignmentOptions.Center, transform);
        label.rectTransform.sizeDelta = new Vector2(92f, 18f);
        slotLabels[id] = label;
    }

    private void CreateBagArea(GameObject bagRoot, InventoryGrid bagGrid)
    {
        bagTitleRect = CreateText("BagTitle", $"BAG   ·   {bagGrid.gridSize.x} × {bagGrid.gridSize.y}", 13f, WarmWhite,
            TextAlignmentOptions.Left, transform).rectTransform;
        Image divider = CreateRect("BagSectionDivider", transform, new Vector2(960f, 1f), new Vector2(0f, -192f))
            .gameObject.AddComponent<Image>();
        divider.color = new Color(0.34f, 0.4f, 0.46f, 0.72f);
        divider.raycastTarget = false;
        bagDividerRect = divider.rectTransform;

        bagViewportRect = CreateRect("BagViewport", transform, new Vector2(900f, 180f), new Vector2(0f, -326f));
        Image viewportImage = bagViewportRect.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(0.035f, 0.055f, 0.08f, 0.78f);
        viewportImage.raycastTarget = true;
        bagViewportRect.gameObject.AddComponent<RectMask2D>();

        bagScrollRect = bagViewportRect.gameObject.AddComponent<ScrollRect>();
        bagScrollRect.horizontal = false;
        bagScrollRect.vertical = true;
        bagScrollRect.movementType = ScrollRect.MovementType.Clamped;
        bagScrollRect.inertia = true;
        bagScrollRect.scrollSensitivity = 44f;
        bagScrollRect.viewport = bagViewportRect;

        bagContentRect = CreateRect("BagContent", bagViewportRect,
            new Vector2(bagGrid.gridSize.x * SlotSize, bagGrid.gridSize.y * SlotSize), Vector2.zero);
        bagContentRect.anchorMin = new Vector2(0.5f, 1f);
        bagContentRect.anchorMax = new Vector2(0.5f, 1f);
        bagContentRect.pivot = new Vector2(0.5f, 1f);
        bagContentRect.anchoredPosition = Vector2.zero;
        bagScrollRect.content = bagContentRect;

        RectTransform bagRootRect = bagRoot.transform as RectTransform;
        bagRootRect.SetParent(bagContentRect, false);
        bagRootRect.anchorMin = new Vector2(0.5f, 1f);
        bagRootRect.anchorMax = new Vector2(0.5f, 1f);
        bagRootRect.pivot = new Vector2(0.5f, 1f);
        bagRootRect.anchoredPosition = Vector2.zero;
        bagRootRect.localScale = Vector3.one;
        bagGrid.rectTransform.anchorMin = new Vector2(0f, 1f);
        bagGrid.rectTransform.anchorMax = new Vector2(0f, 1f);
        bagGrid.rectTransform.pivot = new Vector2(0f, 1f);
        bagGrid.rectTransform.anchoredPosition = Vector2.zero;
        bagGrid.rectTransform.localScale = Vector3.one;

        scrollbarRect = CreateRect("BagScrollbar", transform, new Vector2(8f, 180f), new Vector2(469f, -326f));
        Image track = scrollbarRect.gameObject.AddComponent<Image>();
        track.color = new Color(0.15f, 0.19f, 0.23f, 1f);
        track.raycastTarget = false;

        RectTransform handle = CreateRect("ScrollbarHandle", scrollbarRect, new Vector2(8f, 42f), Vector2.zero);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = AccentColor;
        Scrollbar scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handleImage;
        scrollbar.numberOfSteps = 0;
        bagScrollRect.verticalScrollbar = scrollbar;
        bagScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        bagScrollRect.verticalScrollbarSpacing = -8f;
    }

    private void CreateProgressArea()
    {
        xpPanelRect = CreateRect("XPPanel", transform, new Vector2(390f, 44f), new Vector2(0f, -76f));
        Image panel = xpPanelRect.gameObject.AddComponent<Image>();
        panel.color = SurfaceColor;
        panel.raycastTarget = false;

        levelLabel = CreateText("LevelLabel", "LV 1", 13f, AccentColor,
            TextAlignmentOptions.Left, xpPanelRect);
        levelLabel.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        levelLabel.rectTransform.anchorMax = new Vector2(0.3f, 1f);
        levelLabel.rectTransform.pivot = new Vector2(0f, 0.5f);
        levelLabel.rectTransform.offsetMin = new Vector2(10f, 1f);
        levelLabel.rectTransform.offsetMax = new Vector2(-2f, -1f);

        xpLabel = CreateText("XPLabel", "0 / 500 XP", 11f, WarmWhite,
            TextAlignmentOptions.Right, xpPanelRect);
        xpLabel.rectTransform.anchorMin = new Vector2(0.42f, 0.5f);
        xpLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
        xpLabel.rectTransform.pivot = new Vector2(1f, 0.5f);
        xpLabel.rectTransform.offsetMin = new Vector2(0f, 1f);
        xpLabel.rectTransform.offsetMax = new Vector2(-10f, -1f);

        RectTransform xpTrackRect = CreateRect("XPTrack", xpPanelRect,
            new Vector2(350f, 7f), new Vector2(0f, -11f));
        xpTrackRect.anchorMin = new Vector2(0f, 0f);
        xpTrackRect.anchorMax = new Vector2(1f, 0f);
        xpTrackRect.pivot = new Vector2(0.5f, 0f);
        xpTrackRect.offsetMin = new Vector2(10f, 8f);
        xpTrackRect.offsetMax = new Vector2(-10f, 15f);
        Image xpTrack = xpTrackRect.gameObject.AddComponent<Image>();
        xpTrack.color = new Color(0.025f, 0.035f, 0.055f, 1f);
        xpTrack.raycastTarget = false;

        xpFillRect = CreateRect("XPFill", xpTrackRect, new Vector2(0f, 7f), Vector2.zero);
        xpFillRect.anchorMin = new Vector2(0f, 0f);
        xpFillRect.anchorMax = new Vector2(0f, 1f);
        xpFillRect.pivot = new Vector2(0f, 0.5f);
        xpFillRect.anchoredPosition = Vector2.zero;
        xpFillRect.offsetMin = Vector2.zero;
        xpFillRect.offsetMax = Vector2.zero;
        xpFill = xpFillRect.gameObject.AddComponent<Image>();
        xpFill.color = AccentColor;
        xpFill.raycastTarget = false;
    }

    private void CreateWeaponArea()
    {
        weaponsTitleRect = CreateText("WeaponSlotsTitle", "HANDS", 9f, MutedColor,
            TextAlignmentOptions.Right, transform).rectTransform;
    }

    private void CreatePreview()
    {
        previewFrameRect = CreateRect("CharacterPreviewFrame", transform,
            new Vector2(448f, 304f), new Vector2(0f, 116f));
        Image frame = previewFrameRect.gameObject.AddComponent<Image>();
        frame.color = new Color(0.045f, 0.065f, 0.09f, 1f);
        frame.raycastTarget = false;

        RectTransform previewRect = CreateRect("CharacterPreview", previewFrameRect,
            new Vector2(432f, 288f), Vector2.zero);
        previewRect.anchorMin = Vector2.zero;
        previewRect.anchorMax = Vector2.one;
        previewRect.pivot = new Vector2(0.5f, 0.5f);
        previewRect.offsetMin = new Vector2(8f, 8f);
        previewRect.offsetMax = new Vector2(-8f, -8f);
        previewTarget = previewRect.gameObject.AddComponent<RawImage>();
        previewTarget.color = Color.white;
        previewTarget.raycastTarget = false;
    }

    private void LayoutEquipmentSlots(bool compact)
    {
        foreach (InventoryGrid grid in equipmentGrids)
        {
            if (!slotParents.TryGetValue(grid.id, out RectTransform slotParent))
                continue;

            float slotScale = grid.id == 6 || grid.id == 7 ? (compact ? 0.7f : 0.76f) : 1f;
            slotParent.localScale = Vector3.one * slotScale;
        }
    }

    private void LayoutLabels(bool compact)
    {
        foreach (var entry in slotLabels)
        {
            int id = entry.Key;
            TextMeshProUGUI label = entry.Value;
            RectTransform slotParent = slotParents[id];
            if (id == 6 || id == 7)
            {
                label.rectTransform.anchoredPosition = slotParent.anchoredPosition + new Vector2(0f, -36f);
                label.rectTransform.sizeDelta = new Vector2(compact ? 62f : 76f, 16f);
            }
            else if (id >= 8 || id == 5)
            {
                label.rectTransform.anchoredPosition = slotParent.anchoredPosition + new Vector2(0f, -46f);
                label.rectTransform.sizeDelta = new Vector2(82f, 16f);
            }
            else
            {
                label.rectTransform.anchoredPosition = slotParent.anchoredPosition + new Vector2(0f, -46f);
                label.rectTransform.sizeDelta = new Vector2(82f, 16f);
            }
        }
    }

    private void PositionSlot(int id, float x, float y)
    {
        if (!slotParents.TryGetValue(id, out RectTransform slotParent))
            return;

        slotParent.anchoredPosition = new Vector2(x, y);
    }

    private void RefreshHeaderAndProgress()
    {
        if (player != null)
        {
            if (goldLabel != null)
                goldLabel.text = player.goldAmount.ToString("N0") + "  GOLD";

            if (activePortrait != null && previewTarget != null && previewTarget.texture != null)
            {
                activePortrait.texture = previewTarget.texture;
                if (portraitFallbackLabel != null)
                    portraitFallbackLabel.gameObject.SetActive(false);
            }
            else if (portraitFallbackLabel != null)
            {
                string name = GetCharacterName();
                portraitFallbackLabel.text = string.IsNullOrEmpty(name) ? "P" : name.Substring(0, 1).ToUpperInvariant();
                portraitFallbackLabel.gameObject.SetActive(true);
            }
        }

        if (characterNameLabel != null)
            characterNameLabel.text = GetCharacterName();

        if (playerStats == null)
            return;

        PlayerStatsData stats = playerStats.GetPlayerStats();
        if (stats == null)
            return;

        int needed = Mathf.Max(1, stats.nextLevelXP);
        if (levelLabel != null)
            levelLabel.text = "LV " + stats.lvl;
        if (xpLabel != null)
            xpLabel.text = stats.currentXP.ToString("N0") + " / " + needed.ToString("N0") + " XP";
        if (xpFillRect != null)
        {
            float width = xpFillRect.parent is RectTransform track ? track.rect.width : 0f;
            xpFillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                width * Mathf.Clamp01((float)stats.currentXP / needed));
        }
    }

    private string GetCharacterName()
    {
        string characterName = SaveManager.Instance != null
            ? SaveManager.Instance.ActivePlaythrough?.characterName
            : null;
        return string.IsNullOrWhiteSpace(characterName) ? "Player" : characterName;
    }

    private TextMeshProUGUI CreateText(string name, string value, float fontSize, Color color,
        TextAlignmentOptions alignment, Transform parent)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = 5;
        RectTransform rect = (RectTransform)textObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(180f, 28f);

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = value;
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.enableAutoSizing = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        GameObject child = new(name, typeof(RectTransform));
        child.layer = 5;
        RectTransform rect = (RectTransform)child.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null)
            return;

        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void Update() => RefreshHeaderAndProgress();
}
