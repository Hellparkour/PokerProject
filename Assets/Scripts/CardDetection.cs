using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.IO;
using Vuforia; // Obligatoire pour accéder au moteur

public class CardDetection : MonoBehaviour
{
    [Header("UI & Affichage")]
    public RectTransform valueRegion;
    public RectTransform suitRegion;

    [Header("Liaison")]
    [SerializeField] private PokerLogic pokerLogic;

    private Dictionary<string, float[]> valueRefs = new Dictionary<string, float[]>();
    private Dictionary<string, float[]> suitRefs = new Dictionary<string, float[]>();

    // Format d'image demandé à Vuforia
    private PixelFormat mPixelFormat = PixelFormat.RGB888;

    void Start()
    {
        // On s'abonne à l'événement de démarrage de Vuforia
        VuforiaApplication.Instance.OnVuforiaStarted += OnVuforiaStarted;
        LoadAllSignatures();
    }

    private void OnVuforiaStarted()
    {
        // On demande à Vuforia d'activer le format RGB888 pour qu'on puisse lire les pixels
        bool success = VuforiaBehaviour.Instance.CameraDevice.SetFrameFormat(mPixelFormat, true);
        if (!success) Debug.LogError("Échec de l'activation du format RGB888 sur Vuforia.");
    }

    private void LoadAllSignatures()
    {
        valueRefs.Clear();
        suitRefs.Clear();
        Texture2D[] textures = Resources.LoadAll<Texture2D>("References");

        foreach (var tex in textures)
        {
            float[] sig = GenerateSignature(tex);
            if (tex.name.StartsWith("V_")) valueRefs.Add(tex.name.Replace("V_", ""), sig);
            else if (tex.name.StartsWith("S_")) suitRefs.Add(tex.name.Replace("S_", ""), sig);
        }
    }

    public void ScanFullCard()
    {
        // On récupère l'image actuelle de Vuforia
        Vuforia.Image image = VuforiaBehaviour.Instance.CameraDevice.GetCameraImage(mPixelFormat);

        if (image == null)
        {
            Debug.LogWarning("Vuforia n'a pas encore d'image disponible.");
            return;
        }

        // On crée une texture à partir des pixels de Vuforia
        Texture2D cameraTexture = new Texture2D(image.Width, image.Height, TextureFormat.RGB24, false);
        image.CopyToTexture(cameraTexture);

        // Identification (on passe la texture générée à tes fonctions)
        float[] currentValSig = GetSignatureFromTexture(cameraTexture, valueRegion);
        float[] currentSuitSig = GetSignatureFromTexture(cameraTexture, suitRegion);

        string val = Identify(currentValSig, valueRefs);
        string suit = Identify(currentSuitSig, suitRefs);

        if (val != null && suit != null)
        {
            Debug.Log($"<color=green>DÉTECTÉ VIA VUFORIA : {val} de {suit}</color>");
            SendToPoker(val, suit);
        }

        // Nettoyage mémoire
        Destroy(cameraTexture);
    }

    private float[] GetSignatureFromTexture(Texture2D source, RectTransform region)
    {
        // Ici, on extrait une zone de la texture caméra basée sur ton UI
        // Note : Il faudra peut-être ajuster le mapping UI -> Pixels selon la résolution
        int x = source.width / 2; // Exemple simplifié au centre
        int y = source.height / 2;

        float[] sig = new float[16];
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                sig[i * 4 + j] = source.GetPixel(x + i * 10, y + j * 10).grayscale;

        return sig;
    }

    private string Identify(float[] currentSig, Dictionary<string, float[]> database)
    {
        string best = null;
        float minDiff = float.MaxValue;

        foreach (var entry in database)
        {
            float diff = 0;
            for (int i = 0; i < 16; i++) diff += Mathf.Abs(currentSig[i] - entry.Value[i]);
            if (diff < minDiff) { minDiff = diff; best = entry.Key; }
        }
        return (minDiff < 1.5f) ? best : null;
    }

    private float[] GenerateSignature(Texture2D tex)
    {
        float[] sig = new float[16];
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                sig[i * 4 + j] = tex.GetPixel(i * (tex.width / 4), j * (tex.height / 4)).grayscale;
        return sig;
    }

    private void SendToPoker(string v, string s)
    {
        // Ton code PokerLogic ici...
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame) ScanFullCard();
    }

    void OnOnDestroy()
    {
        // Désactiver le format pour libérer de la mémoire
        VuforiaBehaviour.Instance.CameraDevice.SetFrameFormat(mPixelFormat, false);
    }
}