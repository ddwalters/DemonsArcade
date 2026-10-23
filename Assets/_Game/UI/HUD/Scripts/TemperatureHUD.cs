using UnityEngine;
using UnityEngine.UI;

/// <summary>A compact circular dial: cold left, moderate center, hot right.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class TemperatureHUD : MaskableGraphic
{
    [Tooltip("Exposure points needed to reach either end of the dial. Does not cap gameplay temperature.")]
    [SerializeField, Min(0.1f)] private float fullScaleTemperature = 5f;
    [Tooltip("The middle stays uncolored within this many temperature points of zero.")]
    [SerializeField, Min(0f)] private float moderateRange = 2f;

    private static readonly Color Cold = new Color(0.36f, 0.78f, 0.85f);
    private static readonly Color Hot = new Color(0.91f, 0.42f, 0.31f);
    private static readonly Color Background = new Color(0.22f, 0.23f, 0.24f, 0.55f);
    private PlayerTemperature temperature;
    private float displayedTemperature;
    private float nextPlayerSearch;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    private void Update()
    {
        if (temperature == null && Time.unscaledTime >= nextPlayerSearch)
        {
            temperature = FindAnyObjectByType<PlayerTemperature>();
            nextPlayerSearch = Time.unscaledTime + 0.5f;
        }
        float current = temperature != null ? temperature.CurrentTemperature : 0f;
        if (!Mathf.Approximately(current, displayedTemperature))
        {
            displayedTemperature = current;
            SetVerticesDirty();
        }

    }

    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        vertices.Clear();
        Rect rect = rectTransform.rect;
        float radius = Mathf.Max(1f, Mathf.Min(rect.width, rect.height) * 0.5f - 2f);
        Vector2 center = rect.center;
        AddArc(vertices, center, radius, 0f, 0f, 360f, Background);
        AddArc(vertices, center, radius, radius - 1.2f, 0f, 360f, Color.black);

        float bandOuter = radius - 2f;
        float bandInner = radius - 9f;
        float neutralFraction = Mathf.Clamp01(moderateRange / Mathf.Max(0.1f, fullScaleTemperature));
        float coldEnd = (1f - neutralFraction) * 0.5f;
        float hotStart = (1f + neutralFraction) * 0.5f;
        for (int i = 0; i < 10; i++)
        {
            float from = i / 10f;
            float to = (i + 1) / 10f;
            bool coldSide = from < coldEnd;
            if (!coldSide && to <= hotStart)
                continue;

            if (coldSide)
                to = Mathf.Min(to, coldEnd);
            else
                from = Mathf.Max(from, hotStart);

            if ((to - from) * 260f <= 3f)
                continue;

            Color tint = coldSide ? Cold : Hot;
            float saturation = Mathf.Abs((from + to) * 0.5f - 0.5f) * 2f;
            tint = Color.Lerp(Color.Lerp(tint, Color.white, 0.12f), tint, saturation);
            AddArc(vertices, center, bandOuter, bandInner,
                DialAngle(from) - 1.5f, DialAngle(to) + 1.5f, tint);
        }

        // Short radial dividers read as chunky segments, rather than a speedometer scale.
        for (int i = 0; i <= 10; i++)
        {
            Vector2 direction = Direction(DialAngle(i / 10f));
            Vector2 side = new Vector2(-direction.y, direction.x) * 0.45f;
            AddQuad(vertices, center + direction * bandInner - side,
                center + direction * bandOuter - side, center + direction * bandOuter + side,
                center + direction * bandInner + side, Color.black, Color.black);
        }

        float reading = Mathf.Clamp(displayedTemperature / Mathf.Max(0.1f, fullScaleTemperature), -1f, 1f);
        Vector2 needle = Direction(DialAngle((reading + 1f) * 0.5f));
        Vector2 perpendicular = new Vector2(-needle.y, needle.x);
        AddQuad(vertices, center - perpendicular * 1.4f, center + needle * (radius - 7f),
            center + perpendicular * 1.4f, center - needle * 2f, Color.black, Color.black);
        AddArc(vertices, center, 3f, 0f, 0f, 360f, Cold);
    }

    // A 260-degree sweep leaves a small opening at the bottom, as in the reference icon.
    private static float DialAngle(float fraction) => Mathf.Lerp(220f, -40f, fraction);

    private static Vector2 Direction(float degrees)
    {
        float angle = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    private static void AddArc(VertexHelper vertices, Vector2 center, float outerRadius,
        float innerRadius, float startAngle, float endAngle, Color tint)
    {
        int segments = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(endAngle - startAngle) / 6f));
        for (int i = 0; i < segments; i++)
        {
            Vector2 a = Direction(Mathf.Lerp(startAngle, endAngle, i / (float)segments));
            Vector2 b = Direction(Mathf.Lerp(startAngle, endAngle, (i + 1) / (float)segments));
            AddQuad(vertices, center + a * outerRadius, center + a * innerRadius,
                center + b * innerRadius, center + b * outerRadius, tint, tint);
        }
    }

    private static void AddQuad(VertexHelper vertices, Vector2 a, Vector2 b, Vector2 c, Vector2 d,
        Color left, Color right)
    {
        int index = vertices.currentVertCount;
        vertices.AddVert(a, left, Vector2.zero);
        vertices.AddVert(b, left, Vector2.zero);
        vertices.AddVert(c, right, Vector2.zero);
        vertices.AddVert(d, right, Vector2.zero);
        vertices.AddTriangle(index, index + 1, index + 2);
        vertices.AddTriangle(index + 2, index + 3, index);
    }

}
