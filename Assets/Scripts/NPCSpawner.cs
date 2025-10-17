using UnityEngine;
using System.Collections.Generic;

public class NPCSpawner : MonoBehaviour
{
    public GameObject[] npcPrefabs;       // Assign 3 NPC prefabs in the Inspector
    public Transform spawnPoint;          // Where the NPC will appear
    public float spawnInterval = 5f;      // Time between spawns

    private float timer;
    private List<GameObject> remainingPrefabs = new List<GameObject>();

    void Start()
    {
        // Initialize the list with all prefabs
        remainingPrefabs.AddRange(npcPrefabs);
    }

    void Update()
    {
        // Timed spawning
        timer += Time.deltaTime;
        if (timer >= spawnInterval && remainingPrefabs.Count > 0)
        {
            SpawnNPC();
            timer = 0f;
        }
    }

    // Call this method from another script or event to spawn manually
    public void SpawnNPC()
    {
        if (remainingPrefabs.Count == 0)
            return;

        // Randomly select and remove a prefab from the list
        int index = Random.Range(0, remainingPrefabs.Count);
        GameObject selectedPrefab = remainingPrefabs[index];
        remainingPrefabs.RemoveAt(index);

        Instantiate(selectedPrefab, spawnPoint.position, Quaternion.identity);
    }
}