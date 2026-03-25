using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class ServerInfoResponse
{
    public bool ok;
    public string ip;
    public int port;
    public string baseUrl;
}

public class ServerInfoDisplay : MonoBehaviour
{
    [SerializeField] private string localBaseUrl = "http://127.0.0.1:8080";
    [SerializeField] private string session = "ABCD";

    [Header("UI")]
    [SerializeField] private TMP_Text baseUrlText;
    [SerializeField] private TMP_Text joinUrlText;

    public string CurrentJoinUrl { get; private set; }

    private void Start()
    {
        StartCoroutine(FetchServerInfo());
    }

    private IEnumerator FetchServerInfo()
    {
        using UnityWebRequest request = UnityWebRequest.Get(localBaseUrl + "/api/info");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to get /api/info: " + request.error);
            yield break;
        }

        string json = request.downloadHandler.text;
        ServerInfoResponse info = JsonUtility.FromJson<ServerInfoResponse>(json);

        if (info == null || !info.ok)
        {
            Debug.LogError("Invalid /api/info response.");
            yield break;
        }

        CurrentJoinUrl = $"{info.baseUrl}/?session={session}";

        if (baseUrlText != null)
            baseUrlText.text = "Server: " + info.baseUrl;

        if (joinUrlText != null)
            joinUrlText.text = "Join URL: " + CurrentJoinUrl;

        Debug.Log("Join URL: " + CurrentJoinUrl);
    }
}