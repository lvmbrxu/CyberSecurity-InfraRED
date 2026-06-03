using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Start-of-run countdown: 3 2 1 GO.
/// - Uses unscaled time so it works while timeScale=0.
/// - Freezes gameplay during countdown.
/// - Optionally disables player script until GO.
/// </summary>
[DisallowMultipleComponent]
public sealed class StartCountdownUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text countdownText;

    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float stepSeconds = 1.0f;
    [SerializeField, Min(0f)] private float goHoldSeconds = 0.35f;

    [Header("Gameplay Lock")]
    [Tooltip("Disable this during countdown (recommended: your player controller script).")]
    [SerializeField] private Behaviour gameplayToDisable;

    [SerializeField] private bool freezeTimeScale = true;

    private void Start()
    {
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (countdownText != null) countdownText.gameObject.SetActive(true);

        // Lock gameplay.
        if (gameplayToDisable != null) gameplayToDisable.enabled = false;

        float prevTimeScale = Time.timeScale;
        if (freezeTimeScale) Time.timeScale = 0f;

        // 3 2 1
        yield return Show("3", stepSeconds);
        yield return Show("2", stepSeconds);
        yield return Show("1", stepSeconds);

        // GO
        yield return Show("GO!", goHoldSeconds);

        // Unlock gameplay.
        if (freezeTimeScale) Time.timeScale = prevTimeScale;
        if (gameplayToDisable != null) gameplayToDisable.enabled = true;

        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    private IEnumerator Show(string text, float seconds)
    {
        if (countdownText != null) countdownText.text = text;

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}