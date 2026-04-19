// UIScript.cs
// UI only: show/hide panels + freeze/unfreeze. NO scene loading.
using UnityEngine;

public sealed class UIScript : MonoBehaviour
{
    public GameObject gameOverPanel;
    public GameObject winPanel;

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

    public void HideGameOver()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    public void HideWin()
    {
        if (winPanel) winPanel.SetActive(false);
    }

    // Button hook: Continue (after win)
    public void Continue()
    {
        HideWin();
        Time.timeScale = 1f;
    }
}