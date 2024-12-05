using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public Slider volumeSlider; // Reference to the slider
    public AudioSource audioSource; // Reference to the audio source

    void Start()
    {
        // Set the slider value to the saved volume or default to full volume
        volumeSlider.value = PlayerPrefs.GetFloat("Volume", 1.0f);

        // Apply the saved volume to the audio source
        audioSource.volume = volumeSlider.value;

        // Add a listener to the slider to handle value changes
        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float volume)
    {
        // Update the audio source volume
        audioSource.volume = volume;

        // Save the volume value
        PlayerPrefs.SetFloat("Volume", volume);
    }
}
