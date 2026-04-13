using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public float moveSpeed = 6f;
    public float minSpeed = 2f;
    public float maxSpeed = 8f;

    public float hunger = 100f;
    public float hungerDrainRate = 2f;
    public float hungerRestoreAmount = 20f;

    public int score = 0;
    public float growthAmount = 0.1f;
    public float fixedY = 0.5f;

    public float fishSize = 1f;
    public float minFishSize = 0.5f;

    public float shrinkThreshold = 50f;
    public float shrinkRate = 0.3f;
    public float visualShrinkRate = 0.12f;

    private GameManager gameManager;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();

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
    }

    void MovePlayer()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(h, 0f, v);

        transform.position += move * moveSpeed * Time.deltaTime;

        float clampedX = Mathf.Clamp(transform.position.x, -24f, 24f);
        float clampedZ = Mathf.Clamp(transform.position.z, -24f, 24f);

        transform.position = new Vector3(clampedX, fixedY, clampedZ);

        if (move != Vector3.zero)
        {
            //transform.forward = move;
            if (h > 0)
                spriteRenderer.flipX = false;
            else if (h < 0)
                spriteRenderer.flipX = true;
        }
    }

    void DrainHunger()
    {
        hunger -= hungerDrainRate * Time.deltaTime;
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
            fishSize -= shrinkRate * Time.deltaTime;
            fishSize = Mathf.Max(fishSize, minFishSize);

            Vector3 minScale = new Vector3(minFishSize, minFishSize, minFishSize);

            transform.localScale = Vector3.Max(
                transform.localScale - new Vector3(visualShrinkRate, visualShrinkRate, visualShrinkRate) * Time.deltaTime,
                minScale
            );

            moveSpeed = Mathf.Min(maxSpeed, moveSpeed + 0.1f * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        FishAI otherFish = other.GetComponent<FishAI>();

        if (otherFish != null)
        {
            float mySize = transform.localScale.x;
            float otherSize = other.transform.localScale.x;

            if (mySize > otherSize)
            {
                EatFish(other.gameObject, otherFish.fishSize);
            }
            else if (otherSize >= mySize * 1.2f)
            {
                if (gameManager != null)
                {
                    gameManager.GameOver();
                }
            }
            else
            {
                // sizes are too close, so nothing happens
            }
        }
    }

    void EatFish(GameObject fishObject, float eatenFishSize)
    {
        score++;
        hunger = Mathf.Min(hunger + hungerRestoreAmount, 100f);

        transform.localScale += new Vector3(growthAmount, growthAmount, growthAmount);
        fishSize = transform.localScale.x;

        moveSpeed = Mathf.Max(minSpeed, moveSpeed - 0.2f);

        if (gameManager != null)
        {
            gameManager.UpdateScore(score);
        }

        Destroy(fishObject);
    }
}