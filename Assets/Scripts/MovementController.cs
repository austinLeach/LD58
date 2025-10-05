using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Audio;

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
    [SerializeField] private float dashBufferTime = 0.1f; // Time where dash input is remembered
    [SerializeField] private float frictionDelay = 0.2f; // Time to wait before applying high friction
    [SerializeField] private float jumpGraceTime = 0.3f; // Time after jumping where slide downward force is disabled
    
    [Header("Smooth Movement")]
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float airControl = 0.5f; // How much control you have in the air
    [SerializeField] private float minMoveThreshold = 0.1f; // Minimum velocity to prevent getting stuck
    
    [Header("Slope Physics")]
    [SerializeField] private float slopeFriction = 10f; // High friction when not moving to prevent sliding on steep slopes (reduced for better uphill movement)
    [SerializeField] private float movingFriction = 0f; // Low friction when moving for smooth movement
    
    [Header("Slide Feature")]
    [SerializeField] private float slideSpeedMultiplier = 3f; // Speed multiplier when sliding (increased for faster slides)
    [SerializeField] private float slideFriction = 0f; // Very low friction when sliding
    [SerializeField] private float minSlopeAngle = 15f; // Minimum slope angle to allow sliding (degrees)
    
    [Header("Dash Feature")]
    [SerializeField] private bool enableDash = true; // Enable/disable dash
    [SerializeField] private float dashForce = 15f; // Horizontal force applied during dash
    [SerializeField] private float dashDuration = 0.2f; // How long the dash lasts
    [SerializeField] private float dashCooldown = 1f; // Time between dashes
    [SerializeField] private bool canDashInAir = true; // Allow dashing while airborne
    
    [Header("Speed Modification")]
    [SerializeField] private float minimumSpeed = 1f; // Minimum speed the player can reach
    
    [Header("Audio")]
    [SerializeField] private AudioClip jumpSound; // Sound for regular jump
    [SerializeField] private AudioClip doubleJumpSound; // Sound for double jump
    [SerializeField] private AudioClip dashSound; // Sound for dash
    [SerializeField] private AudioClip slideSound; // Sound for sliding (should be looping)
    [SerializeField] private AudioMixerGroup audioMixerGroup; // Assign your SFX mixer group here
    [SerializeField] private float jumpSoundVolume = 0.8f; // Volume for jump sounds
    
    // Dynamic calculation variables
    private float initialMoveSpeed; // Store the starting speed
    private float calculatedDecreaseAmount; // Calculated based on total coins
    private int totalCoinsInLevel; // Total number of coins at level start
    
    [Header("Input")]
    private InputActionAsset inputActions;
    
    // Components
    private Rigidbody2D rb2d;
    private GroundDetector groundDetector;
    private AudioSource audioSource; // For playing jump sounds
    private AudioSource slideAudioSource; // For playing slide sound (looping)
    
    // Physics materials for slope behavior
    private PhysicsMaterial2D highFrictionMaterial;
    private PhysicsMaterial2D lowFrictionMaterial;
    
    // Input Actions
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction slideAction;
    private InputAction dashAction;
    
    // Input
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    public bool isGrounded;
    private bool wasGrounded;
    private bool jumpPressed;
    private bool slidePressed; // For down input detection
    private bool dashPressed; // For dash input detection
    
    // Slide state
    private bool isSliding = false;
    private bool wasSliding = false; // Track if we were sliding last frame
    private PhysicsMaterial2D slideMaterial;
    
    // Dash state
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private float dashDirection = 0f; // Direction of the dash (1 = right, -1 = left)
    
    // Friction timing for smooth deceleration
    private float noInputTimer = 0f;
    
    // Jump timing
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float dashBufferCounter; // Buffer for dash input
    private int currentJumpCount = 0; // Track number of jumps used (0 = grounded, 1 = first jump, 2 = double jump)
    private float jumpGraceTimer = 0f; // Timer to prevent slide downward force after jumping
    
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
        
        // Initialize speed modification system
        InitializeSpeedSystem();
        
        // Setup Audio
        SetupAudio();
        
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
        UpdateTimers();
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
            
            // Dash action (shift key)
            dashAction = actionMap.AddAction("Dash", InputActionType.Button);
            dashAction.AddBinding("<Keyboard>/leftShift");
            dashAction.AddBinding("<Keyboard>/rightShift");
            
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
            dashAction = playerActionMap.FindAction("Dash");
            
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
    
    private void InitializeSpeedSystem()
    {
        // Store the initial move speed
        initialMoveSpeed = moveSpeed;
        
        // Count total coins in the level at start
        totalCoinsInLevel = GameObject.FindGameObjectsWithTag("Coin").Length;
        
        if (totalCoinsInLevel > 0)
        {
            // Calculate decrease amount: (initialSpeed - minimumSpeed) / totalCoins
            // This ensures that collecting all coins will bring speed exactly to minimum
            calculatedDecreaseAmount = (initialMoveSpeed - minimumSpeed) / totalCoinsInLevel;
        }
        else
        {
            // No coins in level, no speed decrease
            calculatedDecreaseAmount = 0f;
        }
    }
    
    private void SetupAudio()
    {
        // Get or add AudioSource component for one-shot sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure AudioSource for sound effects
        audioSource.playOnAwake = false;
        audioSource.volume = jumpSoundVolume;
        
        // Assign the audio mixer group if one is specified
        if (audioMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = audioMixerGroup;
        }
        
        // Create a separate AudioSource for slide sound (looping)
        GameObject slideAudioGO = new GameObject("SlideAudio");
        slideAudioGO.transform.SetParent(transform);
        slideAudioSource = slideAudioGO.AddComponent<AudioSource>();
        
        // Configure slide AudioSource
        slideAudioSource.clip = slideSound;
        slideAudioSource.loop = true;
        slideAudioSource.playOnAwake = false;
        slideAudioSource.volume = 1; // Same volume as jump sounds
        
        if (audioMixerGroup != null)
        {
            slideAudioSource.outputAudioMixerGroup = audioMixerGroup;
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
        
        // Handle dash input
        if (dashAction != null)
        {
            dashPressed = dashAction.WasPressedThisFrame();
        }
        else
        {
            dashPressed = false;
        }
    }
    
    private void UpdateTimers()
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
        
        // Dash Buffer: Remember dash input for a short time
        if (dashPressed)
        {
            dashBufferCounter = dashBufferTime;
        }
        else
        {
            dashBufferCounter -= Time.deltaTime;
        }
        
        // Dash timers
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                // Restore gravity when dash ends
                rb2d.gravityScale = gravityScale;
            }
        }
        
        // Jump grace timer: Prevent slide downward force after jumping
        if (jumpGraceTimer > 0f)
        {
            jumpGraceTimer -= Time.deltaTime;
        }
        
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
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
        
        // Check for sliding - allow sliding to continue even when trying to jump for proper jump mechanics
        bool canSlide = isGrounded && slidePressed && IsOnSlope();
        
        // Allow sliding to continue when transitioning off edges or on flat ground after sliding
        bool slidingOffEdge = wasSliding && slidePressed && !isGrounded;
        bool slidingOnFlat = wasSliding && slidePressed && isGrounded && !IsOnSlope();
        
        wasSliding = isSliding; // Track previous slide state
        isSliding = canSlide || slidingOffEdge || slidingOnFlat;
        
        // Handle slide audio - only play when sliding and grounded
        bool shouldPlaySlideSound = isSliding && isGrounded;
        if (slideAudioSource != null && slideSound != null)
        {
            if (shouldPlaySlideSound && !slideAudioSource.isPlaying)
            {
                // Start slide sound
                slideAudioSource.Play();
            }
            else if (!shouldPlaySlideSound && slideAudioSource.isPlaying)
            {
                // Stop slide sound
                slideAudioSource.Stop();
            }
        }
        
        // Check for dashing - use buffer system instead of direct input
        bool wantsToDash = dashBufferCounter > 0f; // Player pressed dash recently
        bool hasCollectedTooManyCoinsForDash = GetCoinCollectionRatio() >= (1f / 3f); // Lose dash when >1/3 coins collected
        
        // Check if dash would increase speed
        float currentDashSpeed = Mathf.Abs(rb2d.linearVelocity.x);
        bool wouldIncreaseSpeed = dashForce > currentDashSpeed;
        
        bool canDash = enableDash && wantsToDash && dashCooldownTimer <= 0f && !isDashing && (canDashInAir || isGrounded) && !hasCollectedTooManyCoinsForDash;
        
        if (canDash)
        {
            // Start dash
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            
            // Make immune to gravity during dash
            rb2d.gravityScale = 0f;
            
            // Play dash sound effect
            if (audioSource != null && dashSound != null)
            {
                audioSource.PlayOneShot(dashSound);
            }
            
            // Consume the dash buffer
            dashBufferCounter = 0f;
            
            // Determine dash direction based on input, or face direction if no input
            if (Mathf.Abs(moveInput.x) > 0.1f)
            {
                dashDirection = Mathf.Sign(moveInput.x);
            }
            else
            {
                // If no input, dash in the direction the player was last moving
                dashDirection = rb2d.linearVelocity.x >= 0 ? 1f : -1f;
            }
        }
        
        // Handle horizontal movement
        float targetHorizontalVelocity = moveInput.x * moveSpeed;
        
        // Apply slide speed boost if sliding - use downward force, not horizontal
        if (isSliding)
        {
            // When sliding on a slope, don't apply horizontal velocity - let slope physics handle that
            // When sliding off an edge (airborne) or on flat ground, preserve current momentum
            if (isGrounded && IsOnSlope())
            {
                // On slope - use downward force only
                targetHorizontalVelocity = 0f; // No horizontal input while sliding on slope
            }
            else
            {
                // Sliding off edge or on flat ground - preserve current momentum only if it's significant
                float currentSpeed = Mathf.Abs(rb2d.linearVelocity.x);
                float minPreserveSpeed = moveSpeed * 0.5f; // Only preserve momentum above 50% of move speed
                
                if (currentSpeed >= minPreserveSpeed)
                {
                    // Preserve current momentum but apply deceleration when on flat ground
                    if (isGrounded && !IsOnSlope())
                    {
                        // On flat ground while sliding - apply deceleration to current momentum
                        float currentHorizontalVel = rb2d.linearVelocity.x;
                        float deceleratedVelocity = Mathf.MoveTowards(currentHorizontalVel, 0f, deceleration * 2f * Time.fixedDeltaTime); // 2x deceleration for slides on flat
                        targetHorizontalVelocity = deceleratedVelocity;
                    }
                    else
                    {
                        // Airborne or other cases - preserve momentum without deceleration
                        targetHorizontalVelocity = rb2d.linearVelocity.x;
                    }
                }
                else
                {
                    // Speed too low, treat as normal movement input
                    targetHorizontalVelocity = moveInput.x * moveSpeed;
                }
            }
        }
        
        float currentHorizontalVelocity = rb2d.linearVelocity.x;
        
        // Apply air control factor if not grounded
        float controlFactor = isGrounded ? 1f : airControl;
        
        // Calculate new horizontal velocity
        float newHorizontalVelocity;
        
        // If dashing, override all other movement
        if (isDashing)
        {
            // Only increase horizontal speed if dash would actually make us faster
            if (wouldIncreaseSpeed)
            {
                newHorizontalVelocity = dashDirection * dashForce;
            }
            else
            {
                // Preserve current horizontal speed but still get dash feel (vertical reset handled elsewhere)
                newHorizontalVelocity = currentHorizontalVelocity;
            }
        }
        // If sliding, apply slide velocity directly without normal movement logic
        else if (isSliding)
        {
            // Apply slide velocity directly, bypassing all normal acceleration logic
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
                // Moving in same direction - check if we should preserve momentum
                bool hasHighMomentum = Mathf.Abs(currentHorizontalVelocity) > Mathf.Abs(targetHorizontalVelocity);
                bool shouldPreserveMomentum = hasHighMomentum && !isGrounded; // Only preserve momentum in the air
                
                if (shouldPreserveMomentum)
                {
                    // Don't interfere with momentum while airborne - player is moving faster than target speed
                    // Let natural deceleration handle the slowdown instead of forced movement
                    newHorizontalVelocity = currentHorizontalVelocity;
                }
                else
                {
                    // Normal acceleration when below target speed OR when grounded
                    newHorizontalVelocity = Mathf.MoveTowards(
                        currentHorizontalVelocity, 
                        targetHorizontalVelocity, 
                        effectiveAcceleration * controlFactor * Time.fixedDeltaTime
                    );
                }
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
        bool hasCollectedTooManyCoinsForDoubleJump = GetCoinCollectionRatio() >= (2f / 3f); // Lose double jump when >2/3 coins collected
        bool canDoubleJump = enableDoubleJump && currentJumpCount == 1 && !isGrounded && !hasCollectedTooManyCoinsForDoubleJump; // Can double jump if in air after first jump and haven't collected too many coins
        bool wantsToJump = jumpBufferCounter > 0f; // Player pressed jump recently
        bool jumpExecutedThisFrame = false; // Track if we executed a jump this frame
        
        if (wantsToJump && (canFirstJump || canDoubleJump))
        {
            // Determine which jump this is
            bool isDoubleJump = canDoubleJump && !canFirstJump;
            
            // Apply appropriate jump force
            verticalVelocity = isDoubleJump ? doubleJumpForce : jumpForce;
            jumpExecutedThisFrame = true; // Mark that we executed a jump
            jumpGraceTimer = jumpGraceTime; // Start grace period to prevent slide downward force
            
            // Play jump sound effect
            if (audioSource != null)
            {
                AudioClip soundToPlay = isDoubleJump ? doubleJumpSound : jumpSound;
                if (soundToPlay != null)
                {
                    audioSource.PlayOneShot(soundToPlay);
                }
            }
            
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
        
        // Add downward force when sliding to keep grounded on slopes, but NOT if we just jumped, are in grace period, or not grounded
        if (isSliding && !jumpExecutedThisFrame && jumpGraceTimer <= 0f && isGrounded)
        {
            // Scale slide downward force based on coin collection: 100% at no coins, 85% at many coins, 70% at all coins
            float coinRatio = GetCoinCollectionRatio(); // 0.0 to 1.0
            float slideForcePercentage = Mathf.Lerp(1.0f, 0.7f, coinRatio); // Linear scale from 100% to 70%
            
            // Apply scaled downward force - this gets converted to horizontal speed by slope physics
            float slideDownwardForce = -initialMoveSpeed * slideSpeedMultiplier * slideForcePercentage * 0.7f; // Increased intensity for faster slides
            verticalVelocity = Mathf.Min(verticalVelocity, slideDownwardForce);
        }
        
        // Additional force to stop sliding on steep slopes when no input is given
        if (isGrounded && !isSliding && Mathf.Abs(moveInput.x) < 0.1f && IsOnSlope())
        {
            // Apply strong resistance to prevent sliding down steep slopes
            if (Mathf.Abs(newHorizontalVelocity) > 0.5f) // If still moving on slope without input
            {
                // Aggressively reduce horizontal velocity on steep slopes
                newHorizontalVelocity = Mathf.MoveTowards(newHorizontalVelocity, 0f, 20f * Time.fixedDeltaTime);
            }
            
            // Also apply slight upward force to counteract gravity on steep slopes
            if (verticalVelocity < 0f) // If falling
            {
                verticalVelocity = Mathf.MoveTowards(verticalVelocity, 0f, 10f * Time.fixedDeltaTime);
            }
        }
        
        // Apply the new velocity
        if (isDashing)
        {
            // For perfectly horizontal dash, eliminate vertical velocity
            rb2d.linearVelocity = new Vector2(newHorizontalVelocity, 0f);
        }
        else
        {
            rb2d.linearVelocity = new Vector2(newHorizontalVelocity, verticalVelocity);
        }
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
                // Simple logic: if providing input, use low friction; if not, use high friction after delay
                bool isProvidingInput = Mathf.Abs(moveInput.x) >= 0.1f; // Player is trying to move
                bool hasHighMomentum = Mathf.Abs(rb2d.linearVelocity.x) > initialMoveSpeed * 1.2f; // Preserve momentum from slides/dashes
                bool justStoppedSliding = wasSliding && !isSliding; // Just released slide button
                
                // If just stopped sliding, force high friction to decelerate regardless of momentum
                bool shouldUseHighFriction = (!isProvidingInput && noInputTimer > frictionDelay && !hasHighMomentum) || justStoppedSliding;
                
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
        if (!isGrounded || groundDetector == null) return false;
        
        // Refresh ground contact info to ensure we have the latest data
        groundDetector.RefreshGroundContact();
        
        // Use collision-based detection instead of raycasts
        if (groundDetector.HasValidGroundContact())
        {
            Vector2 groundNormal = groundDetector.GetGroundNormal();
            float angle = Vector2.Angle(groundNormal, Vector2.up);
            bool isSlope = angle >= minSlopeAngle;
            
            Collider2D groundCollider = groundDetector.GetGroundCollider();
            string surfaceName = groundCollider != null ? groundCollider.name : "Unknown";
            
            // Additional validation: make sure the normal is reasonable (pointing generally upward)
            if (groundNormal.y < 0.1f)
            {
                return false;
            }
            
            return isSlope;
        }
        
        return false;
    }
    
    void OnDestroy()
    {
        // Restore gravity if destroyed while dashing
        if (isDashing && rb2d != null)
        {
            rb2d.gravityScale = gravityScale;
        }
        
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
        // Restore gravity if disabled while dashing
        if (isDashing && rb2d != null)
        {
            rb2d.gravityScale = gravityScale;
            isDashing = false; // Also stop the dash state
        }
        
        // Stop slide audio if playing
        if (slideAudioSource != null && slideAudioSource.isPlaying)
        {
            slideAudioSource.Stop();
        }
        
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
        bool hasNotCollectedTooManyCoins = GetCoinCollectionRatio() <= (2f / 3f);
        return enableDoubleJump && currentJumpCount == 1 && !isGrounded && hasNotCollectedTooManyCoins;
    }
    
    public bool HasEnoughSpeedForDoubleJump()
    {
        return GetCoinCollectionRatio() <= (2f / 3f);
    }
    
    public float GetCoinThresholdForDoubleJump()
    {
        return 2f / 3f;
    }
    
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }
    
    public void DecreasePlayerSpeed()
    {
        // Use the calculated decrease amount based on total coins in level
        moveSpeed = Mathf.Max(moveSpeed - calculatedDecreaseAmount, minimumSpeed);
    }
    
    public void DecreasePlayerSpeed(float customAmount)
    {
        // Overload to allow custom decrease amounts
        moveSpeed = Mathf.Max(moveSpeed - customAmount, minimumSpeed);
    }
    
    public float GetCurrentMoveSpeed()
    {
        return moveSpeed;
    }
    
    public float GetSpeedDecreaseAmount()
    {
        return calculatedDecreaseAmount;
    }
    
    public int GetTotalCoinsInLevel()
    {
        return totalCoinsInLevel;
    }
    
    public int GetCoinsCollected()
    {
        int remainingCoins = GameObject.FindGameObjectsWithTag("Coin").Length;
        return totalCoinsInLevel - remainingCoins;
    }
    
    public float GetCoinCollectionRatio()
    {
        if (totalCoinsInLevel == 0) return 0f;
        return (float)GetCoinsCollected() / totalCoinsInLevel;
    }
    
    public void SetJumpForce(float newJumpForce)
    {
        jumpForce = newJumpForce;
    }
    
    public bool IsDashing()
    {
        return isDashing;
    }
    
    public bool CanDash()
    {
        bool groundRequirement = canDashInAir || isGrounded;
        bool hasNotCollectedTooManyCoins = GetCoinCollectionRatio() <= (1f / 3f);
        return enableDash && dashCooldownTimer <= 0f && !isDashing && groundRequirement && hasNotCollectedTooManyCoins;
    }
    
    public bool HasEnoughSpeedForDash()
    {
        return GetCoinCollectionRatio() <= (1f / 3f);
    }
    
    public float GetCoinThresholdForDash()
    {
        return 1f / 3f;
    }
    
    public float GetDashCooldownRemaining()
    {
        return Mathf.Max(0f, dashCooldownTimer);
    }
}