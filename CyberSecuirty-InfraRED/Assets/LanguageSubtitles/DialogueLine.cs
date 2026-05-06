using UnityEngine;

namespace Game.Dialogue
{
    [CreateAssetMenu(menuName = "Game/Dialogue/Dialogue Line")]
    public class DialogueLine : ScriptableObject
    {
        [Header("Subtitles")]
        [TextArea(2, 6)] public string englishText;
        [TextArea(2, 6)] public string dutchText;

        [Header("Voice (optional)")]
        public AudioClip englishVoice;
        public AudioClip dutchVoice;

        [Header("Optional UI")]
        public string speakerName;

        public string GetText(GameLanguage lang)
            => lang == GameLanguage.Dutch ? dutchText : englishText;

        public AudioClip GetVoice(GameLanguage lang)
            => lang == GameLanguage.Dutch ? dutchVoice : englishVoice;
    }
}