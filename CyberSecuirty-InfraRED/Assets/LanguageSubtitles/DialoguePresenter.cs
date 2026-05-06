using TMPro;
using UnityEngine;

namespace Game.Dialogue
{
    public class DialoguePresenter : MonoBehaviour
    {
        [Header("Drag your existing UI references here")]
        [SerializeField] private GameObject rootPanel;   // your dialogue panel object
        [SerializeField] private TMP_Text subtitleText;  // required
        [SerializeField] private TMP_Text speakerText;   // optional

        public void Show()
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            else gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
            else gameObject.SetActive(false);
        }

        public void SetLine(string subtitle, string speaker)
        {
            if (subtitleText != null) subtitleText.text = subtitle ?? "";
            if (speakerText != null) speakerText.text = speaker ?? "";
        }

        public void Clear()
        {
            SetLine("", "");
        }
    }
}