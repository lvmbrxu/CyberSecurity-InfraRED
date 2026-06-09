using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public sealed class MinigameFinishAndReset : MonoBehaviour
{
    [Header("Progress")]
    [SerializeField] private string minigameId = "final_minigame";

    [Header("Return to Main")]
    [SerializeField] private int mainSceneBuildIndex = 0;
    [SerializeField] private string returnSpawnId = "Default";

    [Header("Optional: end narration in Main")]
    [SerializeField] private string endNarrationEventId = "end_narrator";

    [Header("Optional: cutscene before returning")]
    [SerializeField] private VideoClip cutscene;

    private bool done;

    public void Finish()
    {
        if (done) return;
        done = true;

        // Save progress (DO NOT reset save)
        SaveManager.MarkMinigameCompleted(minigameId);
        SaveManager.SetLastMainSpawn(returnSpawnId);

        if (!string.IsNullOrWhiteSpace(endNarrationEventId))
            SaveManager.EnqueueMainEvent(endNarrationEventId);

        // Hard reset THIS minigame scene so it’s clean next time
        int thisScene = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(thisScene, LoadSceneMode.Single);

        // Now go to main (cutscene optional)
        if (cutscene != null && CutsceneSystem.Instance != null)
            CutsceneSystem.Instance.PlayAndLoadScene(cutscene, mainSceneBuildIndex);
        else
            SceneManager.LoadScene(mainSceneBuildIndex, LoadSceneMode.Single);
    }
}