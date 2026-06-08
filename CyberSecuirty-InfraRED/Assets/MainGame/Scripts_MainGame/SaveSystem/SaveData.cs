using System.Collections.Generic;

[System.Serializable]
public sealed class SaveData
{
    public List<string> completedMinigames = new List<string>();
    
    public string lastMainSpawnId = "Default";
    public bool skipMainIntro = false;

    public List<string> mainEventQueue = new List<string>();
}