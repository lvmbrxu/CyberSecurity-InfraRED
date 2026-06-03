using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MainEventRunnerSimple : MonoBehaviour
{
    [Serializable]
    public class EventEntry
    {
        public string eventId;

        [Header("Camera (optional)")]
        public string cameraId;
        public float minHoldTime = 0f; // keep 0 if dialogue handles timing

        [Header("Actions (optional)")]
        public UnityEvent onStart; // hook your dialogue start here
        public UnityEvent onEnd;   // optional cleanup
    }

    [Header("Dependencies")]
    [SerializeField] private CameraRig cameraRig;

    [Header("Events mapping")]
    [SerializeField] private List<EventEntry> events = new();

    [Header("Optional: disable input while event plays")]
    [SerializeField] private GameObject playerInputRoot;

    // If your dialogue system can tell us when it's finished, set this true/false from it.
    public bool IsDialoguePlaying { get; private set; }

    private Dictionary<string, EventEntry> _map;

    private void Awake()
    {
        _map = new Dictionary<string, EventEntry>();
        foreach (var e in events)
        {
            if (!string.IsNullOrWhiteSpace(e.eventId))
                _map[e.eventId] = e;
        }
    }

    private void Start()
    {
        StartCoroutine(RunQueue());
    }

    public void NotifyDialogueStarted() => IsDialoguePlaying = true;
    public void NotifyDialogueEnded() => IsDialoguePlaying = false;

    private IEnumerator RunQueue()
    {
        while (SaveManager.TryDequeueMainEvent(out var eventId))
        {
            if (!_map.TryGetValue(eventId, out var entry))
            {
                Debug.LogWarning($"No EventEntry mapped for eventId: {eventId}");
                continue;
            }

            if (playerInputRoot != null) playerInputRoot.SetActive(false);

            if (cameraRig != null)
                cameraRig.SwitchTo(entry.cameraId);

            // Run your existing dialogue / cutscene triggers
            entry.onStart?.Invoke();

            // Wait: either your dialogue toggles IsDialoguePlaying,
            // or you can just set minHoldTime and ignore dialogue waiting.
            float t = 0f;

            // If you want “super simple”: comment the dialogue wait loop and only use timer.
            while (IsDialoguePlaying)
                yield return null;

            while (t < entry.minHoldTime)
            {
                t += Time.deltaTime;
                yield return null;
            }

            entry.onEnd?.Invoke();

            if (cameraRig != null)
                cameraRig.ReturnToGameplay();

            if (playerInputRoot != null) playerInputRoot.SetActive(true);
        }
    }
}