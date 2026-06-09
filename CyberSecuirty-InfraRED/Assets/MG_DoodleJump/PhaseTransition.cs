using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PhaseTransition : MonoBehaviour
{
    [Header("Fade UI")]
    [SerializeField] private Image fadeImage;
    [SerializeField, Min(0.01f)] private float fadeOutTime = 0.18f;
    [SerializeField, Min(0.01f)] private float fadeInTime = 0.18f;
    [SerializeField] private int sortingOrder = 9999;

    [Header("Music (optional)")]
    [Tooltip("If left empty, PhaseTransition will try to find an AudioSource in the scene.")]
    [SerializeField] private AudioSource musicSource;

    [Tooltip("Music for phase 1 (before swap).")]
    [SerializeField] private AudioClip phase1Music;

    [Tooltip("Music for phase 2 (after swap).")]
    [SerializeField] private AudioClip phase2Music;

    [Tooltip("Crossfade time during the blackout.")]
    [SerializeField, Min(0f)] private float musicCrossfadeSeconds = 0.25f;

    public bool IsBusy { get; private set; }

    private void Awake()
    {
        EnsureOverlay();
        SetAlpha(0f);

        // auto-find if not assigned
        if (musicSource == null)
            musicSource = FindFirstObjectByType<AudioSource>();
    }

    /// <summary>
    /// Fade to black -> (swap) -> fade in.
    /// If you assign phase2Music, it will switch/crossfade during the black screen.
    /// </summary>
    public IEnumerator FadeSwap(Action swap)
    {
        EnsureOverlay();
        if (fadeImage == null || IsBusy) yield break;
        IsBusy = true;

        var canvas = fadeImage.canvas;
        if (canvas != null) canvas.sortingOrder = sortingOrder;
        fadeImage.transform.SetAsLastSibling();

        // Fade out (to black)
        yield return FadeTo(1f, fadeOutTime);

        // While black: do the swap + music change
        if (phase2Music != null)
            yield return SwitchMusic(phase2Music);

        swap?.Invoke();

        // Fade back in
        yield return FadeTo(0f, fadeInTime);

        IsBusy = false;
    }

    // Call this at the start of doodlejump if you want phase1 music
    public void PlayPhase1Music()
    {
        if (musicSource == null || phase1Music == null) return;

        musicSource.clip = phase1Music;
        if (!musicSource.isPlaying) musicSource.Play();
    }

    // -------------------- internals --------------------

    private void EnsureOverlay()
    {
        if (fadeImage != null) return;

        var canvasGo = new GameObject("PhaseFadeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var imgGo = new GameObject("FadeImage", typeof(Image));
        imgGo.transform.SetParent(canvasGo.transform, false);

        fadeImage = imgGo.GetComponent<Image>();
        fadeImage.raycastTarget = false;
        fadeImage.color = Color.black;

        var rt = imgGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private IEnumerator FadeTo(float targetA, float time)
    {
        float startA = fadeImage.color.a;
        float t = 0f;

        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(startA, targetA, t / time);
            SetAlpha(a);
            yield return null;
        }

        SetAlpha(targetA);
    }

    private void SetAlpha(float a)
    {
        var c = fadeImage.color;
        c.a = Mathf.Clamp01(a);
        fadeImage.color = c;
    }

    private IEnumerator SwitchMusic(AudioClip nextClip)
    {
        if (musicSource == null) yield break;

        // If no crossfade, just swap
        if (musicCrossfadeSeconds <= 0.001f)
        {
            musicSource.clip = nextClip;
            musicSource.Play();
            yield break;
        }

        // Fade out current
        float startVol = musicSource.volume;
        float t = 0f;

        while (t < musicCrossfadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / musicCrossfadeSeconds);
            yield return null;
        }

        musicSource.volume = 0f;

        // Swap clip
        musicSource.clip = nextClip;
        musicSource.Play();

        // Fade in
        t = 0f;
        while (t < musicCrossfadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, startVol, t / musicCrossfadeSeconds);
            yield return null;
        }

        musicSource.volume = startVol;
    }
}