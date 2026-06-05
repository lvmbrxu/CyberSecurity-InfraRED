using UnityEngine;

public class PopupDifficultyApplier : MonoBehaviour
{
    public GameStateSO gameState;
    public PopupSpawner spawner;

    [Header("Low (reject cookies)")]
    public float lowInterval = 1.5f;
    public int lowMax = 4;

    [Header("High (accept cookies)")]
    public float highInterval = 0.6f;
    public int highMax = 12;

    private void Start()
    {
        if (spawner == null) return;

        bool high = gameState.popupMode == PopupMode.High;

        spawner.spawnIntervalSeconds = high ? highInterval : lowInterval;
        spawner.maxPopupsOnScreen = high ? highMax : lowMax;
    }
}