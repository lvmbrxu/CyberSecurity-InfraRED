// SceneLoader.cs
// Put on a GameObject in every relevant scene (or a persistent one).
// Then wire UI buttons to these public methods.
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoader : MonoBehaviour
{
    [Header("Scene Build Indexes")]
    public int mainMenuBuildIndex = 0;
    public int gameBuildIndex = 1;        // this block-blast scene
    public int nextLevelBuildIndex = 2;   // optional

    public void LoadMainMenu() => LoadByIndex(mainMenuBuildIndex);
    public void RestartCurrent() => LoadByIndex(SceneManager.GetActiveScene().buildIndex);
    public void LoadGame() => LoadByIndex(gameBuildIndex);
    public void LoadNextLevel() => LoadByIndex(nextLevelBuildIndex);

    public void LoadByIndex(int buildIndex)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(buildIndex);
    }

    public void Quit()
    {
        Application.Quit();
    }
}