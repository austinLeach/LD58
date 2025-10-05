using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using System.Collections;

public class Death : MonoBehaviour
{
    [Header("Death Settings")]
    [SerializeField] private AudioClip[] deathSounds; // Array of 3 death sounds
    [SerializeField] private float delayBeforeReload = 2f; // Delay before reloading scene
    [SerializeField] private AudioMixerGroup audioMixerGroup; // Assign your SFX mixer group here
    
    private bool isCharging = false;
    private AudioSource audioSource;
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
