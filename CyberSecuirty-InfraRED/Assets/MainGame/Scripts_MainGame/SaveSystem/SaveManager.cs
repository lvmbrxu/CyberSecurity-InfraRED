using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    private static readonly string FilePath =
        Path.Combine(Application.persistentDataPath, "save.json");

    private static SaveData _data;
    private static HashSet<string> _completedSet;

    public static event Action OnSaveChanged;

    public static SaveData Data
    {
        get
        {
            EnsureLoaded();
            return _data;
        }
    }

    public static void EnsureLoaded()
    {
        if (_data != null) return;

        if (File.Exists(FilePath))
        {
            try
            {
                string json = File.ReadAllText(FilePath);
                _data = JsonUtility.FromJson<SaveData>(json);
            }
            catch
            {
                _data = new SaveData();
            }
        }
        else
        {
            _data = new SaveData();
        }

        _data.completedMinigames ??= new List<string>();
        _data.pendingMainEvents ??= new List<string>();

        _completedSet = new HashSet<string>(_data.completedMinigames);
    }

    public static void Save()
    {
        EnsureLoaded();
        _data.completedMinigames = new List<string>(_completedSet);

        string json = JsonUtility.ToJson(_data, true);
        File.WriteAllText(FilePath, json);

        OnSaveChanged?.Invoke();
    }

    public static void ResetSave()
    {
        _data = new SaveData();
        _completedSet = new HashSet<string>(_data.completedMinigames);

        if (File.Exists(FilePath))
            File.Delete(FilePath);

        OnSaveChanged?.Invoke();
    }

    // ---- Progress API ----

    public static bool IsMinigameCompleted(string id)
    {
        EnsureLoaded();
        return _completedSet.Contains(id);
    }

    public static void MarkMinigameCompleted(string id)
    {
        EnsureLoaded();
        if (_completedSet.Add(id))
            Save();
    }

    public static void SetLastMainSpawn(string spawnId)
    {
        EnsureLoaded();
        if (_data.lastMainSpawnId == spawnId) return;

        _data.lastMainSpawnId = spawnId;
        Save();
    }

    // ---- Intro skip (optional) ----

    public static void SetSkipMainIntro(bool value)
    {
        EnsureLoaded();
        if (_data.skipMainIntro == value) return;

        _data.skipMainIntro = value;
        Save();
    }

    public static bool ShouldSkipMainIntro()
    {
        EnsureLoaded();
        return _data.skipMainIntro;
    }

    // ---- Main event/cutscene queue ----

    public static void EnqueueMainEvent(string eventId)
    {
        EnsureLoaded();
        _data.pendingMainEvents ??= new List<string>();

        // avoid duplicates
        if (_data.pendingMainEvents.Contains(eventId)) return;

        _data.pendingMainEvents.Add(eventId);
        Save();
    }

    public static bool TryDequeueMainEvent(out string eventId)
    {
        EnsureLoaded();
        _data.pendingMainEvents ??= new List<string>();

        if (_data.pendingMainEvents.Count == 0)
        {
            eventId = null;
            return false;
        }

        eventId = _data.pendingMainEvents[0];
        _data.pendingMainEvents.RemoveAt(0);
        Save();
        return true;
    }
}