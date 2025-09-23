using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ensures the GameObject has a CharacterController component
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // Reference to the player's camera for mouse look
    public Camera playerCamera;

    // Movement speed settings
    public float walkSpeed = 6f;       // Speed when walking
    public float runSpeed = 12f;       // Speed when running
    public float gravity = 10f;        // Gravity force applied when falling
    // Mouse look settings
    public float lookSpeed = 2f;       // Mouse sensitivity
    public float lookXLimit = 45f;     // Vertical look limit
    // Crouch settings
    public float defaultHeight = 2f;   // Normal character height
    public float crouchHeight = 1f;    // Height when crouching
    public float crouchSpeed = 3f;     // Speed when crouched

    // Internal state variables
    private Vector3 moveDirection = Vector3.zero; // Movement direction vector
    private float rotationX = 0;                  // Vertical camera rotation
    private CharacterController characterController; // Reference to CharacterController

    private bool canMove = true; // Controls whether movement is allowed

    void Start()
    {
        // Get the CharacterController component attached to this GameObject
        characterController = GetComponent<CharacterController>();

        // Lock and hide the cursor for immersive gameplay
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // Get directional vectors relative to the player's orientation
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        // Check if the player is holding the run key (Left Shift)
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        // Calculate movement speed based on input and running state
        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0;

        // Preserve vertical movement (e.g., falling)
        float movementDirectionY = moveDirection.y;

        // Combine forward and sideways movement
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        // Apply preserved vertical movement
        moveDirection.y = movementDirectionY;

        // Apply gravity when not grounded
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // Handle crouching when the crouch key (R) is held
        if (Input.GetKey(KeyCode.LeftControl) && canMove)
        {
            characterController.height = crouchHeight; // Set crouch height
            walkSpeed = crouchSpeed;                  // Reduce walk speed
            runSpeed = crouchSpeed;                   // Reduce run speed
        }
        else
        {
            // Reset to default height and speed when not crouching
            characterController.height = defaultHeight;
            walkSpeed = 6f;
            runSpeed = 12f;
        }

        // Move the character based on calculated direction
        characterController.Move(moveDirection * Time.deltaTime);

        // Handle mouse look if movement is allowed
        if (canMove)
        {
            // Vertical camera rotation (look up/down)
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit); // Clamp to prevent over-rotation
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

            // Horizontal player rotation (turn left/right)
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }
    }
}
