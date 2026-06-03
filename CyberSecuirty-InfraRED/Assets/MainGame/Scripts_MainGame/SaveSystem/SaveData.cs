using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int version = 1;

    public List<string> completedMinigames = new List<string>();

    public string lastMainSpawnId = "Default";

    // Skip main intro when returning/resuming (optional, but useful)
    public bool skipMainIntro = false;

    // Queue of one-shot events/cutscenes to play in the main scene
    public List<string> pendingMainEvents = new List<string>();
}