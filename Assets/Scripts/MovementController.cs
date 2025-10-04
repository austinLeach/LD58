using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float gravityScale = 3f;
    [SerializeField] private LayerMask groundLayerMask = 1; // Set to Ground layer only
    
    [Header("Ground Detection")]
    [SerializeField] private Transform manualGroundCheck; // Drag the GroundDetector here for manual positioning
    
    [Header("Jump Feel")]
    [SerializeField] private float coyoteTime = 0.1f; // Time after leaving ground where you can still jump
    [SerializeField] private float jumpBufferTime = 0.1f; // Time before landing where jump input is remembered
    
    [Header("Smooth Movement")]
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float airControl = 0.5f; // How much control you have in the air
    [SerializeField] private float minMoveThreshold = 0.1f; // Minimum velocity to prevent getting stuck
    
    [Header("Input")]
    private InputActionAsset inputActions;
    
    // Components
    private Rigidbody2D rb2d;
    private GroundDetector groundDetector;
    
    // Input Actions
    private InputAction moveAction;
    private InputAction jumpAction;
    
    // Input
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    public bool isGrounded;
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
        if (rb2d == null) {
            rb2d = gameObject.AddComponent<Rigidbody2D>();
        }

        // Set up gravity for platformer movement
        if (rb2d != null)
        {
            rb2d.gravityScale = gravityScale;
            rb2d.freezeRotation = true; // Prevent rotation
            
            // Configure physics settings to prevent sticking
            rb2d.sharedMaterial = CreatePhysicsMaterial();
        }
        
        // Set up collision-based ground detection
        SetupColliderGroundDetection();
        
        // Setup Input Actions
        SetupInputActions();
    }
    
    private void SetupColliderGroundDetection()
    {
        // Require manual ground check - no auto creation
        if (manualGroundCheck == null)
        {
            Debug.LogError("Manual Ground Check is required! Please assign a GameObject with ground detection in the inspector.");
            return;
        }

        GameObject groundDetectorObj = manualGroundCheck.gameObject;
        
        // Make sure it has the required components
        BoxCollider2D trigger = groundDetectorObj.GetComponent<BoxCollider2D>();
        if (trigger == null)
        {
            Debug.LogError("Manual Ground Check GameObject must have a BoxCollider2D component set as a trigger!");
            return;
        }
        
        // Ensure it's set as a trigger
        if (!trigger.isTrigger)
        {
            Debug.LogWarning("Setting Manual Ground Check BoxCollider2D to trigger mode.");
            trigger.isTrigger = true;
        }
        
        // Add GroundDetector script if missing
        groundDetector = groundDetectorObj.GetComponent<GroundDetector>();
        if (groundDetector == null)
        {
            groundDetector = groundDetectorObj.AddComponent<GroundDetector>();
        }
        
        // Initialize the ground detector
        groundDetector.Initialize(this, groundLayerMask);
    }
    
    void Update()
    {
        HandleInput();
        UpdateJumpTimers();
    }
    
    void FixedUpdate()
    {
        HandlePhysicsMovement();
    }
    
    private void SetupInputActions()
    {
        // If no input actions asset is assigned, create input actions directly
        if (inputActions == null)
        {
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
        
        // Calculate new horizontal velocity
        float newHorizontalVelocity;
        
        if (moveInput.x != 0)
        {
            // Player is trying to move
            float effectiveAcceleration = acceleration;
            
            // If acceleration is very low, use a minimum threshold to prevent getting stuck
            if (acceleration < 5f)
            {
                effectiveAcceleration = Mathf.Max(acceleration, 5f);
            }
            
            if (Mathf.Sign(currentHorizontalVelocity) == Mathf.Sign(targetHorizontalVelocity))
            {
                // Moving in same direction - accelerate
                newHorizontalVelocity = Mathf.MoveTowards(
                    currentHorizontalVelocity, 
                    targetHorizontalVelocity, 
                    effectiveAcceleration * controlFactor * Time.fixedDeltaTime
                );
            }
            else
            {
                // Changing direction - use faster rate for turning
                newHorizontalVelocity = Mathf.MoveTowards(
                    currentHorizontalVelocity, 
                    targetHorizontalVelocity, 
                    (effectiveAcceleration + deceleration) * controlFactor * Time.fixedDeltaTime
                );
            }
            
            // Ensure we meet minimum movement threshold to prevent sticking
            if (Mathf.Abs(newHorizontalVelocity) < minMoveThreshold && Mathf.Abs(targetHorizontalVelocity) > minMoveThreshold)
            {
                newHorizontalVelocity = Mathf.Sign(targetHorizontalVelocity) * minMoveThreshold;
            }
        }
        else
        {
            // Player not trying to move - decelerate to zero
            newHorizontalVelocity = Mathf.MoveTowards(
                currentHorizontalVelocity, 
                0f, 
                deceleration * controlFactor * Time.fixedDeltaTime
            );
            
            // Stop completely when velocity is very small
            if (Mathf.Abs(newHorizontalVelocity) < minMoveThreshold)
            {
                newHorizontalVelocity = 0f;
            }
        }
        
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
        }
        
        // Apply the new velocity
        rb2d.linearVelocity = new Vector2(newHorizontalVelocity, verticalVelocity);
    }
    
    private PhysicsMaterial2D CreatePhysicsMaterial()
    {
        // Create a physics material to prevent sticking and sliding
        PhysicsMaterial2D material = new PhysicsMaterial2D("PlayerMaterial");
        material.friction = 0f; // No friction to prevent sticking
        material.bounciness = 0f; // No bouncing
        return material;
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
    
    // Method for GroundDetector to set grounded state
    public void SetGrounded(bool grounded)
    {
        wasGrounded = isGrounded;
        isGrounded = grounded;
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
}