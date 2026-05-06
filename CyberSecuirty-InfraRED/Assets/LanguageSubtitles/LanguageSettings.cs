using System;
using UnityEngine;

namespace Game.Dialogue
{
    public enum GameLanguage
    {
        English = 0,
        Dutch = 1
    }

    [CreateAssetMenu(menuName = "Game/Dialogue/Language Settings")]
    public class LanguageSettings : ScriptableObject
    {
        [SerializeField] private GameLanguage currentLanguage = GameLanguage.English;

        public GameLanguage CurrentLanguage => currentLanguage;

        public event Action<GameLanguage> OnLanguageChanged;

        public void SetLanguage(GameLanguage language)
        {
            if (currentLanguage == language) return;
            currentLanguage = language;
            OnLanguageChanged?.Invoke(currentLanguage);
        }
    }
}