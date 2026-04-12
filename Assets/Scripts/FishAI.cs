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

    private Vector3 moveDirection;
    private float directionTimer;

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

        if (moveDirection != Vector3.zero)
        {
            transform.forward = moveDirection;
        }
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
}
