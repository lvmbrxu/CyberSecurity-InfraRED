using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class UIScript : MonoBehaviour
{
    [Header("Panels")]
    public GameObject gameOverPanel;  // lose canvas/panel root
    public GameObject winPanel;       // win canvas/panel root

    [Header("HUD: Score (assign either TMP or Text)")]
    public TMP_Text scoreTMP;
    public Text scoreText;

    [Header("HUD: Combo (assign either TMP or Text)")]
    public TMP_Text comboTMP;
    public Text comboText;
    public ComboPop comboPop; // optional

    [Header("HUD: Clues (assign either TMP or Text)")]
    public TMP_Text cluesTMP;
    public Text cluesText;

    [Tooltip("RectTransform of your clues text (target for fly-to-UI).")]
    public RectTransform cluesTargetRect;

    [Header("Final Score Text (optional, shown on win/lose panels)")]
    public TMP_Text finalScoreTMP;
    public Text finalScoreText;

    void Awake()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);

        SetScore(0);
        SetCombo(0);
        SetClues(0, 0);
        SetFinalScore(0);
    }

    // ---------- End screens ----------
    public void ShowGameOver(int finalScore)
    {
        SetFinalScore(finalScore);

        if (winPanel) winPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void ShowWin(int finalScore)
    {
        SetFinalScore(finalScore);

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void HideEndScreens()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);
    }

    // ---------- HUD ----------
    public void SetScore(int score)
    {
        string s = $"Score: {score}";
        if (scoreTMP) scoreTMP.text = s;
        if (scoreText) scoreText.text = s;
    }

    // combo <= 1 hides the text
    public void SetCombo(int combo)
    {
        if (combo <= 1)
        {
            if (comboTMP) comboTMP.text = "";
            if (comboText) comboText.text = "";
            return;
        }

        string s = $"Combo x{combo}";
        if (comboTMP) comboTMP.text = s;
        if (comboText) comboText.text = s;

        if (comboPop) comboPop.Trigger();
    }

    public void SetClues(int found, int target)
    {
        string s = (target > 0) ? $"Clues: {found}/{target}" : $"Clues: {found}";
        if (cluesTMP) cluesTMP.text = s;
        if (cluesText) cluesText.text = s;
    }

    void SetFinalScore(int score)
    {
        string s = $"Final Score: {score}";
        if (finalScoreTMP) finalScoreTMP.text = s;
        if (finalScoreText) finalScoreText.text = s;
    }
}