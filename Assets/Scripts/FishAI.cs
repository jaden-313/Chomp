using UnityEngine;

public class FishAI : MonoBehaviour
{
    public string speciesName = "Unknown Fish";
    public float fishSize = 0.8f;
    public float moveSpeed = 2f;
    public int scoreValue = 1;

    public float minX = -64f;
    public float maxX = 64f;
    public float minZ = -36f;
    public float maxZ = 36f;
    public float fixedY = 0.5f;

    public float minDirectionTime = 1.5f;
    public float maxDirectionTime = 4f;
    public float facingAngleOffset = 0f;
    public float facingThreshold = 0.05f;
    public float colliderOutlineScale = 0.88f;
    public float interactionSizeMultiplier = 1f;

    private Vector3 moveDirection;
    private float directionTimer;
    private FishSpriteHitbox fishHitbox;
    private SpriteRenderer spriteRenderer;
    private Transform visualTransform;
    private Quaternion baseVisualWorldRotation;

    void Awake()
    {
        fishHitbox = GetComponent<FishSpriteHitbox>();

        if (fishHitbox == null)
        {
            fishHitbox = gameObject.AddComponent<FishSpriteHitbox>();
        }

        fishHitbox.EnsureReady();
        fishHitbox.SetOutlineScale(colliderOutlineScale);
        spriteRenderer = fishHitbox.SpriteRenderer;
        visualTransform = spriteRenderer != null ? spriteRenderer.transform : null;

        if (visualTransform != null)
        {
            baseVisualWorldRotation = visualTransform.rotation;
        }
    }

    void OnEnable()
    {
        if (!FishHitboxRegistry.ActiveFish.Contains(this))
        {
            FishHitboxRegistry.ActiveFish.Add(this);
        }
    }

    void OnDisable()
    {
        FishHitboxRegistry.ActiveFish.Remove(this);
    }

    void Start()
    {
        PickNewDirection();
        SetNewDirectionTime();
    }

    void Update()
    {
        directionTimer -= Time.deltaTime;

        if (directionTimer <= 0f)
        {
            PickNewDirection();
            SetNewDirectionTime();
        }

        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        bool hitEdge = false;

        if (transform.position.x <= minX || transform.position.x >= maxX)
        {
            hitEdge = true;
        }

        if (transform.position.z <= minZ || transform.position.z >= maxZ)
        {
            hitEdge = true;
        }

        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        float clampedZ = Mathf.Clamp(transform.position.z, minZ, maxZ);

        transform.position = new Vector3(clampedX, fixedY, clampedZ);

        if (hitEdge)
        {
            PickNewDirection();
            SetNewDirectionTime();
        }

        UpdateFacing();
    }

    void PickNewDirection()
    {
        moveDirection = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        ).normalized;
    }

    void SetNewDirectionTime()
    {
        directionTimer = Random.Range(minDirectionTime, maxDirectionTime);
    }

    void UpdateFacing()
    {
        if (visualTransform == null)
        {
            return;
        }

        Vector2 planarDirection = new Vector2(moveDirection.x, moveDirection.z);

        if (planarDirection.sqrMagnitude < facingThreshold * facingThreshold)
        {
            return;
        }

        float headingDegrees = Mathf.Atan2(planarDirection.y, planarDirection.x) * Mathf.Rad2Deg;
        Quaternion facingRotation = Quaternion.AngleAxis(facingAngleOffset - headingDegrees, Vector3.up);
        visualTransform.rotation = facingRotation * baseVisualWorldRotation;
    }
}
