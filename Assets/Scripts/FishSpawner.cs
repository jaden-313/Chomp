using UnityEngine;

public class FishSpawner : MonoBehaviour
{
    public Transform player;
    public bool enableSpawnDebugLogs = false;

    // Add one entry for each species prefab. The spawner instantiates these directly.
    public FishSpawnEntry[] speciesPrefabs;

    public int targetFishCount = 45;
    public float spawnCheckInterval = 2f;
    public float spawnNearPlayerChance = 0.55f;
    public float edibleSpawnChance = 0.65f;
    public float minSpawnDistanceFromPlayer = 8f;
    public float maxSpawnDistanceFromPlayer = 22f;
    public float outerAreaSpawnChance = 0.35f;
    public float outerAreaInsetFraction = 0.28f;

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
            GameObject spawnedFish = Instantiate(fishPrefab, spawnPosition, Quaternion.identity);
            LogSpawnDebug(fishPrefab, spawnedFish);
        }
    }

    void LogSpawnDebug(GameObject fishPrefab, GameObject spawnedFish)
    {
        if (!enableSpawnDebugLogs)
        {
            return;
        }

        if (spawnedFish == null)
        {
            Debug.LogWarning($"[FishSpawn] Failed to instantiate prefab '{fishPrefab?.name ?? "null"}'.");
            return;
        }

        SpriteRenderer spriteRenderer = spawnedFish.GetComponentInChildren<SpriteRenderer>();
        string spriteName = spriteRenderer != null && spriteRenderer.sprite != null ? spriteRenderer.sprite.name : "null";
        string sortingLayerName = spriteRenderer != null ? SortingLayer.IDToName(spriteRenderer.sortingLayerID) : "none";
        string colorText = spriteRenderer != null ? spriteRenderer.color.ToString() : "n/a";
        string scaleText = spawnedFish.transform.lossyScale.ToString("F3");
        string positionText = spawnedFish.transform.position.ToString("F3");

        Debug.Log(
            $"[FishSpawn] prefab='{fishPrefab.name}' instance='{spawnedFish.name}' sprite='{spriteName}' " +
            $"rendererEnabled={(spriteRenderer != null && spriteRenderer.enabled)} color={colorText} alpha={(spriteRenderer != null ? spriteRenderer.color.a : 0f):F2} " +
            $"sortingLayer='{sortingLayerName}' order={(spriteRenderer != null ? spriteRenderer.sortingOrder : 0)} " +
            $"scale={scaleText} position={positionText}",
            spawnedFish
        );
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

        if (Random.value < outerAreaSpawnChance)
        {
            return GetOuterAreaSpawnPosition();
        }

        return new Vector3(
            Random.Range(minX, maxX),
            fixedY,
            Random.Range(minZ, maxZ)
        );
    }

    Vector3 GetOuterAreaSpawnPosition()
    {
        float insetX = Mathf.Clamp((maxX - minX) * outerAreaInsetFraction, 0f, Mathf.Max(0f, (maxX - minX) * 0.5f - 1f));
        float insetZ = Mathf.Clamp((maxZ - minZ) * outerAreaInsetFraction, 0f, Mathf.Max(0f, (maxZ - minZ) * 0.5f - 1f));

        float innerMinX = minX + insetX;
        float innerMaxX = maxX - insetX;
        float innerMinZ = minZ + insetZ;
        float innerMaxZ = maxZ - insetZ;

        bool spawnAlongVerticalEdge = Random.value < 0.5f;
        float spawnX;
        float spawnZ;

        if (spawnAlongVerticalEdge)
        {
            bool leftSide = Random.value < 0.5f;
            spawnX = leftSide ? Random.Range(minX, innerMinX) : Random.Range(innerMaxX, maxX);
            spawnZ = Random.Range(minZ, maxZ);
        }
        else
        {
            bool bottomSide = Random.value < 0.5f;
            spawnX = Random.Range(minX, maxX);
            spawnZ = bottomSide ? Random.Range(minZ, innerMinZ) : Random.Range(innerMaxZ, maxZ);
        }

        return new Vector3(spawnX, fixedY, spawnZ);
    }

    GameObject GetPrefabForSpawn()
    {
        // Most spawns try to keep edible fish available near the player.
        // If none are edible yet, the spawner falls back to the full species list.
        if (player != null)
        {
            float playerSize = player.localScale.x;
            int edibleSpeciesCount = GetEdibleSpeciesCount(playerSize);
            float adaptiveEdibleChance = GetAdaptiveEdibleSpawnChance(edibleSpeciesCount);

            if (Random.value < adaptiveEdibleChance)
            {
                GameObject ediblePrefab = GetRandomEdiblePrefab(playerSize);

                if (ediblePrefab != null)
                {
                    return ediblePrefab;
                }
            }
        }

        return GetRandomPrefab();
    }

    int GetEdibleSpeciesCount(float playerSize)
    {
        if (speciesPrefabs == null || speciesPrefabs.Length == 0)
        {
            return 0;
        }

        int count = 0;

        foreach (FishSpawnEntry entry in speciesPrefabs)
        {
            FishAI fishAI = GetPrefabFishAI(entry);

            if (fishAI != null && fishAI.fishSize < playerSize)
            {
                count++;
            }
        }

        return count;
    }

    float GetAdaptiveEdibleSpawnChance(int edibleSpeciesCount)
    {
        if (edibleSpeciesCount <= 0)
        {
            return 0f;
        }

        if (edibleSpeciesCount == 1)
        {
            return edibleSpawnChance * 0.35f;
        }

        if (edibleSpeciesCount == 2)
        {
            return edibleSpawnChance * 0.65f;
        }

        return edibleSpawnChance;
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
