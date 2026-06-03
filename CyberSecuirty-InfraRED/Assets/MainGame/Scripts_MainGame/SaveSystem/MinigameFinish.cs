using UnityEngine;
using UnityEngine.SceneManagement;

public class MinigameFinish : MonoBehaviour
{
    [SerializeField] private string minigameId = "minigame1";
    [SerializeField] private int mainSceneBuildIndex = 0;
    [SerializeField] private string returnSpawnId = "FromMinigame1";

    // What cutscene/event should play back in main after this minigame?
    [SerializeField] private string mainEventId = "cutscene_after_minigame1";

    public void FinishMinigame()
    {
        SaveManager.MarkMinigameCompleted(minigameId);
        SaveManager.SetLastMainSpawn(returnSpawnId);

        // Skip main intro because we're returning mid-progress
        SaveManager.SetSkipMainIntro(true);

        // Enqueue the cutscene/event to play once in the main scene
        if (!string.IsNullOrWhiteSpace(mainEventId))
            SaveManager.EnqueueMainEvent(mainEventId);

        SceneManager.LoadScene(mainSceneBuildIndex);
    }
}