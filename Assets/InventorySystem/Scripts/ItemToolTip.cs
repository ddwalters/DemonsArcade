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
    }

    private void Update()
    {
        var parentCanvasRectTransform = transform.parent.GetComponent<RectTransform>();
        Vector2 mergedFactors = new Vector2(
            parentCanvasRectTransform.sizeDelta.x / Screen.width,
            parentCanvasRectTransform.sizeDelta.y / Screen.height);

        GetComponent<RectTransform>().anchoredPosition = Input.mousePosition * mergedFactors;
    }

    public void ShowToolTip(string itemName, string itemStatsDescription)
    {
        gameObject.SetActive(true);

        ItemName.text = itemName;
        ItemDescription.text = itemStatsDescription;

        float textPaddingSize = 8f;
        Vector2 backgroundSize = new Vector2(ItemDescription.preferredWidth + textPaddingSize, ItemDescription.preferredHeight + ItemName.preferredHeight + textPaddingSize);
        backgroundRectTransform.sizeDelta = backgroundSize;
    }

    public void HideToolTip()
    {
        gameObject.SetActive(false);
    }
}
