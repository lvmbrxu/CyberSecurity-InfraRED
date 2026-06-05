using UnityEngine;

[CreateAssetMenu(menuName = "CyberGame/Dialogue Data", fileName = "DialogueData")]
public class DialogueDataSO : ScriptableObject
{
    [TextArea(2, 5)] public string npcLine;

    [Header("Choice A")]
    public string choiceAText;
    [TextArea(2, 5)] public string choiceAFeedback;

    [Header("Choice B")]
    public string choiceBText;
    [TextArea(2, 5)] public string choiceBFeedback;

    [Header("What this dialogue affects")]
    public bool affectsPlatforms;
    public PlatformGlitchMode platformResultIfA;
    public PlatformGlitchMode platformResultIfB;

    public bool affectsPopups;
    public PopupMode popupResultIfA;
    public PopupMode popupResultIfB;
}