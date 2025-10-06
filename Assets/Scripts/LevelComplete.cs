using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using System.Collections;

public class LevelComplete : MonoBehaviour
{
    [Header("Level Complete Settings")]
    [SerializeField] private AudioClip levelCompleteSound; // Audio clip to play on level complete
    [SerializeField] private float delayBeforeNextLevel = 2f; // Delay before loading next level
    [SerializeField] private AudioMixerGroup audioMixerGroup; // Assign your SFX mixer group here
    
    [Header("Sprite Animation")]
    [SerializeField] private Sprite[] animationSprites; // Array of sprites to animate through
    [SerializeField] private float animationDuration = 2f; // Duration of the sprite animation
    
    private bool isCharging = false;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalPosition; // Store original position for bottom alignment
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Set up AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure AudioSource
        audioSource.playOnAwake = false;
        audioSource.volume = 0.8f;
        
        // Assign the audio mixer group if one is specified
        if (audioMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = audioMixerGroup;
        }
        
        // Get SpriteRenderer component
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning("LevelComplete: No SpriteRenderer found on this GameObject!");
        }
        
        // Store original position for bottom alignment
        originalPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    void OnTriggerEnter2D(Collider2D col)
    {
        
        if (col.gameObject.CompareTag("Player") && (!isCharging))
        {
            isCharging = true;
            
            // Disable player movement and stop all momentum
            MovementController playerMovement = col.GetComponent<MovementController>();
            if (playerMovement != null)
            {
                playerMovement.enabled = false;
            }
            
            // Stop player momentum immediately
            Rigidbody2D playerRb = col.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
                playerRb.angularVelocity = 0f;
                
                // Remove gravity to prevent falling when collider is disabled
                playerRb.gravityScale = 0f;
                
                // Create and apply high friction material to prevent sliding
                PhysicsMaterial2D stopMaterial = new PhysicsMaterial2D("LevelCompleteStop");
                stopMaterial.friction = 10f; // Very high friction
                stopMaterial.bounciness = 0f; // No bouncing
                playerRb.sharedMaterial = stopMaterial;
            }
            
            // Disable player's collider to prevent deaths after winning
            Collider2D playerCollider = col.GetComponent<Collider2D>();
            if (playerCollider != null)
            {
                playerCollider.enabled = false;
            }
            
            // Play level complete sound
            if (levelCompleteSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(levelCompleteSound);
            }
            
            // Start sprite animation
            if (animationSprites != null && animationSprites.Length > 0 && spriteRenderer != null)
            {
                StartCoroutine(AnimateSprites());
            }
            
            // Start coroutine to handle delayed scene transition
            StartCoroutine(CompleteLevel());
        }
    }
    
    private IEnumerator AnimateSprites()
    {
        if (animationSprites == null || animationSprites.Length == 0 || spriteRenderer == null)
            yield break;
            
        float timePerSprite = animationDuration / animationSprites.Length;
        
        // Get the original sprite's bottom position as reference
        Sprite originalSprite = spriteRenderer.sprite;
        float originalBottom = originalPosition.y - (originalSprite != null ? originalSprite.bounds.size.y * 0.5f : 0f);
        
        for (int i = 0; i < animationSprites.Length; i++)
        {
            if (animationSprites[i] != null)
            {
                spriteRenderer.sprite = animationSprites[i];
                
                // Calculate new position to align bottom edges
                float newSpriteHeight = animationSprites[i].bounds.size.y;
                float newBottom = originalBottom;
                float newY = newBottom + (newSpriteHeight * 0.5f);
                
                // Apply the position adjustment
                transform.position = new Vector3(originalPosition.x, newY, originalPosition.z);
            }
            
            // Wait for the time allocated for this sprite
            yield return new WaitForSeconds(timePerSprite);
        }
        
        // Reset to original position after animation
        transform.position = originalPosition;
    }
    
    private IEnumerator CompleteLevel()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delayBeforeNextLevel);
        
        // Save current music state before changing scenes
        AudioLoop audioLoop = FindObjectOfType<AudioLoop>();
        if (audioLoop != null)
        {
            audioLoop.SaveCurrentMusicState();
        }
        
        // Load the next scene
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        GlobalVariables.totalCollected += GlobalVariables.currentCoins;
        GlobalVariables.currentCoins = 0;

        // Check if next scene exists in build settings
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
    }
}
