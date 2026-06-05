using UnityEngine;

public enum PlatformGlitchMode { Normal, Glitched }
public enum PopupMode { Low, High }

[CreateAssetMenu(menuName = "CyberGame/Game State", fileName = "GameState")]
public class GameStateSO : ScriptableObject
{
    [Header("Chosen outcomes from NPCs")]
    public PlatformGlitchMode platformGlitchMode = PlatformGlitchMode.Normal;
    public PopupMode popupMode = PopupMode.Low;

    public void ResetDefaults()
    {
        platformGlitchMode = PlatformGlitchMode.Normal;
        popupMode = PopupMode.Low;
    }
}