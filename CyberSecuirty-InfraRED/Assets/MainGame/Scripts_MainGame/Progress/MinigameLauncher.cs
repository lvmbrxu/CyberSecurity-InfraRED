using UnityEngine;
using UnityEngine.SceneManagement;

public class MinigameLauncher : MonoBehaviour
{
    [Header("Progress ID (must match what MinigameFinish marks)")]
    [SerializeField] private string minigameId = "doodlejump";

    [Header("Minigame Scene (Build Index)")]
    [SerializeField] private int minigameSceneBuildIndex = 1;

    [Header("Optional: Where to spawn when returning to main")]
    [SerializeField] private string returnSpawnId = "FromDoodleJump";

    private void OnTriggerEnter(Collider other)
    {
        // Only player triggers it (tag approach - simplest)
        if (!other.CompareTag("Player"))
            return;

        // Block replay if completed
        if (SaveManager.IsMinigameCompleted(minigameId))
        {
            Debug.Log($"{minigameId} already completed - blocked.");
            return;
        }

        // Set where player will appear when they return
        SaveManager.SetLastMainSpawn(returnSpawnId);

        // Load minigame
        SceneManager.LoadScene(minigameSceneBuildIndex);
    }
}