// GameManager.cs (add this method + field)
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    public Transform player;
    public Camera cam;

    public GameObject deathPanel;
    public GameObject winPanel;
    public Text debugText;

    public float killBelowScreen = 2.5f;
    public string levelId = "Level_1";

    Transform finish;
    bool ended;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;

        Time.timeScale = 1f;
        if (deathPanel) deathPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);
        if (debugText) debugText.text = "";
    }

    public void SetFinish(Transform finishTransform) => finish = finishTransform;

    void Update()
    {
        if (ended) return;
        if (!player || !cam) return;

        float bottomY = cam.transform.position.y - cam.orthographicSize;
        if (player.position.y < bottomY - killBelowScreen)
            Die();
    }

    public void Die()
    {
        if (ended) return;
        ended = true;
        Time.timeScale = 0f;
        if (deathPanel) deathPanel.SetActive(true);
    }

    public void Win()
    {
        if (ended) return;
        ended = true;

        PlayerPrefs.SetInt(levelId + "_DONE", 1);
        PlayerPrefs.Save();

        Time.timeScale = 0f;
        if (winPanel) winPanel.SetActive(true);
        SetDebug("introduce password");
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Continue()
    {
        Time.timeScale = 1f;
        if (winPanel) winPanel.SetActive(false);
        ended = false;
    }

    public void SetDebug(string msg)
    {
        if (debugText) debugText.text = msg;
        Debug.Log(msg);
    }
}