#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SaveDebugMenu
{
    [MenuItem("Tools/Save/Reset Save (Delete File)")]
    public static void ResetSave()
    {
        SaveManager.ResetSave();
        Debug.Log("Save reset.");
    }
}
#endif