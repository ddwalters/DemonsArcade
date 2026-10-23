using TMPro;
using UnityEngine;

public sealed class PartyMemberHudRow : MonoBehaviour
{
    [SerializeField] private PartyPortraitView portrait;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private RectTransform healthFill;
    [SerializeField] private RectTransform manaFill;
    [SerializeField] private RectTransform staminaFill;
    [SerializeField] private GameObject staminaTrack;
    private PartyMember member;
    private InventoryManager inventory;

    public void Bind(PartyMember character, InventoryManager manager)
    {
        member = character;
        inventory = manager;
        staminaTrack.SetActive(member.IsPlayer);
        Refresh();
    }
    public void Refresh()
    {
        if (member == null) return;
        nameLabel.text = member.Name + "  ·  Lv " + member.Stats.lvl;
        portrait.Refresh(member, inventory);
        var stats = member.Stats;
        Fill(healthFill, stats.currentHealth, stats.maxHealth);
        Fill(manaFill, stats.currentMana, stats.maxMana);
        Fill(staminaFill, stats.currentStamina, stats.maxStamina);
    }
    private static void Fill(RectTransform fill, float value, float maximum)
    {
        fill.anchorMax = new Vector2(maximum > 0f ? Mathf.Clamp01(value / maximum) : 0f, 1f);
    }
}
