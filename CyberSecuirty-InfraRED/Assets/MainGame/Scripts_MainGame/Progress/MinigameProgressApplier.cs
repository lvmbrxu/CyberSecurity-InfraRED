using UnityEngine;

public class MinigameProgressApplier : MonoBehaviour
{
    [Header("Progress ID to check")]
    [SerializeField] private string minigameId = "doodlejump";

    [Header("Block replay")]
    [SerializeField] private GameObject replayTriggerObject; // the trigger GO or collider GO

    [Header("Lights / visuals")]
    [SerializeField] private GameObject lightOnn;
    [SerializeField] private GameObject lightOff;
    [SerializeField] private GameObject lightOnn2;
    [SerializeField] private GameObject lightOff2;
    [SerializeField] private GameObject lightObjectOnn;
    [SerializeField] private GameObject lightObjectOff;

    [Header("Next game unlock visuals")]
    [SerializeField] private GameObject nextGameGL;
    [SerializeField] private GameObject nextGameGL2;
    [SerializeField] private GameObject nextGameRL;
    [SerializeField] private GameObject nextGameRL2;
    [SerializeField] private GameObject nextGameObjectOnn;
    [SerializeField] private GameObject nextGameObjectOff;

    [Header("Wall / blockers")]
    [SerializeField] private GameObject invisWall;

    private void Start()
    {
        Apply();
        SaveManager.OnSaveChanged += Apply; // if save changes while scene is open
    }

    private void OnDestroy()
    {
        SaveManager.OnSaveChanged -= Apply;
    }

    private void Apply()
    {
        bool done = SaveManager.IsMinigameCompleted(minigameId);

        // If not done, keep default scene setup (do nothing)
        if (!done) return;

        // Block replay
        if (replayTriggerObject != null)
            replayTriggerObject.SetActive(false); // simplest

        // Swap lights
        if (lightOnn != null) lightOnn.SetActive(false);
        if (lightOff != null) lightOff.SetActive(true);

        if (lightOnn2 != null) lightOnn2.SetActive(false);
        if (lightOff2 != null) lightOff2.SetActive(true);

        if (lightObjectOnn != null) lightObjectOnn.SetActive(false);
        if (lightObjectOff != null) lightObjectOff.SetActive(true);

        // Next game indicators
        if (nextGameGL != null) nextGameGL.SetActive(true);
        if (nextGameGL2 != null) nextGameGL2.SetActive(true);

        if (nextGameRL != null) nextGameRL.SetActive(false);
        if (nextGameRL2 != null) nextGameRL2.SetActive(false);

        if (nextGameObjectOnn != null) nextGameObjectOnn.SetActive(true);
        if (nextGameObjectOff != null) nextGameObjectOff.SetActive(false);

        // Unlock wall
        if (invisWall != null) invisWall.SetActive(false);
    }
}