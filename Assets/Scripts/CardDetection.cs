using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Vuforia;

public class CardDetection : MonoBehaviour
{
    [Header("UI & Scan Region")]
    public RectTransform redSquare;
    public RawImage debugPreview;

    [Header("Réglages Caméra")]
    public float viewX = 0f; public float viewY = 0f;
    public float viewW = 0.3f; public float viewH = 0.3f;

    [SerializeField] private PokerLogic pokerLogic;

    private Dictionary<string, float[]> databaseRefs = new Dictionary<string, float[]>();
    private PixelFormat mPixelFormat = PixelFormat.RGB888;
    private const int SIG_SIZE = 16; // Grille 16x16 (256 points)

    void Start()
    {
        VuforiaApplication.Instance.OnVuforiaStarted += () => {
            VuforiaBehaviour.Instance.CameraDevice.SetFrameFormat(mPixelFormat, true);
        };
        LoadAllSignatures();
    }

    private void LoadAllSignatures()
    {
        databaseRefs.Clear();
        Texture2D[] textures = Resources.LoadAll<Texture2D>("Capture");
        foreach (var tex in textures)
        {
            databaseRefs.Add(tex.name.ToUpper(), GenerateSignature(tex));
        }
        Debug.Log($"<color=green>Base de données : {databaseRefs.Count} cartes chargées.</color>");
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ScanFromCamera();
        }
    }

    public void ScanFromCamera()
    {
        var image = VuforiaBehaviour.Instance.CameraDevice.GetCameraImage(mPixelFormat);
        if (image == null) return;

        Texture2D fullTex = new Texture2D(image.Width, image.Height, TextureFormat.RGB24, false);
        image.CopyToTexture(fullTex);

        Vector3[] corners = new Vector3[4];
        redSquare.GetWorldCorners(corners);

        int xMin = (int)(((corners[0].x / Screen.width) - viewX) / viewW * fullTex.width);
        int yMaxPos = (int)(((corners[2].y / Screen.height) - viewY) / viewH * fullTex.height);
        int width = (int)(((corners[2].x - corners[0].x) / (Screen.width * viewW)) * fullTex.width);
        int height = (int)(((corners[2].y - corners[0].y) / (Screen.height * viewH)) * fullTex.height);
        int startY = fullTex.height - yMaxPos;

        xMin = Mathf.Clamp(xMin, 0, fullTex.width - width);
        startY = Mathf.Clamp(startY, 0, fullTex.height - height);

        Color[] pixels = fullTex.GetPixels(xMin, startY, width, height);
        Color[] flipped = new Color[pixels.Length];

        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                Color c = pixels[j * width + (width - 1 - i)];
                float v = (c.r + c.g + c.b) / 3f;
                v = v > 0.5f ? 1f : v * 0.5f;
                flipped[j * width + i] = new Color(c.r, c.g, c.b, v);
            }
        }

        Texture2D currentCapture = new Texture2D(width, height);
        currentCapture.SetPixels(flipped);
        currentCapture.Apply();

        if (debugPreview != null) debugPreview.texture = currentCapture;

        Identify(GenerateSignature(currentCapture));
        Destroy(fullTex);
    }

    private float[] GenerateSignature(Texture2D tex)
    {
        float[] sig = new float[(SIG_SIZE * SIG_SIZE) + 1];
        float redPixels = 0;

        for (int i = 0; i < SIG_SIZE; i++)
        {
            for (int j = 0; j < SIG_SIZE; j++)
            {
                Color c = tex.GetPixel(i * (tex.width / SIG_SIZE), j * (tex.height / SIG_SIZE));
                sig[i * SIG_SIZE + j] = c.grayscale;

                if (c.r > c.g + 0.2f && c.r > c.b + 0.2f) redPixels++;
            }
        }
        sig[SIG_SIZE * SIG_SIZE] = (redPixels > 10) ? 1.0f : 0.0f;
        return sig;
    }

    private void Identify(float[] currentSig)
    {
        string bestMatch = null;
        float minDiff = float.MaxValue;
        bool currentIsRed = currentSig[SIG_SIZE * SIG_SIZE] > 0.5f;

        foreach (var entry in databaseRefs)
        {
            float diff = 0;
            bool refIsRed = entry.Value[SIG_SIZE * SIG_SIZE] > 0.5f;

            if (currentIsRed != refIsRed) diff += 500.0f;

            for (int i = 0; i < SIG_SIZE * SIG_SIZE; i++)
            {
                int x = i % SIG_SIZE;
                int y = i / SIG_SIZE;
                float weight = 1.0f;
                if (x < 6 && y > 10) weight = 4.0f;

                diff += Mathf.Abs(currentSig[i] - entry.Value[i]) * weight;
            }

            if (diff < minDiff)
            {
                minDiff = diff;
                bestMatch = entry.Key;
            }
        }

        if (minDiff < 100.0f && bestMatch != null)
        {
            ProcessDetection(bestMatch);
        }
    }

    private void ProcessDetection(string cardName)
    {
        try
        {
            string[] parts = cardName.Split('_');
            if (parts.Length == 2)
            {
                // On prépare les strings pour correspondre aux Enums
                string valueStr = parts[0];
                if (valueStr == "10") valueStr = "Dix";

                // Utilisation du paramètre 'true' pour ignorer ROI vs Roi
                CardValue v = (CardValue)System.Enum.Parse(typeof(CardValue), valueStr, true);
                CardSuit s = (CardSuit)System.Enum.Parse(typeof(CardSuit), parts[1], true);

                pokerLogic.AddDetectedCardToPlayer(new Card { value = v, suit = s });
                Debug.Log($"<color=cyan>Carte envoyée : {v} de {s}</color>");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erreur conversion '{cardName}': {e.Message}");
        }
    }
}