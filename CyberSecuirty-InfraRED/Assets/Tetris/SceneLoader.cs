// SceneLoader.cs
// Drop this in ANY scene. Hook UI Buttons to these public methods.
// Uses build index OR scene name. Works everywhere.
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoader : MonoBehaviour
{
    [Header("Default targets (optional)")]
    [Tooltip("If set, LoadMainMenu() loads this build index.")]
    public int mainMenuBuildIndex = 0;

    [Tooltip("If set, LoadNext() loads this build index.")]
    public int nextBuildIndex = -1;

    // ---- Button hooks ----

    public void RestartCurrent()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuBuildIndex);
    }

    public void LoadNext()
    {
        Time.timeScale = 1f;

        int idx = nextBuildIndex >= 0
            ? nextBuildIndex
            : SceneManager.GetActiveScene().buildIndex + 1;

        SceneManager.LoadScene(idx);
    }

    // ---- Generic (use for any button) ----

    public void LoadByBuildIndex(int buildIndex)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(buildIndex);
    }

    public void LoadByName(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void Quit()
    {
        Application.Quit();
    }
}