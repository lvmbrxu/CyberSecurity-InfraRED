using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class CutsceneSystem : MonoBehaviour
{
    public static CutsceneSystem Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private CanvasGroup videoGroup;
    [SerializeField] private RawImage videoImage;
    [SerializeField] private Image fadeImage;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource videoAudioSource; // optional

    [Header("Music to pause (ONLY music, not cutscene audio)")]
    [SerializeField] private AudioSource musicSource; // drag your BGM source here

    [Header("Fade")]
    [SerializeField] private float fadeOutSeconds = 0.35f;
    [SerializeField] private float fadeInSeconds = 0.35f;

    private bool isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (videoGroup != null) videoGroup.alpha = 0f;
        if (fadeImage != null) SetFadeAlpha(0f);

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.APIOnly;
        }
    }

    public void PlayAndLoadScene(VideoClip clip, int sceneBuildIndex)
    {
        if (isPlaying) return;

        if (clip == null)
        {
            SceneManager.LoadScene(sceneBuildIndex);
            return;
        }

        StartCoroutine(PlayAndLoadRoutine(clip, sceneBuildIndex));
    }

    private IEnumerator PlayAndLoadRoutine(VideoClip clip, int sceneBuildIndex)
    {
        isPlaying = true;

        // ✅ Pause ONLY music
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Pause();

        // Fade to black
        yield return Fade(0f, 1f, fadeOutSeconds);

        // Show video UI
        if (videoGroup != null) videoGroup.alpha = 1f;

        // Setup video
        videoPlayer.clip = clip;

        if (videoAudioSource != null)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, videoAudioSource);
        }

        // Prepare
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;

        if (videoImage != null)
            videoImage.texture = videoPlayer.texture;

        // Play
        videoPlayer.Play();

        // Fade from black to show video
        yield return Fade(1f, 0f, fadeInSeconds);

        // Wait until done
        while (videoPlayer.isPlaying) yield return null;

        // Fade to black at end
        yield return Fade(0f, 1f, fadeOutSeconds);

        // Hide video UI (still black)
        if (videoGroup != null) videoGroup.alpha = 0f;
        if (videoImage != null) videoImage.texture = null;

        // Load next scene while black
        yield return SceneManager.LoadSceneAsync(sceneBuildIndex);
        yield return null;

        // Fade into the new scene
        yield return Fade(1f, 0f, fadeInSeconds);

        // ✅ Resume ONLY music
        if (musicSource != null)
            musicSource.UnPause();

        isPlaying = false;
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        if (fadeImage == null) yield break;

        seconds = Mathf.Max(0.01f, seconds);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / seconds;
            SetFadeAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetFadeAlpha(to);
    }

    private void SetFadeAlpha(float a)
    {
        var c = fadeImage.color;
        c.a = Mathf.Clamp01(a);
        fadeImage.color = c;
    }
}