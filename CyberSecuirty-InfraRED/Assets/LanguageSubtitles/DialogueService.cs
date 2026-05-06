using UnityEngine;

namespace Game.Dialogue
{
    public sealed class DialogueService : MonoBehaviour
    {
        public static DialogueService Instance { get; private set; }

        [SerializeField] private DialoguePlayer player;

        public DialoguePlayer Player => player;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (player == null)
                Debug.LogError("DialogueService: Assign the DialoguePlayer reference.", this);
        }

        public void Play(DialogueSequence sequence) => player.Play(sequence);
        public void Next() => player.Next();
        public void Stop() => player.Stop();
        public bool IsPlaying => player != null && player.IsPlaying;
    }
}