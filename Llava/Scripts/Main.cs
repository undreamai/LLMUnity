using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    [SerializeField] private RawImage img = default;
    private WebCamTexture webCam;

    public Texture2D GetWebCamTextureAsTexture2D()
    {
        if (webCam == null)
        {
            Debug.LogError("Webcam is not initialized.");
            return null;
        }

        Texture2D texture2D = new Texture2D(webCam.width, webCam.height, TextureFormat.RGB24, false);
        texture2D.SetPixels(webCam.GetPixels());
        texture2D.Apply();
        return texture2D;
    }

    public string GetWebCamTextureAsBase64()
    {
        Texture2D texture2D = GetWebCamTextureAsTexture2D();
        if (texture2D == null)
        {
            Debug.LogError("Failed to retrieve Texture2D from webcam.");
            return null;
        }

        byte[] imageBytes = texture2D.EncodeToJPG(); // Kann auch EncodeToPNG() sein, je nach Bedarf
        return System.Convert.ToBase64String(imageBytes);
    }

    private void Start()
    {
        webCam = new WebCamTexture();
        img.texture = webCam;
        if (!webCam.isPlaying) webCam.Play();
    }
}
