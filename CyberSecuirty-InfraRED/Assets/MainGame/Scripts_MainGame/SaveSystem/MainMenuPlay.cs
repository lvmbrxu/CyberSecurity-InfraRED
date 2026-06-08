using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MainMenuPlay : MonoBehaviour
{
    [Header("Load Main Game")]
    [SerializeField] private int mainGameBuildIndex = 4;

    [Header("Intro Cutscene Video")]
    [SerializeField] private VideoClip introCutscene;

    [Header("Main Scene Narration Event Id")]
    [SerializeField] private string introNarrationEventId = "INTRO";

    private bool running;

    public void OnPlayPressed()
    {
        if (running) return;
        running = true;

        // Ensure SaveManager exists + loaded (won't crash if it doesn't, but it will warn)
        SaveManager.EnsureLoaded();

        // We want intro narration in main
        SaveManager.SetSkipMainIntro(false);

        if (!string.IsNullOrWhiteSpace(introNarrationEventId))
        {
            SaveManager.EnqueueMainEvent(introNarrationEventId.Trim());
            Debug.Log($"[MainMenuPlay] Enqueued main event '{introNarrationEventId.Trim()}'");
        }

        // Play cutscene then load main (your CutsceneSystem handles load)
        if (introCutscene != null && CutsceneSystem.Instance != null)
            CutsceneSystem.Instance.PlayAndLoadScene(introCutscene, mainGameBuildIndex);
        else
            SceneManager.LoadScene(mainGameBuildIndex);
    }
}