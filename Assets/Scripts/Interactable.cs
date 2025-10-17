using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    Outline outline;
    public string message;

    public virtual void Interact(PlayerInteraction interaction) 
    {
        onInteraction.Invoke();
    }

    public UnityEvent onInteraction;

    void Start()
    {
        outline = GetComponent<Outline>();
        DisableOutLine();
    }

    public void Interact() 
    {
        onInteraction.Invoke();
    }

    public void DisableOutLine()
    {
        outline.enabled = false;
    }

    public void EnableOutLine()
    {
        outline.enabled = true;
    }

}
