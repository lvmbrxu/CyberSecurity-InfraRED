using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider volumeSlider;

    [Header("Audio")]
    [SerializeField] private AudioMixer mainMixer;

    private void Start()
    {
        // Load saved volume
        float savedVolume = PlayerPrefs.GetFloat("volume", 0.75f);
        volumeSlider.value = savedVolume;
        SetVolume(savedVolume);
    }

    public void ToggleSettings()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void SetVolume(float value)
    {
        // convert slider (0 to 1) into decibels
        float volumeDb = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;

        mainMixer.SetFloat("MasterVolume", volumeDb);

        PlayerPrefs.SetFloat("volume", value);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }
}