using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [Header("Core UI")]
    public GameObject dialogueBox;      // DialogueBox
    public TMP_Text npcText;            // NpcText (TMP)

    [Header("Profile Pics")]
    public GameObject npcProfilePic;    // ProfilePicPassword (NPC)
    public GameObject playerProfilePic; // ProfilePicPlayer (Player)

    [Header("Buttons")]
    public Button continueButton;       // ContinueButton (stretch full panel, alpha 0)
    public Button goodChoiceButton;     // ButtonGoodChoice
    public Button badChoiceButton;      // ButtonBadChoice
    public TMP_Text goodChoiceLabel;    // ButtonGoodChoice/Text (TMP)
    public TMP_Text badChoiceLabel;     // ButtonBadChoice/Text (TMP)

    [Header("Data")]
    public GameStateSO gameState;

    [Header("Freeze Player (Click-to-Move script)")]
    public MonoBehaviour clickToMoveScript; 

    private DialogueDataSO currentData;

    private enum Step { Hidden, NpcTalking, PlayerChoosing, NpcFeedback }
    private Step step = Step.Hidden;

    private void Awake()
    {
        // Button wiring once
        continueButton.onClick.AddListener(OnContinueClicked);
        goodChoiceButton.onClick.AddListener(() => OnChoiceClicked(isA: true));
        badChoiceButton.onClick.AddListener(() => OnChoiceClicked(isA: false));

        Hide(); 
    }

    public void Open(DialogueDataSO data)
    {
        if (data == null) return;

        currentData = data;
        dialogueBox.SetActive(true);

        if (clickToMoveScript != null)
            clickToMoveScript.enabled = false;

        step = Step.NpcTalking;

        npcProfilePic.SetActive(true);
        playerProfilePic.SetActive(false);

        SetNpcTextVisible(true);
        npcText.text = currentData.npcLine;

        SetChoicesVisible(false);
        SetContinueVisible(true);
    }

    private void OnContinueClicked()
    {
        if (currentData == null) return;

        if (step == Step.NpcTalking)
        {
            step = Step.PlayerChoosing;

            npcProfilePic.SetActive(false);
            playerProfilePic.SetActive(true);
            
            SetNpcTextVisible(false);

            goodChoiceLabel.text = currentData.choiceAText;
            badChoiceLabel.text  = currentData.choiceBText;
            
            SetContinueVisible(false);
            SetChoicesVisible(true);
        }
        else if (step == Step.NpcFeedback)
        {
            // Close after feedback
            Hide();
        }
    }

    private void OnChoiceClicked(bool isA)
    {
        if (currentData == null) return;
        if (step != Step.PlayerChoosing) return;

        // Apply results to GameState (A = good button, B = bad button)
        if (currentData.affectsPlatforms)
            gameState.platformGlitchMode = isA ? currentData.platformResultIfA : currentData.platformResultIfB;

        if (currentData.affectsPopups)
            gameState.popupMode = isA ? currentData.popupResultIfA : currentData.popupResultIfB;

        // STEP 3: NPC feedback 
        step = Step.NpcFeedback;

        playerProfilePic.SetActive(false);
        npcProfilePic.SetActive(true);

        SetNpcTextVisible(true);
        npcText.text = isA ? currentData.choiceAFeedback : currentData.choiceBFeedback;

        SetChoicesVisible(false);
        SetContinueVisible(true); // click anywhere to close
    }

    public void Hide()
    {
        step = Step.Hidden;
        currentData = null;

        dialogueBox.SetActive(false);

        // Unfreeze movement
        if (clickToMoveScript != null)
            clickToMoveScript.enabled = true;
    }

    private void SetChoicesVisible(bool visible)
    {
        goodChoiceButton.gameObject.SetActive(visible);
        badChoiceButton.gameObject.SetActive(visible);
    }

    private void SetContinueVisible(bool visible)
    {
        continueButton.gameObject.SetActive(visible);
    }

    private void SetNpcTextVisible(bool visible)
    {
        // Safer than clearing text; stops it rendering at all.
        npcText.gameObject.SetActive(visible);
    }

    public bool IsOpen => dialogueBox.activeSelf;
}