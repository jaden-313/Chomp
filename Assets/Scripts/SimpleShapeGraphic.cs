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
        Triangle,
        SpeakerOn,
        SpeakerOff,
        TexturedQuad
    }

    public ShapeType shape = ShapeType.Circle;
    public int circleSegments = 40;
    public Texture textureAsset;

    public override Texture mainTexture
    {
        get
        {
            return textureAsset != null ? textureAsset : s_WhiteTexture;
        }
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        if (shape == ShapeType.Triangle)
        {
            DrawTriangle(vertexHelper);
        }
        else if (shape == ShapeType.SpeakerOn)
        {
            DrawSpeaker(vertexHelper, true);
        }
        else if (shape == ShapeType.SpeakerOff)
        {
            DrawSpeaker(vertexHelper, false);
        }
        else if (shape == ShapeType.TexturedQuad)
        {
            DrawTexturedQuad(vertexHelper);
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

    void DrawTexturedQuad(VertexHelper vertexHelper)
    {
        Rect rect = rectTransform.rect;
        int index = vertexHelper.currentVertCount;

        vertexHelper.AddVert(new Vector2(rect.xMin, rect.yMin), color, new Vector2(0f, 0f));
        vertexHelper.AddVert(new Vector2(rect.xMin, rect.yMax), color, new Vector2(0f, 1f));
        vertexHelper.AddVert(new Vector2(rect.xMax, rect.yMax), color, new Vector2(1f, 1f));
        vertexHelper.AddVert(new Vector2(rect.xMax, rect.yMin), color, new Vector2(1f, 0f));

        vertexHelper.AddTriangle(index, index + 1, index + 2);
        vertexHelper.AddTriangle(index, index + 2, index + 3);
    }

    void DrawSpeaker(VertexHelper vertexHelper, bool isOn)
    {
        Rect rect = rectTransform.rect;
        float width = rect.width;
        float height = rect.height;

        Vector2 bodyMin = new Vector2(rect.xMin + width * 0.10f, rect.yMin + height * 0.33f);
        Vector2 bodyMax = new Vector2(rect.xMin + width * 0.24f, rect.yMax - height * 0.33f);
        AddQuad(vertexHelper, bodyMin, bodyMax);

        Vector2 coneLeftTop = new Vector2(bodyMax.x, rect.yMax - height * 0.22f);
        Vector2 coneLeftBottom = new Vector2(bodyMax.x, rect.yMin + height * 0.22f);
        Vector2 coneRightTop = new Vector2(rect.xMin + width * 0.52f, rect.yMax - height * 0.10f);
        Vector2 coneRightBottom = new Vector2(rect.xMin + width * 0.52f, rect.yMin + height * 0.10f);
        AddQuad(vertexHelper, coneLeftBottom, coneLeftTop, coneRightTop, coneRightBottom);

        if (isOn)
        {
            float arcThickness = Mathf.Min(width, height) * 0.09f;
            Vector2 arcCenter = new Vector2(rect.xMin + width * 0.49f, rect.center.y);
            AddArc(vertexHelper, arcCenter, width * 0.18f, -42f, 42f, arcThickness, 10);
            AddArc(vertexHelper, arcCenter, width * 0.30f, -48f, 48f, arcThickness, 12);
        }
        else
        {
            float slashThickness = Mathf.Min(width, height) * 0.10f;
            AddLine(
                vertexHelper,
                new Vector2(rect.xMin + width * 0.64f, rect.yMin + height * 0.24f),
                new Vector2(rect.xMin + width * 0.88f, rect.yMax - height * 0.24f),
                slashThickness);

            AddLine(
                vertexHelper,
                new Vector2(rect.xMin + width * 0.64f, rect.yMax - height * 0.24f),
                new Vector2(rect.xMin + width * 0.88f, rect.yMin + height * 0.24f),
                slashThickness);
        }
    }

    void AddQuad(VertexHelper vertexHelper, Vector2 min, Vector2 max)
    {
        int index = vertexHelper.currentVertCount;

        vertexHelper.AddVert(new Vector2(min.x, min.y), color, Vector2.zero);
        vertexHelper.AddVert(new Vector2(min.x, max.y), color, Vector2.zero);
        vertexHelper.AddVert(new Vector2(max.x, max.y), color, Vector2.zero);
        vertexHelper.AddVert(new Vector2(max.x, min.y), color, Vector2.zero);

        vertexHelper.AddTriangle(index, index + 1, index + 2);
        vertexHelper.AddTriangle(index, index + 2, index + 3);
    }

    void AddQuad(VertexHelper vertexHelper, Vector2 bottomLeft, Vector2 topLeft, Vector2 topRight, Vector2 bottomRight)
    {
        int index = vertexHelper.currentVertCount;

        vertexHelper.AddVert(bottomLeft, color, Vector2.zero);
        vertexHelper.AddVert(topLeft, color, Vector2.zero);
        vertexHelper.AddVert(topRight, color, Vector2.zero);
        vertexHelper.AddVert(bottomRight, color, Vector2.zero);

        vertexHelper.AddTriangle(index, index + 1, index + 2);
        vertexHelper.AddTriangle(index, index + 2, index + 3);
    }

    void AddTriangle(VertexHelper vertexHelper, Vector2 a, Vector2 b, Vector2 c)
    {
        int index = vertexHelper.currentVertCount;

        vertexHelper.AddVert(a, color, Vector2.zero);
        vertexHelper.AddVert(b, color, Vector2.zero);
        vertexHelper.AddVert(c, color, Vector2.zero);
        vertexHelper.AddTriangle(index, index + 1, index + 2);
    }

    void AddLine(VertexHelper vertexHelper, Vector2 start, Vector2 end, float thickness)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x) * (thickness * 0.5f);

        int index = vertexHelper.currentVertCount;

        vertexHelper.AddVert(start - normal, color, Vector2.zero);
        vertexHelper.AddVert(start + normal, color, Vector2.zero);
        vertexHelper.AddVert(end + normal, color, Vector2.zero);
        vertexHelper.AddVert(end - normal, color, Vector2.zero);

        vertexHelper.AddTriangle(index, index + 1, index + 2);
        vertexHelper.AddTriangle(index, index + 2, index + 3);
    }

    void AddArc(VertexHelper vertexHelper, Vector2 center, float radius, float startDegrees, float endDegrees, float thickness, int segments)
    {
        int clampedSegments = Mathf.Max(2, segments);
        float innerRadius = Mathf.Max(0f, radius - thickness * 0.5f);
        float outerRadius = radius + thickness * 0.5f;

        for (int i = 0; i < clampedSegments; i++)
        {
            float t0 = i / (float)clampedSegments;
            float t1 = (i + 1) / (float)clampedSegments;
            float angle0 = Mathf.Lerp(startDegrees, endDegrees, t0) * Mathf.Deg2Rad;
            float angle1 = Mathf.Lerp(startDegrees, endDegrees, t1) * Mathf.Deg2Rad;

            Vector2 innerStart = center + new Vector2(Mathf.Cos(angle0), Mathf.Sin(angle0)) * innerRadius;
            Vector2 outerStart = center + new Vector2(Mathf.Cos(angle0), Mathf.Sin(angle0)) * outerRadius;
            Vector2 outerEnd = center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * outerRadius;
            Vector2 innerEnd = center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * innerRadius;

            int index = vertexHelper.currentVertCount;
            vertexHelper.AddVert(innerStart, color, Vector2.zero);
            vertexHelper.AddVert(outerStart, color, Vector2.zero);
            vertexHelper.AddVert(outerEnd, color, Vector2.zero);
            vertexHelper.AddVert(innerEnd, color, Vector2.zero);
            vertexHelper.AddTriangle(index, index + 1, index + 2);
            vertexHelper.AddTriangle(index, index + 2, index + 3);
        }
    }
}
