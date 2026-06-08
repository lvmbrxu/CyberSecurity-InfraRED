using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    // COMPAT: scripts reference this statically: SaveManager.OnSaveChanged += ...
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

    // ---------- Static API expected by your MainGame scripts ----------

    public static void EnsureLoaded()
    {
        if (Instance == null)
        {
            Debug.LogWarning("SaveManager.Instance is null. Ensure SaveManager exists in the boot scene.");
            return;
        }

        if (Instance.data == null)
            Instance.Load();
    }

    public static bool IsMinigameCompleted(string minigameId)
    {
        if (Instance == null) return false;
        Instance.EnsureData();
        return Instance.data.completedMinigames.Contains(minigameId);
    }

    public static void MarkMinigameCompleted(string minigameId)
    {
        if (Instance == null) return;
        Instance.EnsureData();

        if (!Instance.data.completedMinigames.Contains(minigameId))
        {
            Instance.data.completedMinigames.Add(minigameId);
            Instance.Save();
        }
    }

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

    public static void EnqueueMainEvent(string eventId)
    {
        if (Instance == null) return;
        if (string.IsNullOrEmpty(eventId)) return;

        Instance.EnsureData();
        Instance.data.mainEventQueue.Add(eventId);
        Instance.Save();
    }

    public static bool TryDequeueMainEvent(out string eventId)
    {
        eventId = null;

        if (Instance == null) return false;
        Instance.EnsureData();

        var q = Instance.data.mainEventQueue;
        if (q == null || q.Count == 0) return false;

        eventId = q[0];
        q.RemoveAt(0);
        Instance.Save();
        return true;
    }

    // ---------- Backwards-compatible instance API (your older minigame scripts) ----------

    public void MarkMinigameComplete(string minigameId) => MarkMinigameCompleted(minigameId);
    public bool IsMinigameComplete(string minigameId) => IsMinigameCompleted(minigameId);

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
            OnSaveChanged?.Invoke();
            return;
        }

        string json = PlayerPrefs.GetString(saveKey);
        data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        OnSaveChanged?.Invoke();
    }

    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(saveKey);
        data = new SaveData();
        PlayerPrefs.Save();
        OnSaveChanged?.Invoke();
    }

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

    private void EnsureData()
    {
        if (data == null)
            data = new SaveData();

        if (data.completedMinigames == null)
            data.completedMinigames = new List<string>();

        if (data.mainEventQueue == null)
            data.mainEventQueue = new List<string>();
    }
}