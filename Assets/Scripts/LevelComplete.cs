using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelComplete : MonoBehaviour
{
    [Header("Level Complete Settings")]
    [SerializeField] private AudioClip levelCompleteSound; // Audio clip to play on level complete
    [SerializeField] private float delayBeforeNextLevel = 2f; // Delay before loading next level
    
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
            
            // Disable player movement
            MovementController playerMovement = col.GetComponent<MovementController>();
            if (playerMovement != null)
            {
                playerMovement.enabled = false;
            }
            
            // Play level complete sound
            if (levelCompleteSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(levelCompleteSound);
            }
            
            // Start coroutine to handle delayed scene transition
            StartCoroutine(CompleteLevel());
        }
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
        else
        {
            Debug.Log("Last level completed! No more levels to load.");
            // load the menu after the last level
            // SceneManager.LoadScene(0);
        }
    }
}
