using TMPro;
using UnityEngine;

public class ItemToolTip : MonoBehaviour
{
    private static ItemToolTip instance;

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
        instance = this;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent.GetComponent<RectTransform>(), Input.mousePosition, uiCamera, out localPoint);
        transform.localPosition = localPoint;
    }

    private void ShowToolTip(string itemName, string itemStatsDescription)
    {
        gameObject.SetActive(true);

        ItemName.text = itemName;
        ItemDescription.text = itemStatsDescription;

        float textPaddingSize = 7f;
        Vector2 backgorundSize = new Vector2(ItemDescription.preferredWidth + textPaddingSize * 2f, ItemDescription.preferredHeight + ItemName.preferredHeight + textPaddingSize * 2f);
        backgroundRectTransform.sizeDelta = backgorundSize;
    }

    private void HideToolTip()
    {
        gameObject.SetActive(false);
    }

    public static void ShowToolTip_Static(string itemName, string itemStatsDescription)
    {
        instance.ShowToolTip(itemName, itemStatsDescription);
    }

    public static void HideToolTip_Static()
    {
        instance.HideToolTip();
    }

    public static bool GetIfToolTipHidden_Static()
    {
        return instance.enabled;
    }
}
