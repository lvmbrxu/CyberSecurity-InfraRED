using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Guaranteed fullscreen fade overlay.
/// - If no Image is assigned, builds its own ScreenSpace-Overlay canvas + black image at runtime.
/// - Always renders on top (high sortingOrder).
/// - Uses unscaled time (works even if timeScale = 0 later).
/// </summary>
[DisallowMultipleComponent]
public sealed class PhaseTransition : MonoBehaviour
{
    [Header("Optional (can be null)")]
    [SerializeField] private Image fadeImage;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float fadeOutTime = 0.12f;
    [SerializeField, Min(0.01f)] private float fadeInTime = 0.12f;

    [Header("Overlay Sort")]
    [SerializeField] private int sortingOrder = 9999;

    public bool IsBusy { get; private set; }

    private void Awake()
    {
        EnsureOverlay();
        SetAlpha(0f);
    }

    public IEnumerator FadeSwap(Action swap)
    {
        EnsureOverlay();
        if (fadeImage == null || IsBusy) yield break;

        IsBusy = true;

        // Hard guarantee top-most ordering.
        var canvas = fadeImage.canvas;
        if (canvas != null) canvas.sortingOrder = sortingOrder;
        fadeImage.transform.SetAsLastSibling();

        yield return FadeTo(1f, fadeOutTime);
        swap?.Invoke();
        yield return FadeTo(0f, fadeInTime);

        IsBusy = false;
    }

    // ---- internals ----

    private void EnsureOverlay()
    {
        if (fadeImage != null) return;

        // Build an overlay canvas under this object.
        var canvasGo = new GameObject("PhaseFadeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Fullscreen image
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
        if (fadeImage == null) return;
        var c = fadeImage.color;
        fadeImage.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
    }
}