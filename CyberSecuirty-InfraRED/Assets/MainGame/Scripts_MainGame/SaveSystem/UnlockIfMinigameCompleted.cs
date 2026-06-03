using UnityEngine;

public class UnlockIfMinigameCompleted : MonoBehaviour
{
    [SerializeField] private string requiredMinigameId = "minigame1";
    [SerializeField] private GameObject thingToEnable; // door collider, button, etc.

    private void OnEnable()
    {
        SaveManager.OnSaveChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        SaveManager.OnSaveChanged -= Refresh;
    }

    private void Refresh()
    {
        bool unlocked = SaveManager.IsMinigameCompleted(requiredMinigameId);
        thingToEnable.SetActive(unlocked);
    }
}