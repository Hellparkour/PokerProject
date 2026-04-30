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
    private Dictionary<string, float[]> numberRefs = new Dictionary<string, float[]>();

    private PixelFormat mPixelFormat = PixelFormat.RGB888;
    private const int SIG_SIZE = 16;

    void Start()
    {
        if (VuforiaApplication.Instance != null)
        {
            VuforiaApplication.Instance.OnVuforiaStarted += () => {
                VuforiaBehaviour.Instance.CameraDevice.SetFrameFormat(mPixelFormat, true);
            };
        }
        LoadAllSignatures();
    }

    private void LoadAllSignatures()
    {
        databaseRefs.Clear();
        numberRefs.Clear();

        // 1. Charger les cartes entières (Dossier Capture)
        Texture2D[] textures = Resources.LoadAll<Texture2D>("Capture");
        foreach (var tex in textures) databaseRefs.Add(tex.name.ToUpper(), GenerateSignature(tex));

        // 2. Charger les index prioritaires (Dossier Numeros : 2_R, 2_N, 5_N...)
        Texture2D[] nums = Resources.LoadAll<Texture2D>("Numeros");
        foreach (var n in nums) numberRefs.Add(n.name.ToUpper(), GenerateSignature(n));

        Debug.Log($"<color=green>Base prête : {databaseRefs.Count} Cartes | {numberRefs.Count} Index.</color>");
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

        // Traitement de l'image (Flip + Contraste)
        Color[] processed = new Color[pixels.Length];
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                Color c = pixels[j * width + (width - 1 - i)];
                float v = (c.r + c.g + c.b) / 3f;
                v = v > 0.5f ? 1f : v * 0.5f;
                processed[j * width + i] = new Color(c.r, c.g, c.b, v);
            }
        }

        Texture2D currentCapture = new Texture2D(width, height);
        currentCapture.SetPixels(processed);
        currentCapture.Apply();

        if (debugPreview != null) debugPreview.texture = currentCapture;

        // Extraction précise de l'index (Coin supérieur gauche)
        int numW = width / 4;
        int numH = height / 4;
        Color[] numPix = currentCapture.GetPixels(0, height - numH, numW, numH);
        Texture2D numTex = new Texture2D(numW, numH);
        numTex.SetPixels(numPix);
        numTex.Apply();

        IdentifyDual(GenerateSignature(currentCapture), GenerateSignature(numTex));

        Destroy(fullTex);
        Destroy(numTex);
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
        sig[SIG_SIZE * SIG_SIZE] = (redPixels > 8) ? 1.0f : 0.0f;
        return sig;
    }

    private void IdentifyDual(float[] globalSig, float[] numSig)
    {
        string bestNumMatch = null;
        float minNumDiff = float.MaxValue;

        // PASSE 1 : Identifier le numéro (2, 3, 4, 5...)
        foreach (var entry in numberRefs)
        {
            float diff = 0;
            for (int i = 0; i < SIG_SIZE * SIG_SIZE; i++)
            {
                int y = i / SIG_SIZE;
                float weight = (y > 10) ? 3.0f : 1.0f;
                diff += Mathf.Abs(numSig[i] - entry.Value[i]) * weight;
            }

            if (diff < minNumDiff)
            {
                minNumDiff = diff;
                bestNumMatch = entry.Key;
            }
        }

        if (bestNumMatch == null || minNumDiff > 200f) return;

        string[] nParts = bestNumMatch.Split('_');
        string targetValue = nParts[0]; 
        bool targetIsRed = nParts[1] == "R";

        // PASSE 2 : On cherche la carte globale MAIS on filtre strictement
        string bestCard = null;
        float minGlobalDiff = float.MaxValue;

        foreach (var entry in databaseRefs)
        {
            string[] cParts = entry.Key.Split('_');
            bool refIsRed = entry.Value[SIG_SIZE * SIG_SIZE] > 0.5f;

            if (cParts[0] != targetValue || refIsRed != targetIsRed) continue;

            float diff = 0;
            for (int i = 0; i < SIG_SIZE * SIG_SIZE; i++)
            {
                diff += Mathf.Abs(globalSig[i] - entry.Value[i]);
            }

            if (diff < minGlobalDiff)
            {
                minGlobalDiff = diff;
                bestCard = entry.Key;
            }
        }

        if (bestCard != null)
        {
            Debug.Log($"<color=cyan>Index : {bestNumMatch} (Confiance Index: {minNumDiff:F0}) -> Carte : {bestCard}</color>");
            ProcessDetection(bestCard);
        }
    }

    private void ProcessDetection(string cardName)
    {
        try
        {
            string[] parts = cardName.Split('_');
            if (parts.Length == 2)
            {
                string vStr = parts[0] == "10" ? "Dix" : parts[0];
                CardValue v = (CardValue)System.Enum.Parse(typeof(CardValue), vStr, true);
                CardSuit s = (CardSuit)System.Enum.Parse(typeof(CardSuit), parts[1], true);
                pokerLogic.AddDetectedCardToPlayer(new Card { value = v, suit = s });
            }
        }
        catch { }
    }
}