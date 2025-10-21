using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;     // The camera used for first-person view
    private CharacterController controller;           // Cached CharacterController reference

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3f;    // Default walking speed
    [SerializeField] private float runSpeed = 5f;     // Speed when running
    [SerializeField] private float gravity = 10f;     // Downward force applied when airborne

    [Header("Mouse Look Settings")]
    [SerializeField] private float lookSpeed = 2f;    // Sensitivity for mouse movement
    [SerializeField] private float lookXLimit = 45f;  // Vertical rotation clamp (prevents flipping camera)

    [Header("Crouch Settings")]
    [SerializeField] private float defaultHeight = 2f;  // Standing height
    [SerializeField] private float crouchHeight = 1f;   // Height when crouched
    [SerializeField] private float crouchSpeed = 3f;    // Movement speed while crouched


    private Vector3 moveDirection = Vector3.zero;     // Current player velocity vector
    private float rotationX = 0f;                     // Vertical camera rotation value
    private bool canMove = true;                      // Determines if movement and look are active

    public bool disabled = false;


    void Start()
    {
        // Cache the CharacterController reference
        controller = GetComponent<CharacterController>();

        // Lock and hide the cursor for first-person gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!disabled)
        {
            // Handle player motion and view rotation each frame
            HandleMovement();
            HandleMouseLook();
        }
       
    }

    private void HandleMovement()
    {
        // Calculate direction vectors relative to player facing direction
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        // Check player inputs
        bool isRunning = Input.GetKey(KeyCode.LeftShift);    // Hold Left Shift to run
        bool isCrouching = Input.GetKey(KeyCode.LeftControl); // Hold Left Ctrl to crouch

        // Determine desired movement speed based on input and running state
        float currentSpeedX = canMove ? GetCurrentSpeed(isRunning) * Input.GetAxis("Vertical") : 0f;
        float currentSpeedY = canMove ? GetCurrentSpeed(isRunning) * Input.GetAxis("Horizontal") : 0f;

        // Preserve vertical (Y-axis) velocity for jumping or falling
        float verticalVelocity = moveDirection.y;

        // Combine forward/backward and strafe movement
        moveDirection = (forward * currentSpeedX) + (right * currentSpeedY);
        moveDirection.y = verticalVelocity;

        // Apply gravity when not grounded
        if (!controller.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;

        // Handle crouch input and adjust height/speed
        HandleCrouch(isCrouching);

        // Finally, apply movement to the CharacterController
        controller.Move(moveDirection * Time.deltaTime);
    }

    // Returns the correct movement speed depending on whether the player is running or walking
    private float GetCurrentSpeed(bool isRunning)
    {
        return isRunning ? runSpeed : walkSpeed;
    }

    // Handles crouching logic, adjusting character height and speed
    private void HandleCrouch(bool isCrouching)
    {
        if (isCrouching && canMove)
        {
            // Reduce height and speed when crouched
            controller.height = crouchHeight;
            walkSpeed = crouchSpeed;
            runSpeed = crouchSpeed;
        }
        else
        {
            // Restore default height and speed when not crouching
            controller.height = defaultHeight;
            walkSpeed = 3f;
            runSpeed = 5f;
        }
    }

    private void HandleMouseLook()
    {
        // Skip look logic if player movement is disabled
        if (!canMove) return;

        // Vertical camera rotation (look up/down)
        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        // Apply rotation to camera’s local transform
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

        // Horizontal rotation (turn player body left/right)
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
    }
}
