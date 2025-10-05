using UnityEngine;
using UnityEngine.Audio;

public class AudioLoop : MonoBehaviour
{
    [Header("Music Clips")]
    public AudioClip introMusic; // Plays once at the start
    public AudioClip loopMusic; // Loops after intro finishes
    
    [Header("Audio Settings")]
    public AudioMixerGroup musicMixerGroup; // Assign your music mixer group
    [Range(0f, 1f)]
    public float musicVolume = 0.7f;
    
    private AudioSource audioSource;
    private bool introFinished = false;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupAudioSource();
        
        // Check if we should resume music from a saved state
        if (GlobalVariables.musicInitialized)
        {
            ResumeMusicFromSavedState();
        }
        else
        {
            StartMusicSequence();
        }
    }
    
    private void SetupAudioSource()
    {
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure AudioSource for music
        audioSource.playOnAwake = false;
        audioSource.volume = musicVolume;
        
        // Assign mixer group if specified
        if (musicMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = musicMixerGroup;
        }
    }
    
    private void StartMusicSequence()
    {
        if (introMusic != null)
        {
            // Play intro music first
            audioSource.clip = introMusic;
            audioSource.loop = false; // Don't loop the intro
            audioSource.Play();
        }
        else if (loopMusic != null)
        {
            // If no intro, go straight to loop music
            PlayLoopMusic();
        }
    }
    
    private void PlayLoopMusic()
    {
        if (loopMusic != null)
        {
            audioSource.clip = loopMusic;
            audioSource.loop = true; // Loop the main music
            audioSource.Play();
            introFinished = true;
        }
    }
    
    private void ResumeMusicFromSavedState()
    {
        if (GlobalVariables.isPlayingLoopMusic && loopMusic != null)
        {
            // Resume loop music from saved time
            audioSource.clip = loopMusic;
            audioSource.loop = true;
            audioSource.time = GlobalVariables.musicTime;
            audioSource.Play();
            introFinished = true;
        }
        else if (!GlobalVariables.isPlayingLoopMusic && introMusic != null)
        {
            // Resume intro music from saved time
            audioSource.clip = introMusic;
            audioSource.loop = false;
            audioSource.time = GlobalVariables.musicTime;
            audioSource.Play();
            introFinished = false;
        }
        else
        {
            // Fallback to normal music sequence
            StartMusicSequence();
        }
    }
    
    // Public method to save current music state (call this before changing scenes)
    public void SaveCurrentMusicState()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            GlobalVariables.SaveMusicState(audioSource.time, introFinished);
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        // Check if intro has finished and we need to start the loop
        if (!introFinished && introMusic != null && !audioSource.isPlaying)
        {
            PlayLoopMusic();
        }
    }
}
