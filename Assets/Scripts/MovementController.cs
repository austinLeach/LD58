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
    
    [Header("Double Jump")]
    [SerializeField] private bool enableDoubleJump = true; // Enable/disable double jump
    [SerializeField] private float doubleJumpForce = 10f; // Force for the second jump (can be different from first)
    
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
    
    [Header("Slope Physics")]
    [SerializeField] private float slopeFriction = 5f; // High friction when not moving to prevent sliding
    [SerializeField] private float movingFriction = 0f; // Low friction when moving for smooth movement
    
    [Header("Slide Feature")]
    [SerializeField] private float slideSpeedMultiplier = 2.5f; // Speed multiplier when sliding
    [SerializeField] private float slideFriction = 0f; // Very low friction when sliding
    [SerializeField] private float minSlopeAngle = 15f; // Minimum slope angle to allow sliding (degrees)
    
    [Header("Input")]
    private InputActionAsset inputActions;
    
    // Components
    private Rigidbody2D rb2d;
    private GroundDetector groundDetector;
    
    // Physics materials for slope behavior
    private PhysicsMaterial2D highFrictionMaterial;
    private PhysicsMaterial2D lowFrictionMaterial;
    
    // Input Actions
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction slideAction;
    
    // Input
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    public bool isGrounded;
    private bool wasGrounded;
    private bool jumpPressed;
    private bool slidePressed; // For down input detection
    
    // Slide state
    private bool isSliding = false;
    private PhysicsMaterial2D slideMaterial;
    
    // Friction timing for smooth deceleration
    private float noInputTimer = 0f;
    [SerializeField] private float frictionDelay = 0.2f; // Time to wait before applying high friction
    
    // Jump timing
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private int currentJumpCount = 0; // Track number of jumps used (0 = grounded, 1 = first jump, 2 = double jump)
    
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
            
            // Slide action (down key)
            slideAction = actionMap.AddAction("Slide", InputActionType.Button);
            slideAction.AddBinding("<Keyboard>/s");
            slideAction.AddBinding("<Keyboard>/downArrow");
            
            actionMap.Enable();
            return;
        }
        
        // Get the actions from the Player action map
        var playerActionMap = inputActions.FindActionMap("Player");
        if (playerActionMap != null)
        {
            moveAction = playerActionMap.FindAction("Move");
            jumpAction = playerActionMap.FindAction("Jump");
            slideAction = playerActionMap.FindAction("Slide");
            
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
        
        // Handle slide input
        if (slideAction != null)
        {
            slidePressed = slideAction.IsPressed();
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
        // Update no-input timer for friction control
        if (moveInput.x != 0)
        {
            noInputTimer = 0f; // Reset timer when there's input
        }
        else
        {
            noInputTimer += Time.fixedDeltaTime; // Increment when no input
        }
        
        // Check for sliding - but disable sliding if trying to jump
        bool canSlide = isGrounded && slidePressed && IsOnSlope() && !jumpPressed;
        isSliding = canSlide;
        
        // Handle horizontal movement
        float targetHorizontalVelocity = moveInput.x * moveSpeed;
        
        // Apply slide speed boost if sliding
        if (isSliding)
        {
            // Don't modify horizontal velocity when sliding - let slope physics handle it naturally
            // The downward force will be applied later in the vertical velocity section
        }
        
        float currentHorizontalVelocity = rb2d.linearVelocity.x;
        
        // Apply air control factor if not grounded
        float controlFactor = isGrounded ? 1f : airControl;
        
        // Calculate new horizontal velocity
        float newHorizontalVelocity;
        
        // If sliding, apply slide velocity directly without normal movement logic
        if (isSliding)
        {
            newHorizontalVelocity = targetHorizontalVelocity;
        }
        else if (moveInput.x != 0)
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
        bool canFirstJump = coyoteTimeCounter > 0f && currentJumpCount == 0; // Can first jump if recently grounded and haven't jumped
        bool canDoubleJump = enableDoubleJump && currentJumpCount == 1 && !isGrounded; // Can double jump if in air after first jump
        bool wantsToJump = jumpBufferCounter > 0f; // Player pressed jump recently
        
        if (wantsToJump && (canFirstJump || canDoubleJump))
        {
            // Determine which jump this is
            bool isDoubleJump = canDoubleJump && !canFirstJump;
            
            // Apply appropriate jump force
            verticalVelocity = isDoubleJump ? doubleJumpForce : jumpForce;
            
            // Increment jump count
            currentJumpCount++;
            
            // If jumping while sliding (only applies to first jump), preserve and boost horizontal momentum
            if (isSliding && !isDoubleJump)
            {
                // Use current horizontal velocity from sliding and boost it
                float currentHorizontalSpeed = rb2d.linearVelocity.x;
                
                // If we have significant horizontal speed from sliding, preserve and boost it
                if (Mathf.Abs(currentHorizontalSpeed) > moveSpeed * 0.5f)
                {
                    newHorizontalVelocity = currentHorizontalSpeed * 1.3f; // 30% boost for slide jumps
                }
            }
            
            // Consume the jump
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }
        
        // Switch physics material based on movement intent and ground state
        UpdatePhysicsMaterial();
        
        // Add downward force when sliding to keep grounded on slopes
        if (isSliding && !(wantsToJump && (canFirstJump || canDoubleJump))) // Don't apply downward force if trying to jump
        {
            // Apply strong downward force based on slide speed multiplier
            // This forces the player down the slope naturally
            float slideDownwardForce = -moveSpeed * slideSpeedMultiplier * 0.5f; // Adjust 0.5f for intensity
            verticalVelocity = Mathf.Min(verticalVelocity, slideDownwardForce);
        }
        
        // Apply the new velocity
        rb2d.linearVelocity = new Vector2(newHorizontalVelocity, verticalVelocity);
    }
    
    private void UpdatePhysicsMaterial()
    {
        if (rb2d.sharedMaterial == null || highFrictionMaterial == null || lowFrictionMaterial == null || slideMaterial == null)
            return;
            
        PhysicsMaterial2D targetMaterial;
        
        if (isSliding)
        {
            // Use slide material when sliding
            targetMaterial = slideMaterial;
        }
        else
        {
            // Always use low friction when in the air to preserve momentum
            if (!isGrounded)
            {
                targetMaterial = lowFrictionMaterial;
            }
            else
            {
                // Use high friction only when grounded, after deceleration has had time to work
                // This allows smooth deceleration before stopping on slopes
                bool hasSignificantMomentum = Mathf.Abs(rb2d.linearVelocity.x) > moveSpeed * 1.2f; // If moving faster than normal speed
                bool shouldUseHighFriction = Mathf.Abs(moveInput.x) < 0.1f && 
                                           noInputTimer > frictionDelay &&
                                           !hasSignificantMomentum; // Don't use high friction if player has momentum from sliding
                
                targetMaterial = shouldUseHighFriction ? highFrictionMaterial : lowFrictionMaterial;
            }
        }
        
        if (rb2d.sharedMaterial != targetMaterial)
        {
            rb2d.sharedMaterial = targetMaterial;
        }
    }
    
    private PhysicsMaterial2D CreatePhysicsMaterial()
    {
        // Create low friction material for when moving
        lowFrictionMaterial = new PhysicsMaterial2D("PlayerMovingMaterial");
        lowFrictionMaterial.friction = movingFriction;
        lowFrictionMaterial.bounciness = 0f;
        
        // Create high friction material for when not moving (prevents sliding on slopes)
        highFrictionMaterial = new PhysicsMaterial2D("PlayerStoppedMaterial");
        highFrictionMaterial.friction = slopeFriction;
        highFrictionMaterial.bounciness = 0f;
        
        // Create slide material for enhanced slope sliding
        slideMaterial = new PhysicsMaterial2D("PlayerSlideMaterial");
        slideMaterial.friction = slideFriction;
        slideMaterial.bounciness = 0f;
        
        // Start with low friction material
        return lowFrictionMaterial;
    }
    
    private bool IsOnSlope()
    {
        if (!isGrounded) return false;
        
        // Get player's collider bounds to cast from outside the collider
        Collider2D playerCollider = GetComponent<Collider2D>();
        Vector2 rayStart = transform.position;
        float rayDistance = 2f;
        
        if (playerCollider != null)
        {
            // Start the ray from slightly below the bottom of the collider
            float colliderBottom = playerCollider.bounds.min.y;
            rayStart = new Vector2(transform.position.x, colliderBottom - 0.1f);
            rayDistance = 1f;
        }
        
        // Cast in multiple directions to handle steep slopes
        // Try straight down first, then angled rays for steep slopes
        Vector2[] rayDirections = {
            Vector2.down,              // Straight down
            new Vector2(-0.5f, -1f).normalized,  // Down-left for right-facing slopes
            new Vector2(0.5f, -1f).normalized    // Down-right for left-facing slopes
        };
        
        RaycastHit2D hit = new RaycastHit2D();
        bool foundGround = false;
        
        // Try each direction until we find ground
        foreach (Vector2 direction in rayDirections)
        {
            hit = Physics2D.Raycast(rayStart, direction, rayDistance, groundLayerMask);
            
            if (hit.collider != null)
            {
                foundGround = true;
                break;
            }
        }
        
        if (foundGround)
        {
            // Calculate the angle of the surface
            float angle = Vector2.Angle(hit.normal, Vector2.up);
            return angle >= minSlopeAngle;
        }
        
        return false;
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
        
        // Reset jump count when landing
        if (grounded && !wasGrounded)
        {
            currentJumpCount = 0;
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
    
    public int GetJumpCount()
    {
        return currentJumpCount;
    }
    
    public bool CanDoubleJump()
    {
        return enableDoubleJump && currentJumpCount == 1 && !isGrounded;
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