using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using System.Collections;

public class Slime : MonoBehaviour
{
    [Header("Death Settings")]
    [SerializeField] private AudioClip[] deathSounds; // Array of 3 death sounds
    [SerializeField] private float delayBeforeReload = 2f; // Delay before reloading scene
    [SerializeField] private AudioMixerGroup audioMixerGroup; // Assign your SFX mixer group here
    [SerializeField] private float rotationSpeed = 720f; // Degrees per second for violent rotation
    
    private float speed;
    private bool isCharging = false;
    private AudioSource audioSource;
    private Transform playerTransform; // Store reference to player's transform for rotation

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
    }

    // Update is called once per frame
    void Update()
    {
        // Maintain violent rotation if player is dying
        if (isCharging && playerTransform != null)
        {
            // Rotate the player transform directly
            playerTransform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && (!isCharging))
        {
            isCharging = true;
            
            // Disable player movement and stop all momentum
            MovementController playerMovement = collision.gameObject.GetComponent<MovementController>();
            if (playerMovement != null)
            {
                playerMovement.enabled = false;
            }
            
            // Stop player momentum immediately
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
                playerRb.angularVelocity = 0f;
                
                // Disable rotation freezing to allow spinning
                playerRb.freezeRotation = false;
                
                // Store reference for rotation
                playerTransform = collision.transform;
                
                // Create and apply high friction material to prevent sliding
                PhysicsMaterial2D stopMaterial = new PhysicsMaterial2D("DeathStop");
                stopMaterial.friction = 10f; // Very high friction
                stopMaterial.bounciness = 0f; // No bouncing
                playerRb.sharedMaterial = stopMaterial;
            }
            
            // Play random death sound
            if (deathSounds != null && deathSounds.Length > 0 && audioSource != null)
            {
                int randomIndex = UnityEngine.Random.Range(0, deathSounds.Length);
                if (deathSounds[randomIndex] != null)
                {
                    audioSource.PlayOneShot(deathSounds[randomIndex]);
                }
            }
            
            // Reset coins
            GlobalVariables.currentCoins = 0;
            
            // Start coroutine to handle delayed scene reload
            StartCoroutine(ReloadScene());
        }
    }
    
    private IEnumerator ReloadScene()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delayBeforeReload);
        
        // Save current music state before reloading scene
        AudioLoop audioLoop = FindObjectOfType<AudioLoop>();
        if (audioLoop != null)
        {
            audioLoop.SaveCurrentMusicState();
        }
        
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
