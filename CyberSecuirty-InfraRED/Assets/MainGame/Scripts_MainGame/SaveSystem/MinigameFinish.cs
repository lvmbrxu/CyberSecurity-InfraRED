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

    [Header("Reset (minigame only)")]
    [Tooltip("If true, destroys any DontDestroyOnLoad objects that belong to this minigame (prevents broken restarts).")]
    [SerializeField] private bool cleanupMinigameDontDestroyOnLoad = true;

    [Tooltip("Optional name filter for persistent minigame roots to destroy (example: DoodleJumpManager). Leave empty to skip.")]
    [SerializeField] private string[] ddolNameContains;

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

        // Reset minigame runtime leftovers (IMPORTANT if anything in this minigame was marked DontDestroyOnLoad)
        if (cleanupMinigameDontDestroyOnLoad)
            CleanupMinigameDDOL();

        // No cutscene -> load target immediately
        if (!playCutsceneBeforeLoad || cutsceneClip == null || CutsceneSystem.Instance == null)
        {
            SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
            return;
        }

        // Smooth cutscene -> scene (no "flash")
        CutsceneSystem.Instance.PlayAndLoadScene(cutsceneClip, targetScene);
    }

    private void CleanupMinigameDDOL()
    {
        // Destroy only minigame-related persistent objects, never SaveManager / global systems.
        var all = FindObjectsOfType<GameObject>(true);
        for (int i = 0; i < all.Length; i++)
        {
            var go = all[i];
            if (go == null) continue;

            if (go.scene.name != "DontDestroyOnLoad")
                continue;

            // Never kill SaveManager or other known globals.
            if (go.GetComponent<SaveManager>() != null) continue;
            if (go.GetComponent<CutsceneSystem>() != null) continue;

            // If user provided filters, use them.
            if (ddolNameContains != null && ddolNameContains.Length > 0)
            {
                string n = go.name;
                bool match = false;
                for (int k = 0; k < ddolNameContains.Length; k++)
                {
                    string token = ddolNameContains[k];
                    if (string.IsNullOrWhiteSpace(token)) continue;

                    if (n.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        match = true;
                        break;
                    }
                }

                if (match)
                    Destroy(go);
            }
        }
    }
}