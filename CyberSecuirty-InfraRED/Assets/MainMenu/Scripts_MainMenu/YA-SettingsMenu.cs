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

    private bool isInitialized;

    private void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat("volume", 1f);

        // prevent slider event firing during setup
        volumeSlider.onValueChanged.RemoveListener(SetVolume);

        volumeSlider.value = savedVolume;

        SetVolume(savedVolume);

        volumeSlider.onValueChanged.AddListener(SetVolume);

        isInitialized = true;
    }

    public void ToggleSettings()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void SetVolume(float value)
    {
        if (!isInitialized) return;

        float volumeDb = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;

        mainMixer.SetFloat("MasterVolume", volumeDb);

        PlayerPrefs.SetFloat("volume", value);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }
}