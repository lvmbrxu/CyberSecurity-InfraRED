#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SaveDebugMenu
{
    [MenuItem("Tools/Save/Reset Save (Delete File)")]
    public static void ResetSave()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ClearSave();
            Debug.Log("Save reset.");
        }
        else
        {
            Debug.LogWarning("SaveManager.Instance not found. Run the game once or ensure SaveManager exists in a boot scene.");
        }
    }
}
#endif