using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MinigameFinish : MonoBehaviour
{
    [Header("Save / Progress")]
    [SerializeField] private string minigameId = "minigame1";

    [Header("Where to go after finishing")]
    [Tooltip("If true, loads nextSceneBuildIndex. If false, loads mainSceneBuildIndex.")]
    [SerializeField] private bool goToNextScene = false;

    [SerializeField] private int mainSceneBuildIndex = 0;
    [SerializeField] private int nextSceneBuildIndex = 2; // set this to Minigame2 for MG1 flow

    [Header("Return spawn (only used when returning to Main scene)")]
    [SerializeField] private string returnSpawnId = "FromMinigame1";

    [Header("Main Scene Event (optional, only useful when returning to Main)")]
    [SerializeField] private string mainEventId = "";

    [Header("Optional: play a cutscene BEFORE switching scenes")]
    [SerializeField] private bool playCutsceneBeforeLoad = false;
    [SerializeField] private VideoClip cutsceneClip;

    private bool finishing;

    public void FinishMinigame()
    {
        Time.timeScale = 1f;
        if (finishing) return;
        finishing = true;

        // Save progress
        SaveManager.MarkMinigameCompleted(minigameId);

        int targetScene = goToNextScene ? nextSceneBuildIndex : mainSceneBuildIndex;

        // Only set these when going back to MainGame
        if (!goToNextScene)
        {
            SaveManager.SetLastMainSpawn(returnSpawnId);
            SaveManager.SetSkipMainIntro(true);

            if (!string.IsNullOrWhiteSpace(mainEventId))
                SaveManager.EnqueueMainEvent(mainEventId);
        }

        // No cutscene -> load target immediately
        if (!playCutsceneBeforeLoad || cutsceneClip == null || CutsceneSystem.Instance == null)
        {
            SceneManager.LoadScene(targetScene);
            return;
        }

        // Smooth cutscene -> scene (no "flash")
        CutsceneSystem.Instance.PlayAndLoadScene(cutsceneClip, targetScene);
    }
}