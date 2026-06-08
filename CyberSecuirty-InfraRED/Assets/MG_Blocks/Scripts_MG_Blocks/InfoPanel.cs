using UnityEngine;

public class InfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private GameObject cluesUI;

    [Header("Countdown (drag StartCountdownUI here)")]
    [SerializeField] private StartCountdownUI countdown;

    private void Start()
    {
        Time.timeScale = 0f;

        if (cluesUI != null)
            cluesUI.SetActive(false);
    }

    public void StartGame()
    {
        // Hide info panel UI
        if (infoPanel != null)
            infoPanel.SetActive(false);

        // Show your in-game UI
        if (cluesUI != null)
            cluesUI.SetActive(true);

        // Start countdown AFTER button press
        if (countdown != null)
            countdown.StartCountdown();
        else
            Time.timeScale = 1f; // fallback
    }
}