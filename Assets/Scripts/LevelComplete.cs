using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelComplete : MonoBehaviour
{
    private bool isCharging = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

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
}
