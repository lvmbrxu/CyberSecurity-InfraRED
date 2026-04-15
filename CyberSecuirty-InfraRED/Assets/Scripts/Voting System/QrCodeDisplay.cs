using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ServerQrImageLoader : MonoBehaviour
{
    [SerializeField] private string localBaseUrl = "http://127.0.0.1:8080";
    [SerializeField] private string session = "ABCD";
    [SerializeField] private RawImage targetImage;
    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private float delayBeforeLoad = 1.0f;

    private void Start()
    {
        if (loadOnStart)
        {
            StartCoroutine(DelayedLoad());
        }
    }

    private IEnumerator DelayedLoad()
    {
        yield return new WaitForSeconds(delayBeforeLoad);
        yield return LoadQr();
    }

    public IEnumerator LoadQr()
    {
        string url = $"{localBaseUrl}/api/qr?session={UnityWebRequest.EscapeURL(session)}";

        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to load QR: " + request.error);
            yield break;
        }

        Texture texture = DownloadHandlerTexture.GetContent(request);

        if (targetImage != null)
        {
            targetImage.texture = texture;
            targetImage.SetNativeSize();
        }

        Debug.Log("QR loaded from server.");
    }
}