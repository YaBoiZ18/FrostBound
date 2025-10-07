using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    public GameObject npcPrefab;      // Assign your NPC prefab in the Inspector
    public Transform spawnPoint;      // Where the NPC will appear
    public float spawnInterval = 5f;  // Time between spawns
    public int spawnLimit = 10;       // Maximum number of NPCs to spawn

    private float timer;
    private int spawnedCount = 0;

    void Update()
    {
        // Timed spawning
        timer += Time.deltaTime;
        if (timer >= spawnInterval && spawnedCount < spawnLimit)
        {
            SpawnNPC();
            timer = 0f;
        }
    }

    // Call this method from another script or event to spawn manually
    public void SpawnNPC()
    {
        if (spawnedCount >= spawnLimit)
            return;

        Instantiate(npcPrefab, spawnPoint.position, Quaternion.identity);
        spawnedCount++;
    }
}