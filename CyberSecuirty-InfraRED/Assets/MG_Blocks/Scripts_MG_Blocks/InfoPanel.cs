using UnityEngine;

public class InfoPanel : MonoBehaviour
{
    [SerializeField] GameObject infoPanel;
    [SerializeField] private GameObject CluesUI;
    void Start()
    {
        Time.timeScale = 0;
        
    }

    public void StartGame()
    {
        Time.timeScale = 1;
        infoPanel.SetActive(false);
        CluesUI.SetActive(true);
    }
}