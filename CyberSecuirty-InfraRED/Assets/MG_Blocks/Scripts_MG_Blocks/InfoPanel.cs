using UnityEngine;

public class InfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private GameObject cluesUI;

    [Header("Countdown")]
    [SerializeField] private StartCountdownUI countdown;

    private void Start()
    {
        Time.timeScale = 0f;
        if (cluesUI != null) cluesUI.SetActive(false);
    }

    public void StartGame()
    {
        // Hide info panel immediately.
        if (infoPanel != null) infoPanel.SetActive(false);

        // Show HUD.
        if (cluesUI != null) cluesUI.SetActive(true);

        // Start countdown (it will unpause + enable player on GO).
        if (countdown != null) countdown.Begin();
        else Time.timeScale = 1f; // fallback
    }
}