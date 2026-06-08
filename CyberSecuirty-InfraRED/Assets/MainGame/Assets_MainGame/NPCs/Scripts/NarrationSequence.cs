using UnityEngine;

[CreateAssetMenu(menuName = "CyberGame/Narration Sequence", fileName = "NarrationSequence")]
public class NarrationSequenceSO : ScriptableObject
{
    [System.Serializable]
    public class Line
    {
        public SpeakerType speaker = SpeakerType.Narrator;

        [TextArea(2, 6)]
        public string text;
    }

    public Line[] lines;
}