using System.Collections;
using UnityEngine;

public class TeleportManager : MonoBehaviour
{
    [Header("References")]
    public GameObject player;          // Player GameObject
    public GameObject npc;             // NPC GameObject
    public Transform playerDestination; // Where to teleport the player
    public Transform npcDestination;    // Where to teleport the NPC
    public SceneFader fader;

    private PlayerMovement playerMovement; // Reference to the PlayerMovement script

    void Start()
    {
        // Make sure we have a valid player reference before trying to access its components
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }
        else
        {
            Debug.LogError("Player reference is missing on TeleportManager!");
        }

        if (fader == null)
        {
            fader = FindObjectOfType<SceneFader>();
        }
    }

    void Update()
    {
        // Press V to trigger teleportation
        if (Input.GetKeyDown(KeyCode.V))
        {
            StartCoroutine(TeleportWithFade());
        }
    }

    IEnumerator TeleportWithFade()
    {
        if (player == null || npc == null || playerDestination == null || npcDestination == null)
        {
            Debug.LogError("One or more teleport references are missing!");
            yield break;
        }

        if (playerMovement != null)
            playerMovement.disabled = true;

        // Fade to black
        yield return StartCoroutine(FadeToBlack(1f)); // fade duration

        // Teleport
        player.transform.position = playerDestination.position;
        npc.transform.position = npcDestination.position;

        // Fade back in
        yield return StartCoroutine(FadeFromBlack(1f));

        if (playerMovement != null)
            playerMovement.disabled = false;
    }

    IEnumerator FadeToBlack(float duration)
    {
        float t = 0f;
        Color c = fader.image.color;

        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / duration);
            fader.image.color = c;
            yield return null;
        }
    }

    IEnumerator FadeFromBlack(float duration)
    {
        float t = 0f;
        Color c = fader.image.color;

        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = 1f - Mathf.Clamp01(t / duration);
            fader.image.color = c;
            yield return null;
        }
    }
}
