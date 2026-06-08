using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
public sealed class CutsceneSystem : MonoBehaviour
{
    public static CutsceneSystem Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    [Header("UI")]
    [SerializeField] private CanvasGroup videoGroup;
    [SerializeField] private RawImage videoImage;
    [SerializeField] private Image fadeImage;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource videoAudioSource;

    [Header("Music (optional)")]
    [SerializeField] private AudioSource musicSource;

    [Header("Fade")]
    [SerializeField, Min(0f)] private float fadeOutSeconds = 0.35f;
    [SerializeField, Min(0f)] private float fadeInSeconds = 0.35f;

    private bool isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // CRITICAL: Do NOT persist this object if it's on a minigame manager root.
        // If you need a persistent cutscene system, place it on a dedicated bootstrap object in Main Menu scene.

        if (videoGroup != null) videoGroup.alpha = 0f;
        SetFadeAlpha(0f);

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void PlayAndLoadScene(VideoClip clip, int sceneBuildIndex)
    {
        if (isPlaying) return;

        if (clip == null)
        {
            SceneManager.LoadScene(sceneBuildIndex, LoadSceneMode.Single);
            return;
        }

        StartCoroutine(CoPlayAndLoad(clip, sceneBuildIndex));
    }

    private IEnumerator CoPlayAndLoad(VideoClip clip, int sceneBuildIndex)
    {
        isPlaying = true;

        if (musicSource != null && musicSource.isPlaying)
            musicSource.Pause();

        yield return FadeTo(1f, fadeOutSeconds);

        if (videoGroup != null) videoGroup.alpha = 1f;

        if (videoPlayer != null)
        {
            videoPlayer.clip = clip;

            if (videoAudioSource != null)
            {
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoPlayer.SetTargetAudioSource(0, videoAudioSource);
            }

            videoPlayer.Prepare();
            while (videoPlayer != null && !videoPlayer.isPrepared)
                yield return null;

            videoPlayer.Play();
        }

        yield return FadeTo(0f, fadeInSeconds);

        while (videoPlayer != null && videoPlayer.isPlaying)
            yield return null;

        yield return FadeTo(1f, fadeOutSeconds);

        if (videoGroup != null) videoGroup.alpha = 0f;
        if (videoImage != null) videoImage.texture = null;

        yield return SceneManager.LoadSceneAsync(sceneBuildIndex, LoadSceneMode.Single);

        yield return FadeTo(0f, fadeInSeconds);

        if (musicSource != null)
            musicSource.UnPause();

        isPlaying = false;
    }

    private IEnumerator FadeTo(float targetA, float seconds)
    {
        if (fadeImage == null) yield break;

        seconds = Mathf.Max(0.01f, seconds);
        float startA = fadeImage.color.a;

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(startA, targetA, t / seconds);
            SetFadeAlpha(a);
            yield return null;
        }

        SetFadeAlpha(targetA);
    }

    private void SetFadeAlpha(float a)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = Mathf.Clamp01(a);
        fadeImage.color = c;
    }
}