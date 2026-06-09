#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SaveDebugMenu
{
    // Keep these keys in sync with your SaveManager
    private const string SaveKey = "SAVE_DATA";

    // If you also store other keys, add them here
    // (example keys we used earlier in the project)
    private const string PendingEventsKey = "PENDING_MAIN_EVENTS";
    private const string PendingNarrationKey = "PENDING_NARRATION_ID";

    [MenuItem("Tools/Save/Reset Save (Delete PlayerPrefs)")]
    public static void ResetSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.DeleteKey(PendingEventsKey);
        PlayerPrefs.DeleteKey(PendingNarrationKey);

        PlayerPrefs.Save();

        Debug.Log("Save reset (PlayerPrefs keys deleted).");
    }
}
#endif