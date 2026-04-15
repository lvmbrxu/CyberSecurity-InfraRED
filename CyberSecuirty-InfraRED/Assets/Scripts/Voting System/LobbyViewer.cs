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

[Serializable]
public class SessionState
{
    public string phase;
    public int currentRound;
    public bool started;
    public string votingEndsAt;
    public string resultsEndsAt;
    public string winner;
    public float winnerPercent;
    public bool wasTie;
}

[Serializable]
public class StateResponse
{
    public bool ok;
    public SessionState state;
    public bool isAdmin;
    public int playerCount;
    public int readyCount;
    public int votingSeconds;
    public int resultsSeconds;
}

public class LobbyViewer : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:8080";
    [SerializeField] private string session = "ABCD";
    [SerializeField] private float logPollInterval = 0.5f;
    [SerializeField] private float statePollInterval = 1f;

    [Header("UI")]
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private bool appendToUnityConsole = true;

    private string lastTimestamp = "";
    private string lastPhase = "";
    private int lastRound = -1;
    private DateTime? votingEndsAtUtc = null;
    private DateTime? resultsEndsAtUtc = null;

    private void Start()
    {
        StartCoroutine(PollLogs());
        StartCoroutine(PollState());
        StartCoroutine(PrintCountdown());
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
                        string line = $"[{log.ts}] {log.message}";
                        WriteLine(line);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Log polling failed: " + request.error);
            }

            yield return new WaitForSeconds(logPollInterval);
        }
    }

    private IEnumerator PollState()
    {
        while (true)
        {
            string url = $"{baseUrl}/api/state?session={UnityWebRequest.EscapeURL(session)}";
            using UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                StateResponse response = JsonUtility.FromJson<StateResponse>(json);

                if (response != null && response.ok && response.state != null)
                {
                    if (response.state.phase != lastPhase || response.state.currentRound != lastRound)
                    {
                        WriteLine($"[STATE] Round {response.state.currentRound} | Phase: {response.state.phase}");
                        lastPhase = response.state.phase;
                        lastRound = response.state.currentRound;
                    }

                    votingEndsAtUtc = ParseIsoUtc(response.state.votingEndsAt);
                    resultsEndsAtUtc = ParseIsoUtc(response.state.resultsEndsAt);

                    if (response.state.phase == "results" && !string.IsNullOrEmpty(response.state.winner))
                    {
                        string winnerLine = response.state.wasTie
                            ? $"[RESULT] Winner: {response.state.winner} (tie -> random winner)"
                            : $"[RESULT] Winner: {response.state.winner} ({response.state.winnerPercent:0.##}%)";
                        WriteLine(winnerLine);
                    }
                }
            }

            yield return new WaitForSeconds(statePollInterval);
        }
    }

    private IEnumerator PrintCountdown()
    {
        while (true)
        {
            if (lastPhase == "voting" && votingEndsAtUtc.HasValue)
            {
                double secondsLeft = Math.Ceiling((votingEndsAtUtc.Value - DateTime.UtcNow).TotalSeconds);
                if (secondsLeft >= 0)
                {
                    WriteLine($"[TIMER] Vote ends in {secondsLeft:0}s");
                }
            }
            else if (lastPhase == "results" && resultsEndsAtUtc.HasValue)
            {
                double secondsLeft = Math.Ceiling((resultsEndsAtUtc.Value - DateTime.UtcNow).TotalSeconds);
                if (secondsLeft >= 0)
                {
                    WriteLine($"[TIMER] Next round in {secondsLeft:0}s");
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private DateTime? ParseIsoUtc(string iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return null;

        if (DateTime.TryParse(
            iso,
            null,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
            out DateTime parsed))
        {
            return parsed.ToUniversalTime();
        }

        return null;
    }

    private void WriteLine(string line)
    {
        if (outputText != null)
        {
            outputText.text += line + "\n";
        }

        if (appendToUnityConsole)
        {
            Debug.Log(line);
        }
    }
}