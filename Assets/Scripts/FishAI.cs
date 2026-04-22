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
    public float playerDetectionRange = 12f;
    public float stopChasingRange = 17f;
    public float chaseLoseSightDuration = 2.25f;
    public float chaseMoveSpeed = 3.5f;

    private Vector3 moveDirection;
    private float directionTimer;
    private FishSpriteHitbox fishHitbox;
    private FishSpriteHitbox playerHitbox;
    private SpriteRenderer spriteRenderer;
    private Transform visualTransform;
    private Quaternion baseVisualWorldRotation;
    private Transform player;
    private bool isChasingPlayer;
    private float lostSightTimer;

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
        FindPlayer();
    }

    void Update()
    {
        if (ShouldChasePlayer())
        {
            UpdateChaseDirection();
        }
        else
        {
            directionTimer -= Time.deltaTime;

            if (directionTimer <= 0f)
            {
                PickNewDirection();
                SetNewDirectionTime();
            }
        }

        float currentMoveSpeed = isChasingPlayer ? chaseMoveSpeed : moveSpeed;
        transform.position += moveDirection * currentMoveSpeed * Time.deltaTime;

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

        if (hitEdge && !isChasingPlayer)
        {
            PickNewDirection();
            SetNewDirectionTime();
        }

        UpdateFacing();
    }

    void PickNewDirection()
    {
        isChasingPlayer = false;
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

    void FindPlayer()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            playerHitbox = playerObject.GetComponent<FishSpriteHitbox>();
        }
    }

    bool ShouldChasePlayer()
    {
        if (!IsPredatorSpecies())
        {
            isChasingPlayer = false;
            lostSightTimer = 0f;
            return false;
        }

        FindPlayer();

        if (player == null)
        {
            isChasingPlayer = false;
            lostSightTimer = 0f;
            return false;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float chaseEndRange = Mathf.Max(playerDetectionRange, stopChasingRange);
        bool fishIsBigger = IsFishBiggerThanPlayer();

        if (isChasingPlayer)
        {
            if (!fishIsBigger)
            {
                isChasingPlayer = false;
                lostSightTimer = 0f;
                return false;
            }

            if (distanceToPlayer <= chaseEndRange)
            {
                lostSightTimer = 0f;
                return true;
            }

            lostSightTimer += Time.deltaTime;

            if (lostSightTimer >= chaseLoseSightDuration)
            {
                isChasingPlayer = false;
                lostSightTimer = 0f;
                return false;
            }

            return true;
        }

        isChasingPlayer = fishIsBigger && distanceToPlayer <= playerDetectionRange;

        if (isChasingPlayer)
        {
            lostSightTimer = 0f;
        }

        return isChasingPlayer;
    }

    void UpdateChaseDirection()
    {
        if (player == null)
        {
            return;
        }

        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0f;

        if (directionToPlayer.sqrMagnitude > 0.0001f)
        {
            moveDirection = directionToPlayer.normalized;
        }
    }

    bool IsPredatorSpecies()
    {
        string normalizedSpeciesName = speciesName.Trim().ToLowerInvariant();
        return normalizedSpeciesName == "swordfish" ||
               normalizedSpeciesName == "barracuda" ||
               normalizedSpeciesName == "shark";
    }

    bool IsFishBiggerThanPlayer()
    {
        if (player == null)
        {
            return false;
        }

        float playerSize = GetCurrentSize(playerHitbox, player.localScale.x);
        float fishCurrentSize = GetCurrentSize(fishHitbox, Mathf.Max(transform.localScale.x, fishSize));
        return fishCurrentSize > playerSize;
    }

    float GetCurrentSize(FishSpriteHitbox hitbox, float fallbackSize)
    {
        if (hitbox != null && hitbox.TryGetBodySizeMetricXZ(out float size))
        {
            return Mathf.Max(size, 0.01f);
        }

        return Mathf.Max(fallbackSize, 0.01f);
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
