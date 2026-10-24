using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryStatChip : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI value;

    public Image Icon => icon;
    public TextMeshProUGUI Value => value;
}
