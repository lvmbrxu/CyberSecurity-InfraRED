using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum SpeakerType { NPC1, NPC2, Player, Narrator }

public class DialogueUI : MonoBehaviour
{
    public System.Action Closed;

    [Header("Core UI")]
    public GameObject dialogueBox;
    public TMP_Text lineText;

    [Header("Profile Pics")]
    public GameObject npc1ProfilePic;     // NPC 1 portrait
    public GameObject npc2ProfilePic;     // NPC 2 portrait
    public GameObject playerProfilePic;   // Player portrait
    public GameObject narratorProfilePic; // Narrator portrait (optional)

    [Header("Buttons")]
    public Button continueButton;
    public Button goodChoiceButton;
    public Button badChoiceButton;
    public TMP_Text goodChoiceLabel;
    public TMP_Text badChoiceLabel;

    [Header("State")]
    public GameStateSO gameState;

    [Header("Freeze Player (Click-to-Move script)")]
    public MonoBehaviour clickToMoveScript;

    [Header("Narration Camera Cue (optional)")]
    [Tooltip("If ON, will swap camera when narration reaches the chosen line index.")]
    [SerializeField] private bool enableNarrationCameraCue = false;

    [Tooltip("Drag your CameraPrioritySwap_CM3 here.")]
    [SerializeField] private CameraPrioritySwap_CM3 narrationCameraSwap;

    [Tooltip("0 = first line, 1 = second line, 2 = third line...")]
    [SerializeField] private int cameraCueLineIndex = 1; // after 2nd element = 1 (0-based)

    private bool cameraCueFired;

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

        SetSpeaker(choiceData.npcSpeaker);

        lineText.gameObject.SetActive(true);
        lineText.text = choiceData.npcLine;

        SetChoicesVisible(false);
        SetContinueVisible(true);
    }

    public void OpenNarration(NarrationSequenceSO sequence)
    {
        if (sequence == null || sequence.lines == null || sequence.lines.Length == 0) return;

        mode = Mode.Narration;
        narrationSequence = sequence;
        narrationIndex = 0;

        // reset cue each time narration starts
        cameraCueFired = false;

        ShowUI();
        FreezePlayer(true);

        SetChoicesVisible(false);
        SetContinueVisible(true);

        ShowNarrationLine();
    }

    public void Hide()
    {
        // restore camera if we swapped it during narration
        if (enableNarrationCameraCue && narrationCameraSwap != null && cameraCueFired)
            narrationCameraSwap.Restore();

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

        Closed?.Invoke();
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

                // Hide text during choices
                lineText.gameObject.SetActive(false);

                goodChoiceLabel.text = choiceData.choiceAText;
                badChoiceLabel.text = choiceData.choiceBText;

                // disable continue so it doesn't block option clicks
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

        // Apply outcomes
        if (choiceData.affectsPlatforms)
            gameState.platformGlitchMode = isA ? choiceData.platformResultIfA : choiceData.platformResultIfB;

        if (choiceData.affectsPopups)
            gameState.popupMode = isA ? choiceData.popupResultIfA : choiceData.popupResultIfB;

        choiceStep = ChoiceStep.NpcFeedback;

        // Feedback from same NPC
        SetSpeaker(choiceData.npcSpeaker);

        lineText.gameObject.SetActive(true);
        lineText.text = isA ? choiceData.choiceAFeedback : choiceData.choiceBFeedback;

        SetChoicesVisible(false);
        SetContinueVisible(true);
    }

    private void ShowNarrationLine()
    {
        var line = narrationSequence.lines[narrationIndex];

        SetSpeaker(line.speaker);

        lineText.gameObject.SetActive(true);
        lineText.text = line.text;

        // 🔥 Camera cue: trigger once at chosen narration line index
        if (enableNarrationCameraCue &&
            !cameraCueFired &&
            narrationCameraSwap != null &&
            narrationIndex == cameraCueLineIndex)
        {
            cameraCueFired = true;
            narrationCameraSwap.ActivateTargetCamera();
        }
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
        if (npc1ProfilePic != null) npc1ProfilePic.SetActive(false);
        if (npc2ProfilePic != null) npc2ProfilePic.SetActive(false);
        if (playerProfilePic != null) playerProfilePic.SetActive(false);
        if (narratorProfilePic != null) narratorProfilePic.SetActive(false);

        switch (speaker)
        {
            case SpeakerType.NPC1:
                if (npc1ProfilePic != null) npc1ProfilePic.SetActive(true);
                break;
            case SpeakerType.NPC2:
                if (npc2ProfilePic != null) npc2ProfilePic.SetActive(true);
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