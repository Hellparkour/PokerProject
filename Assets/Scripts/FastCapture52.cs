using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Vuforia; // On ajoute Vuforia

public class CardScannerPro : MonoBehaviour
{
    [Header("Configuration UI")]
    public RectTransform redSquare; // Glisse ton carré rouge (64x64) ici
    public RawImage previewDisplay; // Pour voir ce qu'on capture (optionnel)

    // On garde ton système d'index pour les 52 cartes
    private string[] values = { "AS", "2", "3", "4", "5", "6", "7", "8", "9", "10", "VALET", "DAME", "ROI" };
    private string[] suits = { "PIQUE", "COEUR", "CARREAU", "TREFLE" };

    private int valIndex = 0;
    private int suitIndex = 0;
    private bool isFinished = false;

    // Format requis pour Vuforia
    private PixelFormat mPixelFormat = PixelFormat.RGB888;

    void Start()
    {
        // On demande à Vuforia d'activer le format de lecture de pixels
        VuforiaApplication.Instance.OnVuforiaStarted += () => {
            VuforiaBehaviour.Instance.CameraDevice.SetFrameFormat(mPixelFormat, true);
        };

        Debug.Log($"<color=cyan>DÉMARRAGE : Aligne l'<b>{values[valIndex]} de {suits[suitIndex]}</b> dans le carré rouge et appuie sur K</color>");
    }

    void Update()
    {
        if (Keyboard.current.kKey.wasPressedThisFrame && !isFinished)
        {
            CaptureFromVuforia();
        }
    }

    void CaptureFromVuforia()
    {
        // 1. Récupérer l'image de Vuforia
        Vuforia.Image cameraImage = VuforiaBehaviour.Instance.CameraDevice.GetCameraImage(mPixelFormat);

        if (cameraImage == null)
        {
            Debug.LogError("Pas d'image caméra disponible via Vuforia.");
            return;
        }

        // 2. Créer une texture complète temporaire
        Texture2D fullTex = new Texture2D(cameraImage.Width, cameraImage.Height, TextureFormat.RGB24, false);
        cameraImage.CopyToTexture(fullTex);

        // 3. Calculer la position du carré rouge
        // On convertit la position UI en coordonnées Texture
        float ratioX = (float)fullTex.width / Screen.width;
        float ratioY = (float)fullTex.height / Screen.height;

        int centerX = (int)(redSquare.position.x * ratioX);
        int centerY = (int)(redSquare.position.y * ratioY);

        // Zone 64x64
        int startX = Mathf.Clamp(centerX - 32, 0, fullTex.width - 64);
        int startY = Mathf.Clamp(centerY - 32, 0, fullTex.height - 64);

        // 4. Découper (Crop)
        Texture2D snapshot = new Texture2D(64, 64);
        Color[] pixels = fullTex.GetPixels(startX, startY, 64, 64);
        snapshot.SetPixels(pixels);
        snapshot.Apply();

        // 5. Sauvegarder
        string currentName = values[valIndex] + "_" + suits[suitIndex];
        SaveTexture(snapshot, currentName);

        // Nettoyage mémoire
        Destroy(fullTex);

        PrepareNext();
    }

    void SaveTexture(Texture2D tex, string fileName)
    {
        byte[] bytes = tex.EncodeToPNG();
        // Sauvegarde dans Resources/Capture pour pouvoir les utiliser plus tard
        string dirPath = Application.dataPath + "/Resources/Capture/";

        if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);

        File.WriteAllBytes(dirPath + fileName + ".png", bytes);
        Debug.Log($"<color=green>PHOTO PRISE : {fileName}.png (64x64)</color>");
    }

    void PrepareNext()
    {
        valIndex++;
        if (valIndex >= values.Length)
        {
            valIndex = 0;
            suitIndex++;
        }

        if (suitIndex >= suits.Length)
        {
            isFinished = true;
            Debug.Log("<color=white><b>BRAVO ! Tes 52 références sont prêtes.</b></color>");
        }
        else
        {
            Debug.Log($"<color=yellow>SUIVANT : Aligne l'<b>{values[valIndex]} de {suits[suitIndex]}</b></color>");
        }
    }
}