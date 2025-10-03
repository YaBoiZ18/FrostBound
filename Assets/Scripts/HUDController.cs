using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    public static HUDController instance;

    private void Awake()
    {
        instance = this;
    }

    [SerializeField] TMP_Text interactionText;

    public void EnableInteraction(string text)
    {
        interactionText.text = text + "(F)";
        interactionText.gameObject.SetActive(true);
    }

    public void DisableInteraction(string text)
    {
        interactionText.gameObject.SetActive(false);
    }
}
