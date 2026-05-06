using System.Collections.Generic;
using UnityEngine;

namespace Game.Dialogue
{
    [CreateAssetMenu(menuName = "Game/Dialogue/Dialogue Sequence")]
    public class DialogueSequence : ScriptableObject
    {
        public List<DialogueLine> lines = new();
    }
}