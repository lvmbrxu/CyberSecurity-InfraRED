using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    // Scripts in your project reference this statically:
    // SaveManager.OnSaveChanged += ...
    public static event Action OnSaveChanged;

    [SerializeField] private string saveKey = "SAVE_DATA";

    private SaveData data;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ------------------ Ensure data exists ------------------

    private void EnsureData()
    {
        if (data == null)
            data = new SaveData();

        if (data.completedMinigames == null)
            data.completedMinigames = new List<string>();

        if (data.mainEventQueue == null)
            data.mainEventQueue = new List<string>();
    }

    public static void EnsureLoaded()
    {
        if (Instance == null)
        {
            Debug.LogWarning("SaveManager.Instance is null. Ensure SaveManager exists in a boot scene (MainMenu).");
            return;
        }

        if (Instance.data == null)
            Instance.Load();
    }

    // Optional helper if you ever need to force a save from static context
    public static void SaveNow()
    {
        if (Instance == null) return;
        Instance.Save();
    }

    // ------------------ Save/Load ------------------

    public void Save()
    {
        EnsureData();
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();
        OnSaveChanged?.Invoke();
    }

    public void Load()
    {
        if (!PlayerPrefs.HasKey(saveKey))
        {
            data = new SaveData();
            EnsureData();
            OnSaveChanged?.Invoke();
            return;
        }

        string json = PlayerPrefs.GetString(saveKey);
        data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        EnsureData();
        OnSaveChanged?.Invoke();
    }

    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(saveKey);
        data = new SaveData();
        EnsureData();
        PlayerPrefs.Save();
        OnSaveChanged?.Invoke();
    }

    // Editor menu hook compatibility
    public static void ResetSave()
    {
        if (Instance == null)
        {
            Debug.LogWarning("SaveManager.Instance not found. Ensure SaveManager exists in a boot scene.");
            return;
        }
        Instance.ClearSave();
    }

    // ------------------ Minigame progress ------------------

    public static bool IsMinigameCompleted(string minigameId)
    {
        if (Instance == null) return false;
        if (string.IsNullOrWhiteSpace(minigameId)) return false;

        Instance.EnsureData();
        return Instance.data.completedMinigames.Contains(minigameId);
    }

    public static void MarkMinigameCompleted(string minigameId)
    {
        if (Instance == null) return;
        if (string.IsNullOrWhiteSpace(minigameId)) return;

        Instance.EnsureData();

        if (!Instance.data.completedMinigames.Contains(minigameId))
        {
            Instance.data.completedMinigames.Add(minigameId);
            Instance.Save();
        }
    }

    // Backwards-compatible instance methods (some of your older scripts use these)
    public void MarkMinigameComplete(string minigameId) => MarkMinigameCompleted(minigameId);
    public bool IsMinigameComplete(string minigameId) => IsMinigameCompleted(minigameId);

    // ------------------ Main scene routing ------------------

    public static void SetLastMainSpawn(string spawnId)
    {
        if (Instance == null) return;
        Instance.EnsureData();
        Instance.data.lastMainSpawnId = spawnId ?? "";
        Instance.Save();
    }

    public static string GetLastMainSpawn()
    {
        if (Instance == null) return "";
        Instance.EnsureData();
        return Instance.data.lastMainSpawnId ?? "";
    }

    public static void SetSkipMainIntro(bool skip)
    {
        if (Instance == null) return;
        Instance.EnsureData();
        Instance.data.skipMainIntro = skip;
        Instance.Save();
    }

    public static bool GetSkipMainIntro()
    {
        if (Instance == null) return false;
        Instance.EnsureData();
        return Instance.data.skipMainIntro;
    }

    // ------------------ Main event queue ------------------

    private static string NormalizeEventId(string eventId)
    {
        return string.IsNullOrWhiteSpace(eventId) ? null : eventId.Trim();
    }

    public static void EnqueueMainEvent(string eventId)
    {
        if (Instance == null) return;

        string key = NormalizeEventId(eventId);
        if (key == null) return;

        Instance.EnsureData();

        // Prevent duplicates (case-insensitive)
        for (int i = 0; i < Instance.data.mainEventQueue.Count; i++)
        {
            string existing = NormalizeEventId(Instance.data.mainEventQueue[i]);
            if (existing != null && string.Equals(existing, key, StringComparison.OrdinalIgnoreCase))
                return;
        }

        Instance.data.mainEventQueue.Add(key);
        Instance.Save();
    }

    public static bool TryDequeueMainEvent(out string eventId)
    {
        eventId = null;

        if (Instance == null) return false;
        Instance.EnsureData();

        if (Instance.data.mainEventQueue.Count == 0)
            return false;

        // Dequeue
        string raw = Instance.data.mainEventQueue[0];
        Instance.data.mainEventQueue.RemoveAt(0);

        // Normalize output
        eventId = NormalizeEventId(raw);

        Instance.Save();
        return eventId != null;
    }

    public static bool HasMainEvent(string eventId)
    {
        if (Instance == null) return false;

        string key = NormalizeEventId(eventId);
        if (key == null) return false;

        Instance.EnsureData();

        for (int i = 0; i < Instance.data.mainEventQueue.Count; i++)
        {
            string existing = NormalizeEventId(Instance.data.mainEventQueue[i]);
            if (existing != null && string.Equals(existing, key, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static bool RemoveMainEvent(string eventId)
    {
        if (Instance == null) return false;

        string key = NormalizeEventId(eventId);
        if (key == null) return false;

        Instance.EnsureData();

        bool removed = false;

        // Remove all matches (case-insensitive)
        for (int i = Instance.data.mainEventQueue.Count - 1; i >= 0; i--)
        {
            string existing = NormalizeEventId(Instance.data.mainEventQueue[i]);
            if (existing != null && string.Equals(existing, key, StringComparison.OrdinalIgnoreCase))
            {
                Instance.data.mainEventQueue.RemoveAt(i);
                removed = true;
            }
        }

        if (removed) Instance.Save();
        return removed;
    }

    // ------------------ Debug ------------------

    public IReadOnlyList<string> GetCompletedMinigames()
    {
        EnsureData();
        return data.completedMinigames;
    }

    public int GetCompletedCount()
    {
        EnsureData();
        return data.completedMinigames.Count;
    }
}