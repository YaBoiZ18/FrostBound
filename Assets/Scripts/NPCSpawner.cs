using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    public GameObject npcPrefab;      // Assign your NPC prefab in the Inspector
    public Transform spawnPoint;      // Where the NPC will appear
    public float spawnInterval = 5f;  // Time between spawns

    private float timer;

    void Update()
    {
        // Timed spawning
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnNPC();
            timer = 0f;
        }
    }

    // Call this method from another script or event to spawn manually
    public void SpawnNPC()
    {

        Instantiate(npcPrefab, spawnPoint.position, Quaternion.identity);
    }
}
