using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
public class SoundManager : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Slider slider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ChangeVolume(slider.value);
    }

    public void ChangeVolume(float volume)
    {
        audioMixer.SetFloat("volume", volume);
    }
}
