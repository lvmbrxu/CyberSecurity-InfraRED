// UIScript.cs
// Win -> load main menu, GameOver -> restart, etc.
using UnityEngine;

public sealed class UIScript : MonoBehaviour
{
    public GameObject gameOverPanel;
    public GameObject winPanel;

    [Header("Scene Loading")]
    public SceneLoader sceneLoader;

    void Awake()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);
    }

    public void ShowGameOver()
    {
        if (gameOverPanel) gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ShowWin()
    {
        if (winPanel) winPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    // UI Button hooks
    public void Restart() => sceneLoader.RestartCurrent();
    public void WinContinueToMenu() => sceneLoader.LoadMainMenu();
    public void ContinueNextLevel() => sceneLoader.LoadNextLevel();
}