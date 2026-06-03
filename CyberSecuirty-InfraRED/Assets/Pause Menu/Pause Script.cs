using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Audio;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject soundPanel;

    [Header("Audio Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider dialogueSlider;

    [Header("Audio")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("Scene Names")]
    [SerializeField] private string exitSceneName;

    private bool isPaused = false;

    private InputAction pauseAction;

    private void Awake()
    {
        pauseAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/escape");
        pauseAction.performed += _ => TogglePause();
    }

    private void OnEnable()
    {
        pauseAction.Enable();
    }

    private void OnDisable()
    {
        pauseAction.Disable();
    }

    private void Start()
    {
        pausePanel.SetActive(false);
        soundPanel.SetActive(false);

        Time.timeScale = 1f;

        LoadAudioSettings();
        HookSliders();
    }

    private void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        pausePanel.SetActive(true);
        soundPanel.SetActive(false);

        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ResumeGame()
    {
        pausePanel.SetActive(false);
        soundPanel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }

    public void ExitScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(exitSceneName);
    }

    // =========================
    // SOUND PANEL
    // =========================

    public void OpenSoundSettings()
    {
        pausePanel.SetActive(false);
        soundPanel.SetActive(true);
    }

    public void BackToPauseMenu()
    {
        soundPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    // =========================
    // AUDIO
    // =========================

    private void LoadAudioSettings()
    {
        float master = PlayerPrefs.GetFloat("masterVolume", 1f);
        float music = PlayerPrefs.GetFloat("musicVolume", 1f);
        float sfx = PlayerPrefs.GetFloat("sfxVolume", 1f);
        float dialogue = PlayerPrefs.GetFloat("dialogueVolume", 1f);

        masterSlider.value = master;
        musicSlider.value = music;
        sfxSlider.value = sfx;
        dialogueSlider.value = dialogue;

        ApplyVolume("MasterVolume", master);
        ApplyVolume("MusicVolume", music);
        ApplyVolume("SFXVolume", sfx);
        ApplyVolume("DialogueVolume", dialogue);
    }

    private void HookSliders()
    {
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        dialogueSlider.onValueChanged.AddListener(SetDialogueVolume);
    }

    public void SetMasterVolume(float value)
    {
        ApplyVolume("MasterVolume", value);
        PlayerPrefs.SetFloat("masterVolume", value);
    }

    public void SetMusicVolume(float value)
    {
        ApplyVolume("MusicVolume", value);
        PlayerPrefs.SetFloat("musicVolume", value);
    }

    public void SetSFXVolume(float value)
    {
        ApplyVolume("SFXVolume", value);
        PlayerPrefs.SetFloat("sfxVolume", value);
    }

    public void SetDialogueVolume(float value)
    {
        ApplyVolume("DialogueVolume", value);
        PlayerPrefs.SetFloat("dialogueVolume", value);
    }

    private void ApplyVolume(string param, float value)
    {
        float volumeDb = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
        mainMixer.SetFloat(param, volumeDb);
    }
}