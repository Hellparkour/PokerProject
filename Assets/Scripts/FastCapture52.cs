using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Vuforia;

//Script Vuforia réadapté
public class CardScannerPro : MonoBehaviour
{
    public RectTransform redSquare;
    public RawImage debugPreview;

    [Header("Réglages Viewport (0.3)")]
    public float viewX = 0f;
    public float viewY = 0f;
    public float viewW = 0.3f;
    public float viewH = 0.3f;

    private string[] values = { "AS", "2", "3", "4", "5", "6", "7", "8", "9", "10", "VALET", "DAME", "ROI" };
    private string[] suits = { "PIQUE", "COEUR", "CARREAU", "TREFLE" };
    private int valIndex = 0, suitIndex = 0;
    private bool isFinished = false;

    void Start()
    {
        VuforiaApplication.Instance.OnVuforiaStarted += () => {
            VuforiaBehaviour.Instance.CameraDevice.SetFrameFormat(PixelFormat.RGB888, true);
        };
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame && !isFinished)
            CaptureFromVuforia();
    }

    void CaptureFromVuforia()
    {
        var cameraImage = VuforiaBehaviour.Instance.CameraDevice.GetCameraImage(PixelFormat.RGB888);
        if (cameraImage == null) return;

        Texture2D fullTex = new Texture2D(cameraImage.Width, cameraImage.Height, TextureFormat.RGB24, false);
        cameraImage.CopyToTexture(fullTex);

        // 1. On récupère les coins EXACTS du rectangle rouge à l'écran
        Vector3[] corners = new Vector3[4];
        redSquare.GetWorldCorners(corners);

        // 2. Mapping ultra-précis sur les pixels de la texture Vuforia
        // On normalise (0 à 1) puis on multiplie par la largeur/hauteur réelle de la caméra
        int xMin = (int)(((corners[0].x / Screen.width) - viewX) / viewW * fullTex.width);
        int yMin = (int)(((corners[0].y / Screen.height) - viewY) / viewH * fullTex.height);
        int xMax = (int)(((corners[2].x / Screen.width) - viewX) / viewW * fullTex.width);
        int yMax = (int)(((corners[2].y / Screen.height) - viewY) / viewH * fullTex.height);

        int width = xMax - xMin;
        int height = yMax - yMin;

        // 3. Correction de l'inversion Y (Vuforia lit souvent de haut en bas)
        // On recalcule le point de départ vertical
        int startY = fullTex.height - yMax;

        // Sécurité pour ne pas dépasser
        xMin = Mathf.Clamp(xMin, 0, fullTex.width - width);
        startY = Mathf.Clamp(startY, 0, fullTex.height - height);

        // 4. Extraction et MIROIR (Flip Horizontal)
        Color[] pixels = fullTex.GetPixels(xMin, startY, width, height);
        Color[] flippedPixels = new Color[pixels.Length];
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                flippedPixels[j * width + i] = pixels[j * width + (width - 1 - i)];
            }
        }

        Texture2D snapshot = new Texture2D(width, height);
        snapshot.SetPixels(flippedPixels);
        snapshot.Apply();

        // 5. Sauvegarde
        SaveTexture(snapshot, values[valIndex] + "_" + suits[suitIndex]);
        if (debugPreview != null) debugPreview.texture = snapshot;

        Destroy(fullTex);
        PrepareNext();
    }

    void SaveTexture(Texture2D tex, string fileName)
    {
        byte[] bytes = tex.EncodeToPNG();
        string dirPath = Application.dataPath + "/Resources/Capture/";
        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
        File.WriteAllBytes(dirPath + fileName + ".png", bytes);
        Debug.Log($"<color=green>OK: {fileName}.png</color>");
    }

    void PrepareNext()
    {
        valIndex++;
        if (valIndex >= values.Length) { valIndex = 0; suitIndex++; }
        if (suitIndex >= suits.Length) isFinished = true;
    }
}