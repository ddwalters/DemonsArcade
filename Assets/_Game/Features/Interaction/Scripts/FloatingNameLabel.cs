using TMPro;
using UnityEngine;

public class FloatingNameLabel : MonoBehaviour
{
    [SerializeField] private TextMeshPro text;
    [SerializeField, Min(0.1f)] private float minimumWidth = 2f;
    [SerializeField, Min(0.1f)] private float minimumHeight = 5f;

    private Camera viewCamera;

    public void SetName(string targetName)
    {
        text.SetText(targetName);
        Vector2 preferredSize = text.GetPreferredValues(targetName);
        text.rectTransform.sizeDelta = new Vector2(
            Mathf.Max(preferredSize.x, minimumWidth),
            Mathf.Max(preferredSize.y, minimumHeight));
    }

    public void Show(Vector3 worldPosition, Camera camera)
    {
        viewCamera = camera;
        transform.SetPositionAndRotation(worldPosition, camera.transform.rotation);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        viewCamera = null;
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (viewCamera != null)
            transform.rotation = viewCamera.transform.rotation;
    }
}
