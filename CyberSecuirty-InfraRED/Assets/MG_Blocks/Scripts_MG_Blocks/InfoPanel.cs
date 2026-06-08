using UnityEngine;

public class InfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private GameObject cluesUi;

    private void Start()
    {
        Time.timeScale = 0f;
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        if (infoPanel != null) infoPanel.SetActive(false);
        if (cluesUi != null) cluesUi.SetActive(true);
    }
}