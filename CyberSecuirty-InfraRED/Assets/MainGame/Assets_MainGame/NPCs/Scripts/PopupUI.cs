using UnityEngine;
using UnityEngine.UI;

public class PopupUI : MonoBehaviour
{
    [Header("Auto-assign if left empty")]
    [SerializeField] private Image popupImage;     
    [SerializeField] private Button closeButton;

    [Header("Random Art")]
    [SerializeField] private Sprite[] popupSprites;

    private void Awake()
    {
        // Auto-find common setup
        if (popupImage == null)
            popupImage = GetComponentInChildren<Image>(true);

        if (closeButton == null)
        {
            // Try find a button named something like "Close" or "X" first
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                string n = b.name.ToLower();
                if (n.Contains("close") || n == "x" || n.Contains("exit"))
                {
                    closeButton = b;
                    break;
                }
            }
            // If still null, just take the first button found
            if (closeButton == null && buttons.Length > 0)
                closeButton = buttons[0];
        }

        // Random sprite
        if (popupImage != null && popupSprites != null && popupSprites.Length > 0)
        {
            int idx = Random.Range(0, popupSprites.Length);
            popupImage.sprite = popupSprites[idx];
        }

        // Hook close
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }
        else
        {
            Debug.LogWarning($"PopupUI: No close button found on '{name}'.");
        }
    }

    public void Close()
    {
        Debug.Log("CLOSE CLICKED: " + name);
        Destroy(gameObject);
    }
}