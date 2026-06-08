using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MainMenuPlay : MonoBehaviour
{
    [Header("Load Main Game")]
    [SerializeField] private int mainGameBuildIndex = 1;

    [Header("Intro Cutscene Video")]
    [SerializeField] private VideoClip introCutscene;

    [Header("Main Scene Narration Event Id")]
    [SerializeField] private string introNarrationEventId = "intro_narration";

    private bool running;

    public void OnPlayPressed()
    {
        if (running) return;
        running = true;

        // We WANT the intro narration to play in MainGame
        SaveManager.SetSkipMainIntro(false);

        // Queue the narration event once (played by MainSceneSpawner in MainGame)
        if (!string.IsNullOrWhiteSpace(introNarrationEventId))
            SaveManager.EnqueueMainEvent(introNarrationEventId);

        // Play cutscene, then load MainGame without flashing menu
        if (introCutscene != null && CutsceneSystem.Instance != null)
        {
            CutsceneSystem.Instance.PlayAndLoadScene(introCutscene, mainGameBuildIndex);
        }
        else
        {
            // No cutscene system (or no clip) -> go straight to MainGame
            SceneManager.LoadScene(mainGameBuildIndex);
        }
    }
}