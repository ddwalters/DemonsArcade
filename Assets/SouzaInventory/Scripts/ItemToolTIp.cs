using TMPro;
using UnityEngine;

public class ItemToolTip : MonoBehaviour
{
    private TextMeshProUGUI ItemName;
    private TextMeshProUGUI ItemDescription;
    private RectTransform backgroundRectTransform;

    // make the prefab on start? 
    [SerializeField] GameObject ToolTipPrefab;

    private void Awake()
    {
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

        Vector2 backgroundSize;
        float textPaddingSize = 8f;

        if (ItemName.preferredWidth < ItemDescription.preferredWidth)
            backgroundSize = new Vector2(ItemDescription.preferredWidth + textPaddingSize + 8f, ItemDescription.preferredHeight + ItemName.preferredHeight + textPaddingSize + 2f);
        else
            backgroundSize = new Vector2(ItemName.preferredWidth + textPaddingSize + 8f, ItemDescription.preferredHeight + ItemName.preferredHeight + textPaddingSize + 2f);

        backgroundRectTransform.sizeDelta = backgroundSize;
    }

    public void HideToolTip()
    {
        gameObject.SetActive(false);
    }
}