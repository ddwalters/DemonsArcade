using UnityEngine;
using UnityEngine.UI;

// Fit the visible artwork, rather than the prefab's smaller logical grid rectangle.
public class InventoryPanelLayout : MonoBehaviour
{
    private const float Padding = 16f;
    private RectTransform panel;
    private RectTransform canvasRect;
    private Vector3 originalScale;
    private Graphic[] graphics;
    private Vector2 previousSize;
    private bool playerPanel;
    private bool split;
    private bool dirty;
    private readonly Vector3[] corners = new Vector3[4];

    public void Configure(Canvas canvas, bool isPlayerPanel, bool splitView)
    {
        if (panel == null)
        {
            panel = (RectTransform)transform;
            originalScale = panel.localScale;
            graphics = GetComponentsInChildren<Graphic>();
        }

        canvasRect = (RectTransform)canvas.transform;
        playerPanel = isPlayerPanel;
        split = splitView;
        dirty = true;
    }

    private void LateUpdate()
    {
        if (canvasRect != null && (dirty || previousSize != canvasRect.rect.size))
            Fit();
    }

    public void Fit()
    {
        panel.localScale = originalScale;
        Rect available = canvasRect.rect;
        if (split)
        {
            available.width *= 0.5f;
            if (playerPanel)
                available.x += available.width;
        }

        available.xMin += Padding;
        available.xMax -= Padding;
        available.yMin += Padding;
        available.yMax -= Padding;
        Bounds bounds = VisibleBounds();
        float scale = Mathf.Min(1f, available.width / Mathf.Max(bounds.size.x, 1f),
            available.height / Mathf.Max(bounds.size.y, 1f));
        panel.localScale = originalScale * scale;
        bounds = VisibleBounds();
        Vector3 offset = available.center - (Vector2)bounds.center;
        panel.position += canvasRect.TransformVector(offset);
        previousSize = canvasRect.rect.size;
        dirty = false;
    }

    private Bounds VisibleBounds()
    {
        bool initialized = false;
        Bounds bounds = default;
        foreach (var graphic in graphics)
        {
            if (graphic == null || !graphic.isActiveAndEnabled || !graphic.transform.IsChildOf(panel))
                continue;

            graphic.rectTransform.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                Vector3 point = canvasRect.InverseTransformPoint(corner);
                if (!initialized)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    initialized = true;
                }
                else
                    bounds.Encapsulate(point);
            }
        }

        return bounds;
    }
}
