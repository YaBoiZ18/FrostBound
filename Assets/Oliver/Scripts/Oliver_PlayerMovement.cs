using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ensures the GameObject has a CharacterController component
[RequireComponent(typeof(CharacterController))]
public class Oliver_PlayerMovement : MonoBehaviour
{
    // Reference to the player's camera for mouse look
    public Camera playerCamera;

    // Movement speed settings
    public float walkSpeed = 6f;       // Speed when walking -> changed
    public float runSpeed = 12f;       // Speed when running -> changed
    public float gravity = 10f;        // Gravity force applied when falling
    // Mouse look settings
    public float lookSpeed = 2f;       // Mouse sensitivity
    public float lookXLimit = 45f;     // Vertical look limit
    // Crouch settings
    public float defaultHeight = 2f;   // Normal character height
    public float crouchHeight = 1f;    // Height when crouching
    public float crouchSpeed = 3f;     // Speed when crouched

    // Internal state variables
    //private Vector3 moveDirection = Vector3.zero; // Movement direction vector
    private Vector3 velocity = Vector3.zero;       // Replaces moveDirection
    private float rotationX = 0;                  // Vertical camera rotation
    private CharacterController characterController; // Reference to CharacterController

    private bool canMove = true; // Controls whether movement is allowed

    void Start()
    {
        // Get the CharacterController component attached to this GameObject
        characterController = GetComponent<CharacterController>();

        // NEW:
        characterController.detectCollisions = true;    // Collision detection
        characterController.minMoveDistance = 0f;   // Collides even with small movements

        // Lock and hide the cursor for immersive gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Get directional vectors relative to the player's orientation
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        #region // OLD:
        //// Check if the player is holding the run key (Left Shift)
        //bool isRunning = Input.GetKey(KeyCode.LeftShift);

        //// Calculate movement speed based on input and running state
        //float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0;
        //float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0;

        //// Preserve vertical movement (e.g., falling)
        //// float movementDirectionY = moveDirection.y;
        //float movementDirectionY = velocity.y; // replaces moveDirection

        //// Combine forward and sideways movement
        //// moveDirection = (forward * curSpeedX) + (right * curSpeedY);
        //velocity = (forward * curSpeedX) + (right * curSpeedY); // replaces moveDirection

        //// Apply preserved vertical movement
        //moveDirection.y = movementDirectionY;

        //// Apply gravity when not grounded
        //if (!characterController.isGrounded)
        //{
        //    moveDirection.y -= gravity * Time.deltaTime;
        //}

        //// Handle crouching when the crouch key (R) is held
        //if (Input.GetKey(KeyCode.LeftControl) && canMove)
        //{
        //    characterController.height = crouchHeight; // Set crouch height
        //    walkSpeed = crouchSpeed;                  // Reduce walk speed
        //    runSpeed = crouchSpeed;                   // Reduce run speed
        //}
        //else
        //{
        //    // Reset to default height and speed when not crouching
        //    characterController.height = defaultHeight;
        //    walkSpeed = 6f;
        //    runSpeed = 12f;
        //}

        //// Move the character based on calculated direction
        //characterController.Move(moveDirection * Time.deltaTime);

        //// Handle mouse look if movement is allowed
        //if (canMove)
        //{
        //    // Vertical camera rotation (look up/down)
        //    rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        //    rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit); // Clamp to prevent over-rotation
        //    playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

        //    // Horizontal player rotation (turn left/right)
        //    transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        #endregion

        #region // NEW:

        // Check if the player is holding the run key
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        // select target speed based on running state
        // this way is less resource intensive than multiplying input each frame
        float targetSpeed = isRunning ? runSpeed : walkSpeed;

        // Handle crouching when the crouch key is held
        bool isCrouching = Input.GetKey(KeyCode.LeftControl);
        if (isCrouching && canMove)
        {
            characterController.height = crouchHeight;  // Set crouch height
            targetSpeed = Mathf.Min(targetSpeed, crouchSpeed); // clamp speed while crouched
        }
        else
        {
            characterController.height = defaultHeight; // reset height
        }

        // input
        float inputV = canMove ? Input.GetAxisRaw("Vertical") : 0f;
        float inputH = canMove ? Input.GetAxisRaw("Horizontal") : 0f;

        // Build a desired horizontal direction and normalize to avoid faster diagonals
        Vector3 wishDir = (forward * inputV + right * inputH);
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

        // Horizontal velocity we want
        Vector3 horizVel = wishDir * targetSpeed;

        // gravity
        if (characterController.isGrounded)
        {
            // small downward stick keeps you snapped to ground & slopes
            if (velocity.y < 0f) velocity.y = -2f;
        }
        else
        {
            velocity.y += -Mathf.Abs(gravity) * Time.deltaTime;
        }

        // Combine horizontal + vertical
        velocity.x = horizVel.x;
        velocity.z = horizVel.z;

        // Move the character (THIS collides with walls)
        characterController.Move(velocity * Time.deltaTime);

        // Handle mouse look if movement is allowed
        if (canMove && playerCamera)
        {
            // Vertical camera rotation (look up/down)
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit); // Clamp to prevent over-rotation
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

            // Horizontal player rotation (turn left/right)
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }
        #endregion

    }
}

