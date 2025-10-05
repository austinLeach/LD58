using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalVariables : MonoBehaviour
{
    public static int totalCollected = 0;
    public static int currentCoins = 0;
    public static int levelCoins = 0;

    [Header("Music State Persistence")]
    public static float musicTime = 0f; // Current playback time of music
    public static bool isPlayingLoopMusic = false; // true if loop music is playing, false if intro or no music
    public static bool musicInitialized = false; // tracks if music system has been set up


    private void Start()
    {
        levelCoins = GameObject.FindGameObjectsWithTag("Coin").Length;
    }

    public static bool Timer(ref bool isChanging, ref float timer)
    {
        if (isChanging)
        {
            timer -= Time.deltaTime;
            if (timer < 0)
            {
                isChanging = false;
            }
        }
        return isChanging;
    }

    // Method to save current music state (call this when level ends)
    public static void SaveMusicState(float currentTime, bool isLooping)
    {
        musicTime = currentTime;
        isPlayingLoopMusic = isLooping;
        musicInitialized = true;
    }

}
