using System.Collections.Generic;
using TMPro;
using UnityEngine;

// All layout, artwork, fonts, colors and slot positions are authored in the prefab.
public sealed class InventoryPlayerPanel : MonoBehaviour
{
    [SerializeField] private InventoryGrid bagGrid;
    [SerializeField] private InventoryGrid[] equipmentGrids;
    [SerializeField] private RectTransform bagViewport;
    [SerializeField] private RectTransform bagContent;
    [SerializeField] private RectTransform bagArtwork;
    [SerializeField] private Transform tabHolder;
    [SerializeField] private InventoryMemberTab tabPrefab;
    [SerializeField] private PartyPortraitView portrait;
    [SerializeField] private CharacterInventoryPreview preview;
    [SerializeField] private UnityEngine.UI.RawImage previewTarget;
    [SerializeField] private TextMeshProUGUI characterNameLabel;
    [SerializeField] private TextMeshProUGUI levelLabel;
    [SerializeField] private TextMeshProUGUI xpLabel;
    [SerializeField] private RectTransform xpFill;
    private readonly List<InventoryMemberTab> tabs = new();
    private Inventory owner;
    private InventoryManager inventory;
    private CharacterRecord member;
    private string roster;

    public InventoryGrid BagGrid => bagGrid;
    public IReadOnlyList<InventoryGrid> EquipmentGrids => equipmentGrids;
    public Vector2 PreferredSize => ((RectTransform)transform).rect.size;

    public void Initialize(Inventory inventoryOwner, InventoryManager manager)
    {
        owner = inventoryOwner;
        inventory = manager;
        BindMember();
    }
    public void BindMember()
    {
        member = owner.SelectedMember;
        previewTarget.enabled = member?.Actor != null || member?.Definition?.previewModel != null;
        preview.Initialize(previewTarget, member?.Actor != null ? member.Actor : member?.Definition?.previewModel,
            member?.Actor == null ? inventory.EnumerateEquippedItems(owner.SelectedMemberId) : null);
        RefreshBagLayout();
        bagContent.anchoredPosition = Vector2.zero;
        Refresh();
    }
    public void RefreshBagLayout()
    {
        Vector2 size = new(bagGrid.gridSize.x * InventorySettings.slotSize.x, bagGrid.gridSize.y * InventorySettings.slotSize.y);
        float scale = Mathf.Min(1f, bagViewport.rect.width / Mathf.Max(1f, size.x));
        bagArtwork.sizeDelta = size;
        bagArtwork.localScale = Vector3.one * scale;
        bagContent.sizeDelta = size * scale;
    }
    private void RefreshTabs()
    {
        var ids = inventory.Party.MemberIds;
        roster = string.Join("|", ids);
        foreach (var tab in tabs)
        {
            tab.gameObject.SetActive(false);
            Destroy(tab.gameObject);
        }
        tabs.Clear();
        foreach (string id in ids)
        {
            var tab = Instantiate(tabPrefab, tabHolder);
            tab.Bind(inventory.Party.Get(id), owner, inventory);
            tabs.Add(tab);
        }
    }
    private void Refresh()
    {
        if (inventory == null || member == null) return;
        if (roster != string.Join("|", inventory.Party.MemberIds)) RefreshTabs();
        foreach (var tab in tabs) tab.Refresh();
        portrait.Refresh(member, inventory);
        characterNameLabel.text = member.Name;
        levelLabel.text = "LV " + member.Stats.lvl;
        int needed = Mathf.Max(1, member.Stats.nextLevelXP);
        xpLabel.text = member.Stats.currentXP.ToString("N0") + " / " + needed.ToString("N0") + " XP";
        xpFill.anchorMax = new Vector2(Mathf.Clamp01((float)member.Stats.currentXP / needed), 1f);
    }
    private void Update() => Refresh();
}
