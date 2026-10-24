using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The prefab owns every visual. This component only chooses the character's portrait source.
public sealed class PartyPortraitView : MonoBehaviour
{
    [SerializeField] private Image portrait;
    [SerializeField] private RawImage modelPortrait;
    [SerializeField] private TextMeshProUGUI initial;
    [SerializeField] private CharacterInventoryPreview preview;
    private GameObject previewSource;
    private string memberId;

    public void Refresh(CharacterRecord member, InventoryManager inventory)
    {
        Sprite sprite = member?.Portrait;
        portrait.sprite = sprite;
        portrait.enabled = sprite != null;
        GameObject source = sprite == null ? (member?.Actor != null ? member.Actor : member?.Definition?.previewModel) : null;
        modelPortrait.enabled = source != null;
        if (source != previewSource || memberId != member?.Id)
        {
            previewSource = source;
            memberId = member?.Id;
            preview.Initialize(modelPortrait, source,
                member?.Actor == null && member != null ? inventory.EnumerateEquippedItems(member.Id) : null);
        }
        bool hasModel = source != null && modelPortrait.texture != null;
        initial.gameObject.SetActive(sprite == null && !hasModel);
        initial.text = string.IsNullOrEmpty(member?.Name) ? "?" : member.Name.Substring(0, 1);
    }
}
