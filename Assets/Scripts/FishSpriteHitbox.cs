using System.Collections.Generic;
using UnityEngine;

public class FishSpriteHitbox : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool removeLegacy3DColliders = true;
    [SerializeField] private float outlineScale = 0.88f;

    private PolygonCollider2D polygonCollider;
    private Sprite lastSprite;

    private static readonly List<Vector2> ShapeBuffer = new List<Vector2>(64);
    private static readonly List<Vector2> PathBufferA = new List<Vector2>(64);
    private static readonly List<Vector2> PathBufferB = new List<Vector2>(64);
    private static readonly List<Vector2> BoundsPath = new List<Vector2>(4);
    private static readonly List<Vector2> WorldBoundsBuffer = new List<Vector2>(64);

    public SpriteRenderer SpriteRenderer => spriteRenderer;
    public PolygonCollider2D PolygonCollider => polygonCollider;

    public void SetOutlineScale(float scale)
    {
        outlineScale = Mathf.Clamp(scale, 0.5f, 1f);
        RefreshColliderIfNeeded(force: true);
    }

    void Awake()
    {
        EnsureReady();
    }

    void OnEnable()
    {
        EnsureReady();
        RefreshColliderIfNeeded(force: true);
    }

    void LateUpdate()
    {
        RefreshColliderIfNeeded();
    }

    public void EnsureReady()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        if (removeLegacy3DColliders)
        {
            RemoveLegacy3DColliders();
        }

        if (polygonCollider == null)
        {
            polygonCollider = spriteRenderer.GetComponent<PolygonCollider2D>();
        }

        if (polygonCollider == null)
        {
            polygonCollider = spriteRenderer.gameObject.AddComponent<PolygonCollider2D>();
        }

        polygonCollider.isTrigger = true;
    }

    public void RefreshColliderIfNeeded(bool force = false)
    {
        if (spriteRenderer == null)
        {
            EnsureReady();
        }

        if (spriteRenderer == null || polygonCollider == null)
        {
            return;
        }

        Sprite currentSprite = spriteRenderer.sprite;

        if (!force && currentSprite == lastSprite)
        {
            return;
        }

        lastSprite = currentSprite;

        if (currentSprite == null)
        {
            polygonCollider.pathCount = 0;
            return;
        }

        int shapeCount = currentSprite.GetPhysicsShapeCount();

        if (shapeCount <= 0)
        {
            BuildBoundsFallback(currentSprite);
            shapeCount = 1;
        }
        else
        {
            polygonCollider.pathCount = shapeCount;

            for (int i = 0; i < shapeCount; i++)
            {
                ShapeBuffer.Clear();
                currentSprite.GetPhysicsShape(i, ShapeBuffer);
                TightenPath(ShapeBuffer);
                polygonCollider.SetPath(i, ShapeBuffer);
            }
        }
    }

    public bool Overlaps(FishSpriteHitbox other)
    {
        if (other == null)
        {
            return false;
        }

        RefreshColliderIfNeeded();
        other.RefreshColliderIfNeeded();

        if (polygonCollider == null || other.polygonCollider == null)
        {
            return false;
        }

        Rect myBounds = GetWorldBoundsXZ(polygonCollider);
        Rect otherBounds = GetWorldBoundsXZ(other.polygonCollider);

        if (!myBounds.Overlaps(otherBounds))
        {
            return false;
        }

        for (int myPathIndex = 0; myPathIndex < polygonCollider.pathCount; myPathIndex++)
        {
            GetWorldPathXZ(polygonCollider, myPathIndex, PathBufferA);

            if (PathBufferA.Count < 3)
            {
                continue;
            }

            for (int otherPathIndex = 0; otherPathIndex < other.polygonCollider.pathCount; otherPathIndex++)
            {
                other.GetWorldPathXZ(other.polygonCollider, otherPathIndex, PathBufferB);

                if (PathBufferB.Count < 3)
                {
                    continue;
                }

                if (PolygonsOverlap(PathBufferA, PathBufferB))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public float GetBodySizeMetricXZ()
    {
        return TryGetBodySizeMetricXZ(out float size) ? size : 0f;
    }

    public bool TryGetBodySizeMetricXZ(out float size)
    {
        RefreshColliderIfNeeded();

        if (polygonCollider == null || polygonCollider.pathCount <= 0)
        {
            size = 0f;
            return false;
        }

        Rect bounds = GetWorldBoundsXZ(polygonCollider);
        size = Mathf.Max(bounds.width, bounds.height);
        return size > Mathf.Epsilon;
    }

    void RemoveLegacy3DColliders()
    {
        Collider[] legacyColliders = GetComponentsInChildren<Collider>(includeInactive: true);

        for (int i = 0; i < legacyColliders.Length; i++)
        {
            Destroy(legacyColliders[i]);
        }
    }

    void BuildBoundsFallback(Sprite currentSprite)
    {
        Bounds spriteBounds = currentSprite.bounds;

        BoundsPath.Clear();
        BoundsPath.Add(new Vector2(spriteBounds.min.x, spriteBounds.min.y));
        BoundsPath.Add(new Vector2(spriteBounds.min.x, spriteBounds.max.y));
        BoundsPath.Add(new Vector2(spriteBounds.max.x, spriteBounds.max.y));
        BoundsPath.Add(new Vector2(spriteBounds.max.x, spriteBounds.min.y));

        TightenPath(BoundsPath);
        polygonCollider.pathCount = 1;
        polygonCollider.SetPath(0, BoundsPath);
    }

    void TightenPath(List<Vector2> path)
    {
        if (path == null || path.Count == 0 || outlineScale >= 0.999f)
        {
            return;
        }

        Vector2 center = Vector2.zero;

        for (int i = 0; i < path.Count; i++)
        {
            center += path[i];
        }

        center /= path.Count;

        for (int i = 0; i < path.Count; i++)
        {
            path[i] = center + (path[i] - center) * outlineScale;
        }
    }

    Rect GetWorldBoundsXZ(PolygonCollider2D collider2D)
    {
        bool hasPoint = false;
        float minX = 0f;
        float maxX = 0f;
        float minZ = 0f;
        float maxZ = 0f;

        for (int pathIndex = 0; pathIndex < collider2D.pathCount; pathIndex++)
        {
            GetWorldPathXZ(collider2D, pathIndex, WorldBoundsBuffer);

            for (int i = 0; i < WorldBoundsBuffer.Count; i++)
            {
                Vector2 point = WorldBoundsBuffer[i];

                if (!hasPoint)
                {
                    minX = maxX = point.x;
                    minZ = maxZ = point.y;
                    hasPoint = true;
                    continue;
                }

                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minZ = Mathf.Min(minZ, point.y);
                maxZ = Mathf.Max(maxZ, point.y);
            }
        }

        if (!hasPoint)
        {
            return new Rect();
        }

        return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
    }

    void GetWorldPathXZ(PolygonCollider2D collider2D, int pathIndex, List<Vector2> results)
    {
        results.Clear();
        collider2D.GetPath(pathIndex, ShapeBuffer);

        Transform pathTransform = collider2D.transform;

        for (int i = 0; i < ShapeBuffer.Count; i++)
        {
            Vector2 localPoint = ShapeBuffer[i];
            Vector3 worldPoint = pathTransform.TransformPoint(new Vector3(localPoint.x, localPoint.y, 0f));
            results.Add(new Vector2(worldPoint.x, worldPoint.z));
        }
    }

    static bool PolygonsOverlap(List<Vector2> polygonA, List<Vector2> polygonB)
    {
        if (AnySegmentsIntersect(polygonA, polygonB))
        {
            return true;
        }

        if (PointInPolygon(polygonA[0], polygonB))
        {
            return true;
        }

        return PointInPolygon(polygonB[0], polygonA);
    }

    static bool AnySegmentsIntersect(List<Vector2> polygonA, List<Vector2> polygonB)
    {
        for (int a = 0; a < polygonA.Count; a++)
        {
            Vector2 aStart = polygonA[a];
            Vector2 aEnd = polygonA[(a + 1) % polygonA.Count];

            for (int b = 0; b < polygonB.Count; b++)
            {
                Vector2 bStart = polygonB[b];
                Vector2 bEnd = polygonB[(b + 1) % polygonB.Count];

                if (SegmentsIntersect(aStart, aEnd, bStart, bEnd))
                {
                    return true;
                }
            }
        }

        return false;
    }

    static bool SegmentsIntersect(Vector2 aStart, Vector2 aEnd, Vector2 bStart, Vector2 bEnd)
    {
        float d1 = Cross(bStart, bEnd, aStart);
        float d2 = Cross(bStart, bEnd, aEnd);
        float d3 = Cross(aStart, aEnd, bStart);
        float d4 = Cross(aStart, aEnd, bEnd);

        if (((d1 > 0f && d2 < 0f) || (d1 < 0f && d2 > 0f)) &&
            ((d3 > 0f && d4 < 0f) || (d3 < 0f && d4 > 0f)))
        {
            return true;
        }

        if (Mathf.Approximately(d1, 0f) && PointOnSegment(aStart, bStart, bEnd)) return true;
        if (Mathf.Approximately(d2, 0f) && PointOnSegment(aEnd, bStart, bEnd)) return true;
        if (Mathf.Approximately(d3, 0f) && PointOnSegment(bStart, aStart, aEnd)) return true;
        if (Mathf.Approximately(d4, 0f) && PointOnSegment(bEnd, aStart, aEnd)) return true;

        return false;
    }

    static float Cross(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    static bool PointOnSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
    {
        return point.x >= Mathf.Min(segmentStart.x, segmentEnd.x) - Mathf.Epsilon &&
               point.x <= Mathf.Max(segmentStart.x, segmentEnd.x) + Mathf.Epsilon &&
               point.y >= Mathf.Min(segmentStart.y, segmentEnd.y) - Mathf.Epsilon &&
               point.y <= Mathf.Max(segmentStart.y, segmentEnd.y) + Mathf.Epsilon;
    }

    static bool PointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        bool inside = false;

        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];

            bool crosses = ((a.y > point.y) != (b.y > point.y)) &&
                           (point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y + Mathf.Epsilon) + a.x);

            if (crosses)
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
