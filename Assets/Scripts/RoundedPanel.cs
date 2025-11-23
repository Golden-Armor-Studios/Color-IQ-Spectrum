using UnityEngine;
using UnityEngine.UI;

// Lightweight rounded-rectangle fill for UI panels without needing a sprite.
[RequireComponent(typeof(CanvasRenderer))]
public class RoundedPanel : MaskableGraphic
{
    [SerializeField, Range(0f, 0.5f)]
    float normalizedCornerRadius = 0.08f;

    public float NormalizedCornerRadius
    {
        get => normalizedCornerRadius;
        set
        {
            normalizedCornerRadius = Mathf.Clamp01(value);
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float minSide = Mathf.Min(rect.width, rect.height);
        float radius = Mathf.Clamp(normalizedCornerRadius * minSide, 0f, minSide * 0.5f);
        int segmentsPerCorner = Mathf.Max(2, Mathf.RoundToInt(radius / 8f));
        Color32 color32 = color;

        // Center rectangle
        AddQuad(vh, rect.xMin + radius, rect.yMin + radius, rect.xMax - radius, rect.yMax - radius, color32);

        // Horizontal strips
        AddQuad(vh, rect.xMin + radius, rect.yMin, rect.xMax - radius, rect.yMin + radius, color32);
        AddQuad(vh, rect.xMin + radius, rect.yMax - radius, rect.xMax - radius, rect.yMax, color32);

        // Vertical strips
        AddQuad(vh, rect.xMin, rect.yMin + radius, rect.xMin + radius, rect.yMax - radius, color32);
        AddQuad(vh, rect.xMax - radius, rect.yMin + radius, rect.xMax, rect.yMax - radius, color32);

        // Rounded corners
        DrawCorner(vh, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, Mathf.PI, 1.5f * Mathf.PI, segmentsPerCorner, color32);
        DrawCorner(vh, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 0.5f * Mathf.PI, Mathf.PI, segmentsPerCorner, color32);
        DrawCorner(vh, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 0.5f * Mathf.PI, segmentsPerCorner, color32);
        DrawCorner(vh, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 1.5f * Mathf.PI, 2f * Mathf.PI, segmentsPerCorner, color32);
    }

    static void DrawCorner(VertexHelper vh, Vector2 center, float radius, float startAngle, float endAngle, int segments, Color32 color)
    {
        float angleStep = (endAngle - startAngle) / segments;
        Vector2 prevPoint = center + new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            AddTriangle(vh, center, prevPoint, point, color);
            prevPoint = point;
        }
    }

    static void AddQuad(VertexHelper vh, float xMin, float yMin, float xMax, float yMax, Color32 color)
    {
        int index = vh.currentVertCount;
        vh.AddVert(new Vector3(xMin, yMin), color, Vector2.zero);
        vh.AddVert(new Vector3(xMin, yMax), color, Vector2.zero);
        vh.AddVert(new Vector3(xMax, yMax), color, Vector2.zero);
        vh.AddVert(new Vector3(xMax, yMin), color, Vector2.zero);
        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }

    static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color32 color)
    {
        int index = vh.currentVertCount;
        vh.AddVert(a, color, Vector2.zero);
        vh.AddVert(b, color, Vector2.zero);
        vh.AddVert(c, color, Vector2.zero);
        vh.AddTriangle(index, index + 1, index + 2);
    }
}
