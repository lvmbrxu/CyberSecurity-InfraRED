// PhaseTransition.cs
// Fullscreen fade to hide phase swap pops.
// Setup: Put a full-screen UI Image (black) on top of everything (Screen Space - Overlay).
// Assign that Image to fadeImage.
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PhaseTransition : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField, Min(0.01f)] private float fadeOutTime = 0.18f;
    [SerializeField, Min(0.01f)] private float fadeInTime = 0.18f;

    public bool IsBusy { get; private set; }

    public IEnumerator FadeSwap(Action swap)
    {
        if (fadeImage == null || IsBusy) yield break;
        IsBusy = true;

        yield return FadeTo(1f, fadeOutTime);
        swap?.Invoke();
        yield return FadeTo(0f, fadeInTime);

        IsBusy = false;
    }

    private IEnumerator FadeTo(float targetA, float time)
    {
        Color c = fadeImage.color;
        float startA = c.a;

        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(startA, targetA, t / time);
            fadeImage.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }

        fadeImage.color = new Color(c.r, c.g, c.b, targetA);
    }
}