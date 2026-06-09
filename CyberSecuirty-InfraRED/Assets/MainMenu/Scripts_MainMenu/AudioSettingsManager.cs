using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioSettingsManager : MonoBehaviour
{
    public static AudioSettingsManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("Exposed Params")]
    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string musicParam  = "MusicVol";
    [SerializeField] private string sfxParam    = "SfxVol";

    private const string KMaster = "vol_master";
    private const string KMusic  = "vol_music";
    private const string KSfx    = "vol_sfx";

    // slider values are linear 0..1
    public float Master01 { get; private set; } = 1f;
    public float Music01  { get; private set; } = 1f;
    public float Sfx01    { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
        ApplyAll();
    }

    public void SetMaster01(float v) { Master01 = Mathf.Clamp01(v); Apply(masterParam, Master01); Save(); }
    public void SetMusic01(float v)  { Music01  = Mathf.Clamp01(v); Apply(musicParam,  Music01);  Save(); }
    public void SetSfx01(float v)    { Sfx01    = Mathf.Clamp01(v); Apply(sfxParam,    Sfx01);    Save(); }

    private void ApplyAll()
    {
        Apply(masterParam, Master01);
        Apply(musicParam,  Music01);
        Apply(sfxParam,    Sfx01);
    }

    private void Apply(string param, float linear01)
    {
        if (mainMixer == null || string.IsNullOrWhiteSpace(param)) return;

        // Convert 0..1 linear to decibels. 0 becomes -80 dB (silence).
        float db = Linear01ToDb(linear01);
        mainMixer.SetFloat(param, db);
    }

    private static float Linear01ToDb(float v)
    {
        v = Mathf.Clamp(v, 0.0001f, 1f);
        return Mathf.Log10(v) * 20f; // 1 -> 0 dB, 0.5 -> -6 dB, etc.
    }

    private void Save()
    {
        PlayerPrefs.SetFloat(KMaster, Master01);
        PlayerPrefs.SetFloat(KMusic,  Music01);
        PlayerPrefs.SetFloat(KSfx,    Sfx01);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        Master01 = PlayerPrefs.GetFloat(KMaster, 1f);
        Music01  = PlayerPrefs.GetFloat(KMusic,  1f);
        Sfx01    = PlayerPrefs.GetFloat(KSfx,    1f);
    }
}