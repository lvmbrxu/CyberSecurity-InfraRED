using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StartCountdownUI : MonoBehaviour
{
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private float stepSeconds = 1f;
    [SerializeField] private float goHoldSeconds = 0.35f;
    [SerializeField] private Behaviour gameplayToDisable;
    [SerializeField] private bool freezeTimeScale = true;

    private void Start() => StartCoroutine(Run());

    private IEnumerator Run()
    {
        if (countdownText != null) countdownText.gameObject.SetActive(true);

        if (gameplayToDisable != null) gameplayToDisable.enabled = false;

        float prev = Time.timeScale;
        if (freezeTimeScale) Time.timeScale = 0f;

        yield return Show("3", stepSeconds);
        yield return Show("2", stepSeconds);
        yield return Show("1", stepSeconds);
        yield return Show("GO!", goHoldSeconds);

        if (freezeTimeScale) Time.timeScale = prev;
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