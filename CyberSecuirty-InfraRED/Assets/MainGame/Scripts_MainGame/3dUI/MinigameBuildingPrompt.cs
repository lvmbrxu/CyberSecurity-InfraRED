using UnityEngine;
using UnityEngine.SceneManagement;

public class MinigameBuildingPrompt : MonoBehaviour
{
    [Header("Progress")]
    [SerializeField] private string minigameId = "doodlejump";
    [SerializeField] private int minigameSceneIndex = 1;
    [SerializeField] private string returnSpawnId = "FromDoodleJump";

    [Header("Visuals")]
    [SerializeField] private GameObject highlightObject;   // outline/emissive mesh etc
    [SerializeField] private GameObject promptRoot;        // world-space UI root
    [SerializeField] private GameObject completedLabel;    // optional: "Completed" text GO
    [SerializeField] private GameObject playButtonRoot;    // optional: button GO to hide when completed

    private bool _playerInside;

    private void Awake()
    {
        SetVisible(false);
    }

    private void OnEnable()
    {
        SaveManager.OnSaveChanged += RefreshState;
        RefreshState();
    }

    private void OnDisable()
    {
        SaveManager.OnSaveChanged -= RefreshState;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = true;
        SetVisible(true);
        RefreshState();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = false;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (highlightObject != null) highlightObject.SetActive(visible);
        if (promptRoot != null) promptRoot.SetActive(visible);
    }

    private void RefreshState()
    {
        if (!_playerInside) return; // only update UI when visible (optional)

        bool done = SaveManager.IsMinigameCompleted(minigameId);

        if (completedLabel != null) completedLabel.SetActive(done);
        if (playButtonRoot != null) playButtonRoot.SetActive(!done);
    }

    // Hook this to the Play button OnClick()
    public void PlayMinigame()
    {
        if (SaveManager.IsMinigameCompleted(minigameId))
            return;

        SaveManager.SetLastMainSpawn(returnSpawnId);
        SceneManager.LoadScene(minigameSceneIndex);
    }

    // Hook this to the Back button OnClick()
    public void ClosePrompt()
    {
        // keep highlight if you want, but usually close both:
        SetVisible(false);
        _playerInside = false;
    }
}