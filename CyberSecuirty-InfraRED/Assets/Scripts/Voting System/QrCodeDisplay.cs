using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.Common;

public class QRCodeDisplay : MonoBehaviour
{
    [SerializeField] private ServerInfoDisplay serverInfoDisplay;
    [SerializeField] private RawImage targetImage;
    [SerializeField] private int width = 512;
    [SerializeField] private int height = 512;
    [SerializeField] private bool generateOnStart = true;

    private void Start()
    {
        if (generateOnStart)
        {
            Invoke(nameof(GenerateQrFromServerInfo), 1.0f);
        }
    }

    public void GenerateQrFromServerInfo()
    {
        if (serverInfoDisplay == null)
        {
            Debug.LogError("ServerInfoDisplay reference missing.");
            return;
        }

        string text = serverInfoDisplay.CurrentJoinUrl;
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("Join URL not ready yet.");
            return;
        }

        GenerateQr(text);
    }

    public void GenerateQr(string text)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 1
            }
        };

        Color32[] pixels = writer.Write(text);

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();

        if (targetImage != null)
        {
            targetImage.texture = texture;
        }
    }
}