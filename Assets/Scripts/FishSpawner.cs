using UnityEngine;

public class FishSpawner : MonoBehaviour
{
    public Transform player;

    // Add one entry for each species prefab. The spawner instantiates these directly.
    public FishSpawnEntry[] speciesPrefabs;

    public int targetFishCount = 45;
    public float spawnCheckInterval = 2f;
    public float spawnNearPlayerChance = 0.75f;
    public float edibleSpawnChance = 0.65f;
    public float minSpawnDistanceFromPlayer = 8f;
    public float maxSpawnDistanceFromPlayer = 22f;

    public float minX = -60f;
    public float maxX = 60f;
    public float minZ = -34f;
    public float maxZ = 34f;
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
        FishAI[] allFish = FindObjectsByType<FishAI>(FindObjectsSortMode.None);

        while (allFish.Length < targetFishCount)
        {
            SpawnFish();
            allFish = FindObjectsByType<FishAI>(FindObjectsSortMode.None);
        }
    }

    void SpawnFish()
    {
        Vector3 spawnPosition = GetSpawnPosition();

        GameObject fishPrefab = GetPrefabForSpawn();

        if (fishPrefab != null)
        {
            Instantiate(fishPrefab, spawnPosition, Quaternion.identity);
        }
    }

    Vector3 GetSpawnPosition()
    {
        if (player != null && Random.value < spawnNearPlayerChance)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minSpawnDistanceFromPlayer, maxSpawnDistanceFromPlayer);

            float spawnX = player.position.x + direction.x * distance;
            float spawnZ = player.position.z + direction.y * distance;

            return new Vector3(
                Mathf.Clamp(spawnX, minX, maxX),
                fixedY,
                Mathf.Clamp(spawnZ, minZ, maxZ)
            );
        }

        return new Vector3(
            Random.Range(minX, maxX),
            fixedY,
            Random.Range(minZ, maxZ)
        );
    }

    GameObject GetPrefabForSpawn()
    {
        // Most spawns try to keep edible fish available near the player.
        // If none are edible yet, the spawner falls back to the full species list.
        if (player != null && Random.value < edibleSpawnChance)
        {
            GameObject ediblePrefab = GetRandomEdiblePrefab(player.localScale.x);

            if (ediblePrefab != null)
            {
                return ediblePrefab;
            }
        }

        return GetRandomPrefab();
    }

    GameObject GetRandomEdiblePrefab(float playerSize)
    {
        if (speciesPrefabs == null || speciesPrefabs.Length == 0)
        {
            return null;
        }

        int totalWeight = 0;

        foreach (FishSpawnEntry entry in speciesPrefabs)
        {
            FishAI fishAI = GetPrefabFishAI(entry);

            if (fishAI != null && fishAI.fishSize < playerSize)
            {
                totalWeight += Mathf.Max(0, entry.spawnWeight);
            }
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int roll = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (FishSpawnEntry entry in speciesPrefabs)
        {
            FishAI fishAI = GetPrefabFishAI(entry);

            if (fishAI == null || fishAI.fishSize >= playerSize)
            {
                continue;
            }

            currentWeight += Mathf.Max(0, entry.spawnWeight);

            if (roll < currentWeight)
            {
                return entry.fishPrefab;
            }
        }

        return null;
    }

    GameObject GetRandomPrefab()
    {
        if (speciesPrefabs == null || speciesPrefabs.Length == 0)
        {
            return null;
        }

        int totalWeight = 0;

        foreach (FishSpawnEntry entry in speciesPrefabs)
        {
            if (entry != null && entry.fishPrefab != null)
            {
                totalWeight += Mathf.Max(0, entry.spawnWeight);
            }
        }

        if (totalWeight <= 0)
        {
            return speciesPrefabs[Random.Range(0, speciesPrefabs.Length)].fishPrefab;
        }

        int roll = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (FishSpawnEntry entry in speciesPrefabs)
        {
            if (entry == null || entry.fishPrefab == null)
            {
                continue;
            }

            currentWeight += Mathf.Max(0, entry.spawnWeight);

            if (roll < currentWeight)
            {
                return entry.fishPrefab;
            }
        }

        return speciesPrefabs[speciesPrefabs.Length - 1].fishPrefab;
    }

    FishAI GetPrefabFishAI(FishSpawnEntry entry)
    {
        if (entry == null || entry.fishPrefab == null)
        {
            return null;
        }

        return entry.fishPrefab.GetComponent<FishAI>();
    }
}
