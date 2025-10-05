using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactRange = 3f; // Max distance to detect interactables

    public KeyCode interactKey = KeyCode.F;

    private Interactable currentInteractable;

    void Update()
    {
        CheckForInteractable();

        if (Input.GetKeyDown(interactKey) && currentInteractable != null)
        {
            currentInteractable.Interact(this);
        }
    }

    void CheckForInteractable()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        // Cast ray forward from the camera
        if (Physics.Raycast(ray, out hit, interactRange))
        {
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();

            if (interactable != null)
            {
                // If we're looking at a new interactable
                if (currentInteractable != interactable)
                {
                    ClearCurrentInteractable();

                    currentInteractable = interactable;
                    currentInteractable.EnableOutLine();


                    // Show interaction message
                    HUDController.instance.EnableInteraction(currentInteractable.message);

                }
                return;
            }
        }

        // If no interactable found, clear the current one
        ClearCurrentInteractable();
    }

    void ClearCurrentInteractable()
    {
        if (currentInteractable != null)
        {
            currentInteractable.DisableOutLine();
            HUDController.instance.DisableInteraction(""); // You can ignore the parameter if not needed
            currentInteractable = null;
        }
    }
}
