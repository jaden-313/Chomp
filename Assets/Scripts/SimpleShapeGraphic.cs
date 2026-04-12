using UnityEngine;
using UnityEngine.UI;

// Draws a simple UI circle or triangle directly in the Canvas.
// This keeps the minimap shapes visible in the editor without runtime-made sprites.
[ExecuteAlways]
public class SimpleShapeGraphic : MaskableGraphic
{
    public enum ShapeType
    {
        Circle,
        Triangle
    }

    public ShapeType shape = ShapeType.Circle;
    public int circleSegments = 40;

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        if (shape == ShapeType.Triangle)
        {
            DrawTriangle(vertexHelper);
        }
        else
        {
            DrawCircle(vertexHelper);
        }
    }

    void DrawCircle(VertexHelper vertexHelper)
    {
        Rect rect = rectTransform.rect;
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
        int segments = Mathf.Max(12, circleSegments);

        vertexHelper.AddVert(center, color, Vector2.zero);

        for (int i = 0; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            vertexHelper.AddVert(point, color, Vector2.zero);
        }

        for (int i = 1; i <= segments; i++)
        {
            vertexHelper.AddTriangle(0, i, i + 1);
        }
    }

    void DrawTriangle(VertexHelper vertexHelper)
    {
        Rect rect = rectTransform.rect;
        Vector2 top = new Vector2(rect.center.x, rect.yMax);
        Vector2 left = new Vector2(rect.xMin, rect.yMin);
        Vector2 right = new Vector2(rect.xMax, rect.yMin);

        vertexHelper.AddVert(top, color, Vector2.zero);
        vertexHelper.AddVert(left, color, Vector2.zero);
        vertexHelper.AddVert(right, color, Vector2.zero);
        vertexHelper.AddTriangle(0, 1, 2);
    }
}
