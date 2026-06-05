using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    public System.Action Closed; // ✅ camera restore hooks into this

    [Header("Core UI")]
    public GameObject dialogueBox; // DialogueBox
    public TMP_Text lineText;      // NpcText (TMP) - can be used for narrator too

    [Header("Profile Pics")]
    public GameObject npcProfilePic;      // ProfilePicPassword
    public GameObject playerProfilePic;   // ProfilePicPlayer
    public GameObject narratorProfilePic; // optional

    [Header("Buttons")]
    public Button continueButton;       // ContinueButton (stretch full panel, alpha 0)
    public Button goodChoiceButton;     // ButtonGoodChoice
    public Button badChoiceButton;      // ButtonBadChoice
    public TMP_Text goodChoiceLabel;    // ButtonGoodChoice/Text (TMP)
    public TMP_Text badChoiceLabel;     // ButtonBadChoice/Text (TMP)

    [Header("State")]
    public GameStateSO gameState;

    [Header("Freeze Player (Click-to-Move script)")]
    public MonoBehaviour clickToMoveScript; // drag your click-to-move component here

    // ---- Choice dialogue data ----
    private DialogueDataSO choiceData;

    // ---- Narration data ----
    private NarrationSequenceSO narrationSequence;
    private int narrationIndex;

    private enum Mode { None, Choice, Narration }
    private Mode mode = Mode.None;

    private enum ChoiceStep { Hidden, NpcLine, Choosing, NpcFeedback }
    private ChoiceStep choiceStep = ChoiceStep.Hidden;

    private void Awake()
    {
        continueButton.onClick.AddListener(OnContinueClicked);
        goodChoiceButton.onClick.AddListener(() => OnChoiceClicked(isA: true));
        badChoiceButton.onClick.AddListener(() => OnChoiceClicked(isA: false));

        Hide(); // start hidden
    }

    // =========================
    // PUBLIC API
    // =========================

    public void OpenChoice(DialogueDataSO data)
    {
        if (data == null) return;

        mode = Mode.Choice;
        choiceData = data;

        ShowUI();
        FreezePlayer(true);

        choiceStep = ChoiceStep.NpcLine;

        SetSpeaker(SpeakerType.NPC);
        lineText.gameObject.SetActive(true);
        lineText.text = choiceData.npcLine;

        SetChoicesVisible(false);
        SetContinueVisible(true); // click anywhere
    }

    public void OpenNarration(NarrationSequenceSO sequence)
    {
        if (sequence == null || sequence.lines == null || sequence.lines.Length == 0) return;

        mode = Mode.Narration;
        narrationSequence = sequence;
        narrationIndex = 0;

        ShowUI();
        FreezePlayer(true);

        SetChoicesVisible(false);
        SetContinueVisible(true);

        ShowNarrationLine();
    }

    public void Hide()
    {
        mode = Mode.None;
        choiceStep = ChoiceStep.Hidden;

        choiceData = null;
        narrationSequence = null;
        narrationIndex = 0;

        if (dialogueBox != null)
            dialogueBox.SetActive(false);

        FreezePlayer(false);

        if (lineText != null)
            lineText.gameObject.SetActive(true);

        Closed?.Invoke(); // ✅ camera swap restores here
    }

    public bool IsOpen => dialogueBox != null && dialogueBox.activeSelf;

    // =========================
    // INTERNAL FLOW
    // =========================

    private void OnContinueClicked()
    {
        if (!IsOpen) return;

        if (mode == Mode.Narration)
        {
            narrationIndex++;

            if (narrationSequence == null || narrationIndex >= narrationSequence.lines.Length)
            {
                Hide();
                return;
            }

            ShowNarrationLine();
            return;
        }

        if (mode == Mode.Choice && choiceData != null)
        {
            if (choiceStep == ChoiceStep.NpcLine)
            {
                choiceStep = ChoiceStep.Choosing;

                SetSpeaker(SpeakerType.Player);

                // Hide text during the choice phase (your requirement)
                lineText.gameObject.SetActive(false);

                goodChoiceLabel.text = choiceData.choiceAText;
                badChoiceLabel.text  = choiceData.choiceBText;

                // IMPORTANT: disable continue so it doesn't block option clicks
                SetContinueVisible(false);
                SetChoicesVisible(true);
            }
            else if (choiceStep == ChoiceStep.NpcFeedback)
            {
                Hide();
            }
        }
    }

    private void OnChoiceClicked(bool isA)
    {
        if (mode != Mode.Choice) return;
        if (choiceData == null) return;
        if (choiceStep != ChoiceStep.Choosing) return;

        if (choiceData.affectsPlatforms)
            gameState.platformGlitchMode = isA ? choiceData.platformResultIfA : choiceData.platformResultIfB;

        if (choiceData.affectsPopups)
            gameState.popupMode = isA ? choiceData.popupResultIfA : choiceData.popupResultIfB;

        choiceStep = ChoiceStep.NpcFeedback;

        SetSpeaker(SpeakerType.NPC);

        lineText.gameObject.SetActive(true);
        lineText.text = isA ? choiceData.choiceAFeedback : choiceData.choiceBFeedback;

        SetChoicesVisible(false);
        SetContinueVisible(true); // click anywhere to close
    }

    private void ShowNarrationLine()
    {
        var line = narrationSequence.lines[narrationIndex];

        SetSpeaker(line.speaker);

        lineText.gameObject.SetActive(true);
        lineText.text = line.text;
    }

    private void ShowUI()
    {
        if (dialogueBox != null)
            dialogueBox.SetActive(true);
    }

    private void FreezePlayer(bool freeze)
    {
        if (clickToMoveScript != null)
            clickToMoveScript.enabled = !freeze;
    }

    private void SetSpeaker(SpeakerType speaker)
    {
        if (npcProfilePic != null) npcProfilePic.SetActive(false);
        if (playerProfilePic != null) playerProfilePic.SetActive(false);
        if (narratorProfilePic != null) narratorProfilePic.SetActive(false);

        switch (speaker)
        {
            case SpeakerType.NPC:
                if (npcProfilePic != null) npcProfilePic.SetActive(true);
                break;

            case SpeakerType.Player:
                if (playerProfilePic != null) playerProfilePic.SetActive(true);
                break;

            case SpeakerType.Narrator:
                if (narratorProfilePic != null) narratorProfilePic.SetActive(true);
                break;
        }
    }

    private void SetChoicesVisible(bool visible)
    {
        if (goodChoiceButton != null) goodChoiceButton.gameObject.SetActive(visible);
        if (badChoiceButton != null) badChoiceButton.gameObject.SetActive(visible);
    }

    private void SetContinueVisible(bool visible)
    {
        if (continueButton != null) continueButton.gameObject.SetActive(visible);
    }
}