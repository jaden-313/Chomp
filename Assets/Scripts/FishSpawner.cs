using UnityEngine;

public class FishSpawner : MonoBehaviour
{
    public GameObject aiFishPrefab;

    public int targetFishCount = 12;
    public float spawnCheckInterval = 2f;

    public float minX = -20f;
    public float maxX = 20f;
    public float minZ = -20f;
    public float maxZ = 20f;
    public float fixedY = 0.5f;

    private float timer;

    void Start()
    {
        SpawnUntilTarget();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnCheckInterval)
        {
            timer = 0f;
            SpawnUntilTarget();
        }
    }

    void SpawnUntilTarget()
    {
        FishAI[] allFish = FindObjectsOfType<FishAI>();

        while (allFish.Length < targetFishCount)
        {
            SpawnFish();
            allFish = FindObjectsOfType<FishAI>();
        }
    }

    void SpawnFish()
    {
        Vector3 spawnPosition = new Vector3(
            Random.Range(minX, maxX),
            fixedY,
            Random.Range(minZ, maxZ)
        );

        GameObject newFish = Instantiate(aiFishPrefab, spawnPosition, Quaternion.identity);

        FishAI fishAI = newFish.GetComponent<FishAI>();

        if (fishAI != null)
        {
            float randomSize = GetWeightedFishSize();
            float randomSpeed = GetSpeedFromSize(randomSize);

            fishAI.SetFishStats(randomSize, randomSpeed);
        }
    }

    float GetWeightedFishSize()
    {
        float roll = Random.value;

        if (roll < 0.65f)
        {
            // 65% clearly edible
            return Random.Range(0.35f, 0.70f);
        }
        else if (roll < 0.82f)
        {
            // 17% slightly smaller / medium
            return Random.Range(0.70f, 0.90f);
        }
        else if (roll < 0.95f)
        {
            // 13% dangerous but not huge
            return Random.Range(1.20f, 1.80f);
        }
        else
        {
            // 5% giant fish
            return Random.Range(2.2f, 3.5f);
        }
    }

    float GetSpeedFromSize(float size)
    {
        if (size < 0.95f)
        {
            return Random.Range(2.0f, 3.0f);
        }
        else if (size < 1.4f)
        {
            return Random.Range(1.5f, 2.3f);
        }
        else
        {
            return Random.Range(1.0f, 1.6f);
        }
    }
}