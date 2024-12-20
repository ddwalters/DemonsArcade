using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemToolTip : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI ItemName;
    [SerializeField] TextMeshProUGUI ItemDescription;
    [SerializeField] RectTransform backgroundRectTransform;
    [SerializeField] RawImage border;

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

    public void ShowToolTip(string itemName, string itemStatsDescription, Color BorderColor)
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

        ItemName.color = BorderColor;
        border.color = BorderColor;
        


    }

    public void HideToolTip()
    {
        gameObject.SetActive(false);
    }
}