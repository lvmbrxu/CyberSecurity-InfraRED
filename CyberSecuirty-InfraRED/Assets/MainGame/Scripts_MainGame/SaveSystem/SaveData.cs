using System.Collections.Generic;

[System.Serializable]
public sealed class SaveData
{
    public List<string> completedMinigames = new List<string>();

    // Main scene routing / intro
    public string lastMainSpawnId = "";
    public bool skipMainIntro = false;

    // Main events queue (cutscenes/narration triggers)
    public List<string> mainEventQueue = new List<string>();
}