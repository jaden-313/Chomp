using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum FishInteractionOutcome
    {
        Neutral,
        PlayerEatsFish,
        FishKillsPlayer
    }

    public SpriteRenderer spriteRenderer;
    public float moveSpeed = 11f;
    public float minSpeed = 4f;
    public float maxSpeed = 11f;


    public float hungerRestoreAmount = 20f;
    public float minHungerRestoreFactor = 0.05f;
    public float maxHungerRestoreFactor = 1f;
    public float hungerRestoreExponent = 2.4f;

    public float smallPreyPenaltyStartSize = 1.35f;
    public float smallPreyPenaltyMaxSize = 3.2f;
    public float smallPreyRelativeSizeThreshold = 0.42f;
    public float smallPreyMaxPenaltyMultiplier = 0.22f;


    public float hunger = 100f;
    public float hungerDrainRate = 2f;

    public int score = 0;
    public float growthAmount = 0.1f;
    public float fixedY = 0.5f;

    public float fishSize = 1f;
    public float minFishSize = 0.5f;

    public float minX = -64f;
    public float maxX = 64f;
    public float minZ = -36f;
    public float maxZ = 36f;

    public float shrinkThreshold = 50f;
    public float shrinkRate = 0.3f;
    public float visualShrinkRate = 0.12f;
    public float facingAngleOffset = 0f;
    public float facingThreshold = 0.05f;
    public float colliderOutlineScale = 0.86f;
    public float speedSizeReference = 10f;
    public float speedFalloffExponent = 1.1f;
    public float sprintMultiplier = 1.35f;
    public float sprintHungerDrainRate = 4f;
    public float sprintMaxSpeed = 14.5f;
    public float decisiveSizeMultiplier = 1.4f;
    public float smallestRecoverablePreySize = 0.4f;
    public float recoverySizeMargin = 0.14f;
    public float minGrowthFactor = 0.08f;
    public float maxGrowthFactor = 0.9f;

    private GameManager gameManager;
    private FishSpriteHitbox playerHitbox;
    private Transform visualTransform;
    private Quaternion baseVisualWorldRotation;
    private bool isSprinting;

    void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        playerHitbox = GetComponent<FishSpriteHitbox>();

        if (playerHitbox == null)
        {
            playerHitbox = gameObject.AddComponent<FishSpriteHitbox>();
        }

        playerHitbox.EnsureReady();
        playerHitbox.SetOutlineScale(colliderOutlineScale);
        visualTransform = spriteRenderer != null ? spriteRenderer.transform : null;

        if (visualTransform != null)
        {
            baseVisualWorldRotation = visualTransform.rotation;
        }
    }

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager != null)
        {
            gameManager.UpdateHunger(hunger);
            gameManager.UpdateScore(score);
        }
    }

    void Update()
    {
        MovePlayer();
        DrainHunger();
        HandleShrinking();
        CheckFishOverlaps();
    }

    void MovePlayer()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = Vector3.ClampMagnitude(new Vector3(h, 0f, v), 1f);
        isSprinting = move.sqrMagnitude > 0.0001f && Input.GetKey(KeyCode.Space) && hunger > 0f;
        float currentMoveSpeed = GetCurrentMoveSpeed();

        transform.position += move * currentMoveSpeed * Time.deltaTime;

        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        float clampedZ = Mathf.Clamp(transform.position.z, minZ, maxZ);

        transform.position = new Vector3(clampedX, fixedY, clampedZ);

        UpdateFacing(move);
    }

    float GetCurrentMoveSpeed()
    {
        float currentSize = GetCurrentCombatSize(playerHitbox, fishSize, transform.localScale.x);
        float normalizedSize = Mathf.InverseLerp(minFishSize, speedSizeReference, currentSize);
        normalizedSize = Mathf.Pow(normalizedSize, speedFalloffExponent);

        float normalMoveSpeed = Mathf.Lerp(moveSpeed, minSpeed, normalizedSize);

        if (!isSprinting)
        {
            return Mathf.Min(normalMoveSpeed, maxSpeed);
        }

        return Mathf.Min(normalMoveSpeed * sprintMultiplier, sprintMaxSpeed);
    }

    void UpdateFacing(Vector3 move)
    {
        if (visualTransform == null)
        {
            return;
        }

        Vector2 planarDirection = new Vector2(move.x, move.z);

        if (planarDirection.sqrMagnitude < facingThreshold * facingThreshold)
        {
            return;
        }

        float headingDegrees = Mathf.Atan2(planarDirection.y, planarDirection.x) * Mathf.Rad2Deg;
        Quaternion facingRotation = Quaternion.AngleAxis(facingAngleOffset - headingDegrees, Vector3.up);
        visualTransform.rotation = facingRotation * baseVisualWorldRotation;
    }

    void DrainHunger()
    {
        float totalHungerDrainRate = hungerDrainRate;

        if (isSprinting)
        {
            totalHungerDrainRate += sprintHungerDrainRate;
        }

        hunger -= totalHungerDrainRate * Time.deltaTime;
        hunger = Mathf.Clamp(hunger, 0f, 100f);

        if (gameManager != null)
        {
            gameManager.UpdateHunger(hunger);
        }

        if (hunger <= 0f && gameManager != null)
        {
            gameManager.GameOver();
        }
    }

    void HandleShrinking()
    {
        if (hunger < shrinkThreshold)
        {
            float minimumRecoverySize = GetMinimumRecoverySizeFloor();
            fishSize -= shrinkRate * Time.deltaTime;
            fishSize = Mathf.Max(fishSize, minimumRecoverySize);

            Vector3 minScale = new Vector3(minimumRecoverySize, minimumRecoverySize, minimumRecoverySize);

            transform.localScale = Vector3.Max(
                transform.localScale - new Vector3(visualShrinkRate, visualShrinkRate, visualShrinkRate) * Time.deltaTime,
                minScale
            );
        }
    }

    void CheckFishOverlaps()
    {
        if (playerHitbox == null)
        {
            return;
        }

        for (int i = FishHitboxRegistry.ActiveFish.Count - 1; i >= 0; i--)
        {
            FishAI otherFish = FishHitboxRegistry.ActiveFish[i];

            if (otherFish == null || otherFish.gameObject == gameObject)
            {
                continue;
            }

            FishSpriteHitbox otherHitbox = otherFish.GetComponent<FishSpriteHitbox>();

            if (otherHitbox == null || !playerHitbox.Overlaps(otherHitbox))
            {
                continue;
            }

            FishInteractionOutcome outcome = GetInteractionOutcome(
                playerHitbox,
                fishSize,
                transform.localScale.x,
                otherHitbox,
                otherFish.fishSize,
                otherFish.transform.localScale.x,
                decisiveSizeMultiplier * Mathf.Max(0.1f, otherFish.interactionSizeMultiplier));

            if (outcome == FishInteractionOutcome.PlayerEatsFish)
            {
                EatFish(otherFish, otherHitbox);
            }
            else if (outcome == FishInteractionOutcome.FishKillsPlayer)
            {
                if (gameManager != null)
                {
                    gameManager.GameOver();
                }

                return;
            }
            else
            {
                // Sizes are too close, so nothing happens.
            }
        }
    }

    public static FishInteractionOutcome GetInteractionOutcome(
        FishSpriteHitbox playerHitbox,
        float playerConfiguredSize,
        float playerFallbackScale,
        FishSpriteHitbox fishHitbox,
        float fishConfiguredSize,
        float fishFallbackScale,
        float decisiveSizeMultiplier)
    {
        return TryEvaluateInteraction(
            playerHitbox,
            playerConfiguredSize,
            playerFallbackScale,
            fishHitbox,
            fishConfiguredSize,
            fishFallbackScale,
            decisiveSizeMultiplier,
            out FishInteractionOutcome outcome,
            out _) ? outcome : FishInteractionOutcome.Neutral;
    }

    public FishInteractionOutcome GetInteractionOutcome(FishAI fish)
    {
        GetInteractionRead(fish, out FishInteractionOutcome outcome, out _);
        return outcome;
    }

    public bool GetInteractionRead(FishAI fish, out FishInteractionOutcome outcome, out float neutralBias)
    {
        outcome = FishInteractionOutcome.Neutral;
        neutralBias = 0f;

        if (fish == null)
        {
            return false;
        }

        FishSpriteHitbox fishHitbox = fish.GetComponent<FishSpriteHitbox>();
        float effectiveMultiplier = decisiveSizeMultiplier * Mathf.Max(0.1f, fish.interactionSizeMultiplier);

        return TryEvaluateInteraction(
            playerHitbox,
            fishSize,
            transform.localScale.x,
            fishHitbox,
            fish.fishSize,
            fish.transform.localScale.x,
            effectiveMultiplier,
            out outcome,
            out neutralBias);
    }

    static bool TryGetCurrentCombatSize(FishSpriteHitbox hitbox, float configuredSize, float fallbackScale, out float size)
    {
        if (hitbox != null && hitbox.TryGetBodySizeMetricXZ(out size))
        {
            return true;
        }

        if (configuredSize > 0f)
        {
            size = configuredSize;
            return hitbox == null;
        }

        size = fallbackScale;
        return hitbox == null && size > 0f;
    }

    void EatFish(FishAI eatenFish, FishSpriteHitbox eatenFishHitbox)
    {
        if (eatenFish == null)
        {
            return;
        }

        score += eatenFish.scoreValue;
        float hungerRestore = GetHungerRestoreAmount(eatenFish, eatenFishHitbox);
        hunger = Mathf.Min(hunger + hungerRestore, 100f);

        float growthGain = GetGrowthGain(eatenFish, eatenFishHitbox);
        transform.localScale += new Vector3(growthGain, growthGain, growthGain);
        fishSize = transform.localScale.x;
        if (gameManager != null)
        {
            gameManager.UpdateHunger(hunger);
            gameManager.UpdateScore(score);
        }

        Destroy(eatenFish.gameObject);
    }

    float GetGrowthGain(FishAI eatenFish, FishSpriteHitbox eatenFishHitbox)
    {
        GetRelativePreySize(eatenFish, eatenFishHitbox, out float playerCurrentSize, out float preyCurrentSize, out float relativePreySize);

        if (playerCurrentSize <= Mathf.Epsilon || preyCurrentSize <= Mathf.Epsilon)
        {
            return growthAmount;
        }

        float growthFactor = Mathf.Lerp(minGrowthFactor, maxGrowthFactor, relativePreySize);
        return growthAmount * growthFactor;
    }

    float GetHungerRestoreAmount(FishAI eatenFish, FishSpriteHitbox eatenFishHitbox)
    {
        GetRelativePreySize(eatenFish, eatenFishHitbox, out float playerCurrentSize, out float preyCurrentSize, out float relativePreySize);

        if (playerCurrentSize <= Mathf.Epsilon || preyCurrentSize <= Mathf.Epsilon)
        {
            return hungerRestoreAmount;
        }

        float weightedRatio = Mathf.Pow(relativePreySize, hungerRestoreExponent);
        float hungerFactor = Mathf.Lerp(minHungerRestoreFactor, maxHungerRestoreFactor, weightedRatio);

        // Extra nerf for very small prey once the player is clown-fish sized or larger.
        if (relativePreySize <= smallPreyRelativeSizeThreshold)
        {
            float sizePenaltyT = Mathf.InverseLerp(
                smallPreyPenaltyStartSize,
                smallPreyPenaltyMaxSize,
                playerCurrentSize
            );

            float sizePenaltyMultiplier = Mathf.Lerp(1f, smallPreyMaxPenaltyMultiplier, sizePenaltyT);
            hungerFactor *= sizePenaltyMultiplier;
        }

        return hungerRestoreAmount * hungerFactor;
    }

    void GetRelativePreySize(FishAI eatenFish, FishSpriteHitbox eatenFishHitbox, out float playerCurrentSize, out float preyCurrentSize, out float relativePreySize)
    {
        playerCurrentSize = GetCurrentCombatSize(playerHitbox, fishSize, transform.localScale.x);
        preyCurrentSize = GetCurrentCombatSize(eatenFishHitbox, eatenFish.fishSize, eatenFish.transform.localScale.x);
        relativePreySize = playerCurrentSize > Mathf.Epsilon ? Mathf.Clamp01(preyCurrentSize / playerCurrentSize) : 0f;
    }

    static float GetCurrentCombatSize(FishSpriteHitbox hitbox, float configuredSize, float fallbackScale)
    {
        return TryGetCurrentCombatSize(hitbox, configuredSize, fallbackScale, out float size) ? size : 0f;
    }

    static bool TryEvaluateInteraction(
        FishSpriteHitbox playerHitbox,
        float playerConfiguredSize,
        float playerFallbackScale,
        FishSpriteHitbox fishHitbox,
        float fishConfiguredSize,
        float fishFallbackScale,
        float decisiveSizeMultiplier,
        out FishInteractionOutcome outcome,
        out float neutralBias)
    {
        outcome = FishInteractionOutcome.Neutral;
        neutralBias = 0f;

        if (!TryGetCurrentCombatSize(playerHitbox, playerConfiguredSize, playerFallbackScale, out float playerSize) ||
            !TryGetCurrentCombatSize(fishHitbox, fishConfiguredSize, fishFallbackScale, out float fishSize))
        {
            return false;
        }

        float clampedMultiplier = Mathf.Max(1f, decisiveSizeMultiplier);

        if (playerSize > fishSize * clampedMultiplier)
        {
            outcome = FishInteractionOutcome.PlayerEatsFish;
            neutralBias = 1f;
            return true;
        }

        if (fishSize > playerSize * clampedMultiplier)
        {
            outcome = FishInteractionOutcome.FishKillsPlayer;
            neutralBias = -1f;
            return true;
        }

        float playerAdvantageRatio = playerSize / Mathf.Max(fishSize, Mathf.Epsilon);

        if (playerAdvantageRatio >= 1f)
        {
            neutralBias = Mathf.InverseLerp(1f, clampedMultiplier, playerAdvantageRatio);
        }
        else
        {
            float fishAdvantageRatio = fishSize / Mathf.Max(playerSize, Mathf.Epsilon);
            neutralBias = -Mathf.InverseLerp(1f, clampedMultiplier, fishAdvantageRatio);
        }

        return true;
    }

    float GetMinimumRecoverySizeFloor()
    {
        float minimumEdibleSize = smallestRecoverablePreySize * Mathf.Max(1f, decisiveSizeMultiplier);
        return Mathf.Max(minFishSize, minimumEdibleSize + recoverySizeMargin);
    }
}
