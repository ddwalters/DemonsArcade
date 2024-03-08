using TMPro;
using UnityEngine;

public class ItemToolTip : MonoBehaviour
{
    private Camera uiCamera;

    private TextMeshProUGUI ItemName;
    private TextMeshProUGUI ItemDescription;
    private RectTransform backgroundRectTransform;

    private void Awake()
    {
        uiCamera = GameObject.Find("PlayerCamera").GetComponent<Camera>();
        backgroundRectTransform = transform.Find("ToolTipBackground").GetComponent<RectTransform>();
        ItemName = transform.Find("ItemNameToolTipText").GetComponent<TextMeshProUGUI>();
        ItemDescription = transform.Find("ItemDescriptionToolTipText").GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        HideToolTip();
        //ShowToolTip("Item Name", "Item stats and descriptions ect. \nnew Line stats");
    }

    private void Update()
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent.GetComponent<RectTransform>(), Input.mousePosition, uiCamera, out localPoint);
        transform.localPosition = localPoint;
    }

    public void ShowToolTip(string itemName, string itemStatsDescription)
    {
        gameObject.SetActive(true);

        ItemName.text = itemName;
        ItemDescription.text = itemStatsDescription;

        float textPaddingSize = 7f;
        Vector2 backgorundSize = new Vector2(ItemDescription.preferredWidth + textPaddingSize * 2f, ItemDescription.preferredHeight + ItemName.preferredHeight + textPaddingSize * 2f);
        backgroundRectTransform.sizeDelta = backgorundSize;
    }

    public void HideToolTip()
    {
        gameObject.SetActive(false);
    }
}
