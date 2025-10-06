using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using System.Collections;
using UnityEditor;
using UnityEngine.UI;
using Unity.Mathematics;

public class Slime : MonoBehaviour
{
    [Header("Death Settings")]
    [SerializeField] private AudioClip[] deathSounds; // Array of 3 death sounds
    [SerializeField] private float delayBeforeReload = 2f; // Delay before reloading scene
    [SerializeField] private AudioMixerGroup audioMixerGroup; // Assign your SFX mixer group here
    [SerializeField] private float rotationSpeed = 720f; // Degrees per second for violent rotation
    [SerializeField] private string playerObjectToEnableName; // Name of GameObject on player to enable

    
    public GameObject leftWall;
    public GameObject leftGround;
    public GameObject rightWall;
    public GameObject rightGround;
    private Rigidbody2D rb2d;

    private float curVel;
    private bool test;
    public float Speed;

    public float moveTimer;
    public float stopTimer;
    public float fullStopTimer;

    private float moveSave;
    private float stopSave;
    private float fullSave;

    private float curTime;


    private bool stop = true;
    private bool fullStop = true;
    private bool moveCharge = true;
    private int leftChecks = 0;
    private int rightChecks = 0;
    private bool isCharging = false;
    private AudioSource audioSource;
    private Transform playerTransform; // Store reference to player's transform for rotation


    void Start()
    {
        moveSave = moveTimer;


        //Setup rigidbody physics
        PhysicsMaterial2D stopMaterial = new PhysicsMaterial2D("SlimeStop");
        stopMaterial.friction = 2f;
        PhysicsMaterial2D fullStopMaterial = new PhysicsMaterial2D("SlimeFullStop");
        fullStopMaterial.friction = 10f;
        rb2d = GetComponent<Rigidbody2D>();
        rb2d.sharedMaterial = stopMaterial;

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
        
        if (moveTimer <= moveSave / 2 & stop == false)
        {
            Debug.Log("HALF " + moveTimer + "Save " + moveSave);
            curVel = rb2d.linearVelocityX;
            rb2d.AddForceX(-1 * curVel);
            stop = true;

        }

        if (moveTimer <= moveSave / 4 & fullStop == false)
        {
            Debug.Log("Quart " + moveTimer + "Save " + moveSave);
            curVel = rb2d.linearVelocityX;
            rb2d.linearVelocityX = rb2d.linearVelocityX * 0;
            fullStop = true;

        }

        Debug.Log(moveCharge);
        //Check if can move
        if (moveCharge == false)
        {
            movement();

        }
        else
        {

            GlobalVariables.Timer(ref moveCharge, ref moveTimer);

        }
        //Debug.Log("Stop: " + stopTimer + "   FullStop: " + fullStopTimer + "   Move: " + moveTimer + "   Left: " +  leftChecks + "   Right: " +  rightChecks);






        //Check if can slow down
        //if (stopIsCharging == false && Mathf.Abs(rb2d.linearVelocityX) > 0)
        //{
        //    //Debug.Log("STOP");
        //    curVel = rb2d.linearVelocityX;
        //    rb2d.AddForceX(-1 * curVel);
        //    stopIsCharging = true;
        //    stopTimer = stopSave;

        //    if (fullStopIsCharging == false)
        //    {
        //        //Debug.Log("FULL STOP");
        //        rb2d.linearVelocityX = rb2d.linearVelocityX * 0;
        //        fullStopIsCharging = true;
        //        fullStopTimer = fullSave;
        //    }

        //}




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
            
            // Enable the specified GameObject on the player
            if (!string.IsNullOrEmpty(playerObjectToEnableName))
            {
                Transform childObject = collision.transform.Find(playerObjectToEnableName);
                if (childObject != null)
                {
                    childObject.gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogWarning($"Could not find child object '{playerObjectToEnableName}' on player {collision.gameObject.name}");
                }
            }
            
            // Reset coins
            GlobalVariables.currentCoins = 0;
            
            // Start coroutine to handle delayed scene reload
            StartCoroutine(ReloadScene());
        }
    }

    private void movement()
    {
        //Add checks
        if(leftWall.GetComponent<SlimeChildCollide>().hit == false)
        {
            leftChecks++;
        }
        if (leftGround.GetComponent<SlimeChildCollide>().hit == true)
        {
            leftChecks += 2;
        }
        if (rightWall.GetComponent<SlimeChildCollide>().hit == false)
        {
            rightChecks++;
        }
        if (rightGround.GetComponent<SlimeChildCollide>().hit == true)
        {
            rightChecks += 2;
        }

        if (leftChecks >= rightChecks)
        {
            //go left
            rb2d.AddForceX(-1 * Speed);
            stop = false;
            fullStop = false;
            moveCharge = true;
            moveTimer = moveSave;
            Debug.Log("Left");
        }
        else
        {
            //go right
            rb2d.AddForceX(Speed);
            stop = false;
            fullStop = false;
            moveCharge = true;
            moveTimer = moveSave;
            Debug.Log("Right");
        }
        
        leftChecks = 0;
        rightChecks = 0;

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
