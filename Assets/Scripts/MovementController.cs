using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool usePhysicsMovement = true;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float gravityScale = 3f;
    [SerializeField] private LayerMask groundLayerMask = -1; // Default to everything
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private bool debugGroundCheck = true;
    
    [Header("Jump Feel")]
    [SerializeField] private float coyoteTime = 0.1f; // Time after leaving ground where you can still jump
    [SerializeField] private float jumpBufferTime = 0.1f; // Time before landing where jump input is remembered
    
    [Header("Optional: Smooth Movement")]
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float airControl = 0.5f; // How much control you have in the air
    
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    
    // Components
    private Rigidbody2D rb2d;
    
    // Input Actions
    private InputAction moveAction;
    private InputAction jumpAction;
    
    // Input
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    private bool isGrounded;
    private bool wasGrounded;
    private bool jumpPressed;
    
    // Jump timing
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    
    void Start()
    {
        // Get the Rigidbody2D component
        rb2d = GetComponent<Rigidbody2D>();
        
        // If using physics movement but no Rigidbody2D exists, add one
        if (usePhysicsMovement && rb2d == null)
        {
            rb2d = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // Set up gravity for platformer movement
        if (rb2d != null)
        {
            rb2d.gravityScale = gravityScale;
            rb2d.freezeRotation = true; // Prevent rotation
        }
        
        // Create ground check if it doesn't exist
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            // Position it at the bottom of the collider
            Collider2D playerCollider = GetComponent<Collider2D>();
            if (playerCollider != null)
            {
                float yOffset = -playerCollider.bounds.extents.y - 0.1f;
                groundCheckObj.transform.localPosition = new Vector3(0, yOffset, 0);
            }
            else
            {
                groundCheckObj.transform.localPosition = new Vector3(0, -0.5f, 0);
            }
            groundCheck = groundCheckObj.transform;
        }
        
        // Setup Input Actions
        SetupInputActions();
    }
    
    void Update()
    {
        HandleInput();
        CheckGrounded();
        UpdateJumpTimers();
    }
    
    void FixedUpdate()
    {
        if (usePhysicsMovement && rb2d != null)
        {
            HandlePhysicsMovement();
        }
        else
        {
            HandleTransformMovement();
        }
    }
    
    private void SetupInputActions()
    {
        // If no input actions asset is assigned, create input actions directly
        if (inputActions == null)
        {
            Debug.LogWarning("No InputActionAsset assigned. Please assign the InputSystem_Actions asset in the inspector for full functionality.");
            
            // Create a simple input action as fallback
            var actionMap = new InputActionMap("Player");
            
            // Horizontal movement only (no vertical)
            moveAction = actionMap.AddAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
            
            // Jump action
            jumpAction = actionMap.AddAction("Jump", InputActionType.Button, binding: "<Keyboard>/space");
            
            actionMap.Enable();
            return;
        }
        
        // Get the actions from the Player action map
        var playerActionMap = inputActions.FindActionMap("Player");
        if (playerActionMap != null)
        {
            moveAction = playerActionMap.FindAction("Move");
            jumpAction = playerActionMap.FindAction("Jump");
            
            // If no Jump action exists, try common alternatives
            if (jumpAction == null)
            {
                jumpAction = playerActionMap.FindAction("Attack"); // Fallback to Attack if no Jump
                if (jumpAction != null)
                {
                    Debug.LogWarning("No 'Jump' action found, using 'Attack' action for jumping.");
                }
            }
            
            playerActionMap.Enable();
        }
        else
        {
            Debug.LogError("Player action map not found in the assigned InputActionAsset!");
        }
    }
    
    private void HandleInput()
    {
        if (moveAction != null)
        {
            // Get horizontal input only
            if (moveAction.expectedControlType == "Vector2")
            {
                Vector2 input = moveAction.ReadValue<Vector2>();
                moveInput = new Vector2(input.x, 0); // Only use X axis
            }
            else
            {
                float horizontalInput = moveAction.ReadValue<float>();
                moveInput = new Vector2(horizontalInput, 0);
            }
        }
        
        // Handle jump input
        if (jumpAction != null)
        {
            jumpPressed = jumpAction.WasPressedThisFrame();
        }
    }
    
    private void CheckGrounded()
    {
        // Store previous grounded state
        wasGrounded = isGrounded;
        
        // Check if the player is touching the ground
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayerMask);
        
        // Debug output (less frequent to avoid spam)
        if (debugGroundCheck && Time.frameCount % 30 == 0) // Only every 30 frames
        {
            Debug.Log($"Ground Check - Position: {groundCheck.position}, IsGrounded: {isGrounded}, LayerMask: {groundLayerMask.value}");
            
            // Check what we're actually hitting
            Collider2D hit = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayerMask);
            if (hit != null)
            {
                Debug.Log($"Hit object: {hit.name} on layer: {hit.gameObject.layer}");
            }
        }
    }
    
    private void UpdateJumpTimers()
    {
        // Coyote Time: Grace period after leaving ground
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        
        // Jump Buffer: Remember jump input for a short time
        if (jumpPressed)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }
    
    private void HandlePhysicsMovement()
    {
        // Handle horizontal movement
        float targetHorizontalVelocity = moveInput.x * moveSpeed;
        float currentHorizontalVelocity = rb2d.linearVelocity.x;
        
        // Apply air control factor if not grounded
        float controlFactor = isGrounded ? 1f : airControl;
        float accelerationRate = moveInput.x != 0 ? acceleration : deceleration;
        
        // Smooth horizontal movement
        float newHorizontalVelocity = Mathf.MoveTowards(
            currentHorizontalVelocity, 
            targetHorizontalVelocity, 
            accelerationRate * controlFactor * Time.fixedDeltaTime
        );
        
        // Handle jumping with coyote time and jump buffering
        float verticalVelocity = rb2d.linearVelocity.y;
        bool canJump = coyoteTimeCounter > 0f; // Can jump if recently grounded
        bool wantsToJump = jumpBufferCounter > 0f; // Player pressed jump recently
        
        if (wantsToJump && canJump)
        {
            verticalVelocity = jumpForce;
            
            // Consume the jump
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            
            if (debugGroundCheck)
            {
                Debug.Log("JUMP! - Grounded: " + isGrounded + ", Coyote: " + (coyoteTimeCounter > 0));
            }
        }
        
        // Apply the new velocity
        rb2d.linearVelocity = new Vector2(newHorizontalVelocity, verticalVelocity);
    }
    
    private void HandleTransformMovement()
    {
        // For platformers, physics movement is highly recommended
        // This is a basic fallback that doesn't handle gravity properly
        Debug.LogWarning("Transform movement is not recommended for platformers. Enable 'Use Physics Movement' for better results.");
        
        // Only handle horizontal movement
        Vector2 targetVelocity = new Vector2(moveInput.x * moveSpeed, 0);
        
        if (moveInput.x != 0)
        {
            currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, targetVelocity.x, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, 0, deceleration * Time.fixedDeltaTime);
        }
        
        // Move the transform horizontally only
        transform.Translate(new Vector2(currentVelocity.x * Time.fixedDeltaTime, 0));
    }
    
    void OnDestroy()
    {
        // Disable input actions when the object is destroyed
        if (inputActions != null)
        {
            var playerActionMap = inputActions.FindActionMap("Player");
            if (playerActionMap != null)
                playerActionMap.Disable();
        }
        else if (moveAction != null)
        {
            moveAction.actionMap?.Disable();
        }
    }
    
    void OnDisable()
    {
        // Disable input actions when the component is disabled
        if (inputActions != null)
        {
            var playerActionMap = inputActions.FindActionMap("Player");
            if (playerActionMap != null)
                playerActionMap.Disable();
        }
        else if (moveAction != null)
        {
            moveAction.actionMap?.Disable();
        }
    }
    
    void OnEnable()
    {
        // Re-enable input actions when the component is enabled
        if (inputActions != null)
        {
            var playerActionMap = inputActions.FindActionMap("Player");
            if (playerActionMap != null)
                playerActionMap.Enable();
        }
        else if (moveAction != null)
        {
            moveAction.actionMap?.Enable();
        }
    }
    
    // Public methods for external access
    public Vector2 GetMoveInput()
    {
        return moveInput;
    }
    
    public Vector2 GetCurrentVelocity()
    {
        return rb2d != null ? rb2d.linearVelocity : currentVelocity;
    }
    
    public bool IsMoving()
    {
        return Mathf.Abs(moveInput.x) > 0;
    }
    
    public bool IsGrounded()
    {
        return isGrounded;
    }
    
    public bool IsJumping()
    {
        return rb2d != null && rb2d.linearVelocity.y > 0.1f;
    }
    
    public bool IsFalling()
    {
        return rb2d != null && rb2d.linearVelocity.y < -0.1f;
    }
    
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }
    
    public void SetJumpForce(float newJumpForce)
    {
        jumpForce = newJumpForce;
    }
    
    // Draw ground check gizmo in the scene view
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            
            // Draw a line showing the ground check
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, groundCheck.position);
        }
    }
    
    void OnDrawGizmos()
    {
        // Always show ground check when selected in editor
        if (groundCheck != null && debugGroundCheck)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
