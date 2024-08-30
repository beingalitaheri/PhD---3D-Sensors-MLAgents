using UnityEngine;

public class PrefabSpawner : MonoBehaviour
{
    public GameObject prefabToSpawn; // Assign your prefab in the Unity Editor
    public int numberOfPrefabs = 10; // Number of prefabs to spawn
    public Vector3 spawnArea = new Vector3(10, 10, 10); // Size of the area to spawn prefabs within

    void Start()
    {
        SpawnPrefabs();
    }

    void SpawnPrefabs()
    {
        for (int i = 0; i < numberOfPrefabs; i++)
        {
            // Generate a random position within the defined area
            Vector3 randomPosition = new Vector3(
                Random.Range(-spawnArea.x / 2, spawnArea.x / 2),
                Random.Range(-spawnArea.y / 2, spawnArea.y / 2),
                Random.Range(-spawnArea.z / 2, spawnArea.z / 2)
            );

            // Spawn the prefab at the random position
            Instantiate(prefabToSpawn, randomPosition, Quaternion.identity, transform);
        }
    }
}
