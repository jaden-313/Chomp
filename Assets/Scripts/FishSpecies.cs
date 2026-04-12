using UnityEngine;

// This class appears in the FishSpawner Inspector.
// Each entry points to one species prefab and controls how common it is.
[System.Serializable]
public class FishSpawnEntry
{
    public GameObject fishPrefab;
    public int spawnWeight = 1;
}
