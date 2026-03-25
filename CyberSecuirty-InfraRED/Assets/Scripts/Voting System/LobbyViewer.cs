using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class ServerLogItem
{
    public string ts;
    public string type;
    public string message;
}

[Serializable]
public class LogsResponse
{
    public bool ok;
    public ServerLogItem[] logs;
    public string serverTime;
}

public class LobbyLogViewer : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:8080";
    [SerializeField] private string session = "ABCD";
    [SerializeField] private float pollInterval = 0.5f;

    [Header("UI")]
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private bool appendToUnityConsole = true;

    private string lastTimestamp = "";
    private bool startedPolling = false;

    private void Start()
    {
        if (!startedPolling)
        {
            startedPolling = true;
            StartCoroutine(PollLogs());
        }
    }

    private IEnumerator PollLogs()
    {
        while (true)
        {
            string url = $"{baseUrl}/api/logs?session={UnityWebRequest.EscapeURL(session)}";

            if (!string.IsNullOrEmpty(lastTimestamp))
            {
                url += $"&since={UnityWebRequest.EscapeURL(lastTimestamp)}";
            }

            using UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                LogsResponse response = JsonUtility.FromJson<LogsResponse>(json);

                if (response != null && response.ok && response.logs != null)
                {
                    foreach (var log in response.logs)
                    {
                        lastTimestamp = log.ts;
                        string line = $"[{log.ts}] {log.message}\n";

                        if (outputText != null)
                        {
                            outputText.text += line;
                        }

                        if (appendToUnityConsole)
                        {
                            Debug.Log(line);
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("Log polling failed: " + request.error);
            }

            yield return new WaitForSeconds(pollInterval);
        }
    }
}