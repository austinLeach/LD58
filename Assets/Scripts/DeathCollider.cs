using UnityEngine;
using UnityEngine.SceneManagement;

public class Death : MonoBehaviour
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
            GlobalVariables.currentCoins = 0;
            
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
}
