using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactRange = 3f; // Max distance to detect interactables

    private Interactable currentInteractable;

    void Update()
    {
        CheckForInteractable();
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
            currentInteractable = null;
        }
    }
}
