using UnityEngine;

public enum SpeakerType { NPC, Player, Narrator }

[CreateAssetMenu(menuName = "CyberGame/Narration Sequence", fileName = "NarrationSequence")]
public class NarrationSequenceSO : ScriptableObject
{
    [System.Serializable]
    public class Line
    {
        public SpeakerType speaker = SpeakerType.Narrator;

        [TextArea(2, 6)]
        public string text;

        [Header("Optional event marker (camera, animation, etc.)")]
        public bool triggerEvent;
        public string eventKey; // e.g. "CAM_ZOOM", "CAM_SHAKE"
    }

    public Line[] lines;
}