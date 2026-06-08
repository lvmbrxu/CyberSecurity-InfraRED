using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MainMenuPlay : MonoBehaviour
{
    [SerializeField] private int mainGameBuildIndex = 0;
    [SerializeField] private VideoClip introCutscene;
    [SerializeField] private string introNarrationEventId = "INTRO";

    private bool running;

    public void OnPlayPressed()
    {
        if (running) return;
        running = true;

        SaveManager.EnsureLoaded();

        // If they already have progress, skip intro flow
        if (SaveManager.HasProgress())
        {
            SceneManager.LoadScene(mainGameBuildIndex);
            return;
        }

        // Fresh run: enforce defaults and queue intro narration
        SaveManager.SetLastMainSpawn("Default");

        if (!string.IsNullOrWhiteSpace(introNarrationEventId))
            SaveManager.EnqueueMainEvent(introNarrationEventId);

        // Cutscene then main game
        if (introCutscene != null && CutsceneSystem.Instance != null)
            CutsceneSystem.Instance.PlayAndLoadScene(introCutscene, mainGameBuildIndex);
        else
            SceneManager.LoadScene(mainGameBuildIndex);
    }

    // Optional: separate New Game button that always resets
    public void OnNewGamePressed()
    {
        if (running) return;
        running = true;

        SaveManager.EnsureLoaded();
        SaveManager.ResetSave();

        SaveManager.SetLastMainSpawn("Default");
        SaveManager.EnqueueMainEvent(introNarrationEventId);

        if (introCutscene != null && CutsceneSystem.Instance != null)
            CutsceneSystem.Instance.PlayAndLoadScene(introCutscene, mainGameBuildIndex);
        else
            SceneManager.LoadScene(mainGameBuildIndex);
    }
}