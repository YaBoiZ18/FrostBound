using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    // Reference to the UI Image used for the fade effect (should cover the whole screen).
    public Image image;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Starts the fade-out effect when the scene begins.
    /// </summary>
    private void Start()
    {
        StartCoroutine(FadeOut());
    }

    /// <summary>
    /// Public method to trigger a fade-in effect and load a new scene.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    /// <param name="duration">The duration of the fade effect in seconds.</param>
    public void FadeAndLoad(string sceneName, float duration)
    {
        StartCoroutine(Fader(sceneName, duration));
    }

    /// <summary>
    /// Coroutine that gradually fades the screen to black and then loads the specified scene.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    /// <param name="duration">The duration of the fade effect.</param>
    IEnumerator Fader(string sceneName, float duration)
    {
        float t = 0f; // Timer to track fade progress
        Color c = image.color; // Get the current color of the image

        // Gradually increase the alpha value to 1 (fully opaque)
        while (t < duration)
        {
            t += Time.deltaTime; // Increment timer by the time since last frame
            c.a = t / duration; // Calculate new alpha value
            image.color = c; // Apply the new color to the image
            yield return null; // Wait for the next frame
        }

        // Load the new scene after fade is complete
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Coroutine that fades the screen from black to transparent at the start of the scene.
    /// </summary>
    IEnumerator FadeOut()
    {
        float t = 0f;
        Color c = image.color;

        // Gradually decrease the alpha value to 0 (fully transparent)
        while (t < 1f)
        {
            t += Time.deltaTime;
            c.a = 1f - (t / 1f); // Inverse of fade-in
            image.color = c;
            yield return null;
        }
    }

    /// <summary>
    /// Checks for key input to trigger scene transitions for testing purposes. Can be changed for later stuff.
    /// </summary>
    private void Update()
    {
        // Press 'A' to fade to "FadetoBlackTesting" scene
        if (Input.GetKeyDown(KeyCode.A))
        {
            FadeAndLoad("FadetoBlackTesting", 1f);
        }

        // Press 'B' to fade to "FadetoBlackTestingPrt2" scene
        if (Input.GetKeyDown(KeyCode.B))
        {
            FadeAndLoad("FadetoBlackTestingPrt2", 1f);
        }
    }
}