using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Coins : MonoBehaviour
{
    UI uiScript;
    private CapsuleCollider2D Collider;
    private int coinScript = 0;
    public bool isCharging = false;
    public float startTimer = 5f;
    
    [Header("Audio")]
    public AudioClip[] coinPickupSounds; // Array of 5 different coin pickup sounds
    public AudioMixerGroup audioMixerGroup; // Assign your SFX mixer group here
    private AudioSource audioSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        uiScript = GameObject.FindGameObjectWithTag("UI").GetComponent<UI>();
        Collider = GetComponent<CapsuleCollider2D>();
        
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
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
            // Play a random coin pickup sound
            if (coinPickupSounds != null && coinPickupSounds.Length > 0)
            {
                // Pick a random sound from the array
                int randomIndex = UnityEngine.Random.Range(0, coinPickupSounds.Length);
                AudioClip selectedSound = coinPickupSounds[randomIndex];
                
                if (selectedSound != null)
                {
                    // Use the coin's AudioSource (which can be assigned to your mixer) instead of PlayClipAtPoint
                    audioSource.clip = selectedSound;
                    audioSource.Play();
                    
                    // Delay destruction to allow sound to finish
                    StartCoroutine(DestroyAfterSound(selectedSound.length));
                }
            }
            
            // Update game state immediately
            col.GetComponent<MovementController>().DecreasePlayerSpeed();
            coinScript++;
            //Debug.Log("Trigger " + coinScript);
            GlobalVariables.currentCoins++;
            uiScript.UpdateCoinCount();
            
            // Hide the coin visually but don't destroy yet (let sound finish)
            GetComponent<Renderer>().enabled = false;
            GetComponent<Collider2D>().enabled = false;
            
            //Debug.Log("Coin Count: " + GlobalVariables.currentCoins);
            isCharging = true;
            GlobalVariables.Timer(ref isCharging, ref startTimer);
        }
        
    }
    
    private IEnumerator DestroyAfterSound(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

}

