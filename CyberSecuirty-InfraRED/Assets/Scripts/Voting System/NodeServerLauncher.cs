using System.Diagnostics;
using System.IO;
using UnityEngine;

public class NodeServerLauncher : MonoBehaviour
{
    [Header("Node Server")]
    [SerializeField] private string nodeExe = "node";
    [SerializeField] private string serverJsPath = @"C:\YourFolder\server.js";
    [SerializeField] private bool startOnPlay = true;
    [SerializeField] private bool killOnQuit = true;

    private Process process;

    private void Start()
    {
        if (startOnPlay)
        {
            StartServer();
        }
    }

    public void StartServer()
    {
        if (process != null && !process.HasExited)
        {
            UnityEngine.Debug.Log("Node server already running.");
            return;
        }

        if (!File.Exists(serverJsPath))
        {
            UnityEngine.Debug.LogError("server.js not found: " + serverJsPath);
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = nodeExe,
            Arguments = $"\"{serverJsPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(serverJsPath)
        };

        process = new Process();
        process.StartInfo = psi;
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                UnityEngine.Debug.Log("[NODE] " + e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                UnityEngine.Debug.LogError("[NODE ERR] " + e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        UnityEngine.Debug.Log("Started Node server.");
    }

    public void StopServer()
    {
        if (process == null) return;

        if (!process.HasExited)
        {
            process.Kill();
            UnityEngine.Debug.Log("Stopped Node server.");
        }
    }

    private void OnApplicationQuit()
    {
        if (killOnQuit)
        {
            StopServer();
        }
    }
}