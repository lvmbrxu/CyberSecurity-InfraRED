using System.Collections;
using UnityEngine;

public class MainSceneSpawner : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player; // can be null if Player is tagged "Player"

    [Header("Narration UI (in MainGame scene)")]
    [SerializeField] private DialogueUI dialogueUI;

    [System.Serializable]
    private class NarrationEvent
    {
        public string eventId;                // e.g. "intro_narration"
        public NarrationSequenceSO narration; // e.g. NS_Intro
    }

    [Header("EventId -> Narration asset mapping")]
    [SerializeField] private NarrationEvent[] narrationEvents;

    private void Start()
    {
        SaveManager.EnsureLoaded();

        // Spawn first
        SpawnPlayer();

        // Then play any queued main events (intro narration is queued from MainMenu)
        StartCoroutine(PlayQueuedMainEvents());
    }

    private void SpawnPlayer()
    {
        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (player == null)
        {
            Debug.LogWarning("MainSceneSpawner: Player not assigned and not found by tag 'Player'.");
            return;
        }

        string targetId = SaveManager.Data.lastMainSpawnId;
        var points = FindObjectsOfType<SpawnPoint>();

        foreach (var p in points)
        {
            if (p.spawnId == targetId)
            {
                player.position = p.transform.position;
                player.rotation = p.transform.rotation;
                return;
            }
        }
    }

    private IEnumerator PlayQueuedMainEvents()
    {
        // If you use this flag to skip intros when returning from minigames:
        if (SaveManager.Data.skipMainIntro)
            yield break;

        while (SaveManager.TryDequeueMainEvent(out string eventId))
        {
            var narration = FindNarration(eventId);
            if (narration == null)
            {
                Debug.LogWarning($"MainSceneSpawner: No narration mapped for eventId '{eventId}'.");
                continue;
            }

            if (dialogueUI == null)
            {
                Debug.LogError("MainSceneSpawner: dialogueUI not assigned (can't play narration).");
                yield break;
            }

            bool closed = false;

            void OnClosed()
            {
                closed = true;
                dialogueUI.Closed -= OnClosed;
            }

            dialogueUI.Closed += OnClosed;
            dialogueUI.OpenNarration(narration);

            while (!closed)
                yield return null;
        }
    }

    private NarrationSequenceSO FindNarration(string eventId)
    {
        if (narrationEvents == null || string.IsNullOrWhiteSpace(eventId))
            return null;

        string key = eventId.Trim();

        for (int i = 0; i < narrationEvents.Length; i++)
        {
            var e = narrationEvents[i];
            if (e == null || string.IsNullOrWhiteSpace(e.eventId)) continue;

            if (string.Equals(e.eventId.Trim(), key, System.StringComparison.OrdinalIgnoreCase))
                return e.narration;
        }

        return null;
    }
}