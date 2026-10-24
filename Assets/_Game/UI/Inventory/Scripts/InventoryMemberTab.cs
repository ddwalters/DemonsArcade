using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryMemberTab : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private PartyPortraitView portrait;
    [SerializeField] private GameObject selectedIndicator;
    private CharacterRecord member;
    private Inventory owner;
    private InventoryManager inventory;

    public void Bind(CharacterRecord character, Inventory inventoryOwner, InventoryManager manager)
    {
        button.onClick.RemoveListener(SelectMember);
        member = character;
        owner = inventoryOwner;
        inventory = manager;
        button.onClick.AddListener(SelectMember);
        Refresh();
    }
    private void SelectMember() => owner.SelectPartyMember(member.Id);
    public void Refresh()
    {
        if (member == null) return;
        portrait.Refresh(member, inventory);
        selectedIndicator.SetActive(owner.SelectedMemberId == member.Id);
        button.interactable = owner.selectedItem == null;
    }
    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(SelectMember);
    }
}
