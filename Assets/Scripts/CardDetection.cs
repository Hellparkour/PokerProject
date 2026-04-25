using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CardDetection : MonoBehaviour
{
    [Header("Configuration UI")]
    public RawImage cameraDisplay;

    [Header("Paramètres de Détection")]
    
    [Range(0f, 1f)] public float threshold = 0.5f;
    [SerializeField] private PokerLogic pokerLogic;

    private WebCamTexture webcamTexture;
    private string targetCameraName = "Live Streamer CAM 313";
    private Dictionary<string, Color[]> referenceData = new Dictionary<string, Color[]>();

    // Taille cible pour la comparaison (celle de la caméra)
    private int camWidth = 1280;
    private int camHeight = 720;

    void Start()
    {
        InitCameraSystem();
    }

    void Update()
    {
        // Touche Espace pour scanner (Nouveau Input System)
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ScanHand();
        }
    }

    private void InitCameraSystem()
    {
        if (cameraDisplay == null) return;

        WebCamDevice[] devices = WebCamTexture.devices;
        foreach (var device in devices)
        {
            if (device.name == targetCameraName)
            {
                // On fixe la résolution souhaitée
                webcamTexture = new WebCamTexture(device.name, camWidth, camHeight, 30);
                cameraDisplay.texture = webcamTexture;
                webcamTexture.Play();

                StartCoroutine(LoadReferencesWhenCameraReady());
                return;
            }
        }
        Debug.LogError("Caméra non trouvée !");
    }

    private System.Collections.IEnumerator LoadReferencesWhenCameraReady()
    {
        // Attendre que la caméra commence vraiment à filmer
        while (webcamTexture.width < 100) yield return null;

        camWidth = webcamTexture.width;
        camHeight = webcamTexture.height;
        LoadReferences();
    }

    private void LoadReferences()
    {
        referenceData.Clear();
        Texture2D[] textures = Resources.LoadAll<Texture2D>("Cards");

        if (textures.Length == 0) Debug.LogError("Dossier Resources/Cards vide !");

        foreach (var tex in textures)
        {
            if (tex.name.StartsWith("AS_"))
            {
                // === CORRECTION 1 : REDIMENSIONNEMENT ===
                // On force la photo de référence à la taille de la caméra
                Texture2D resizedTex = ResizeTexture(tex, camWidth, camHeight);
                referenceData.Add(tex.name, resizedTex.GetPixels());
                Debug.Log($"Référence chargée et redimensionnée ({camWidth}x{camHeight}) : {tex.name}");
            }
        }
    }

    // Fonction utilitaire pour redimensionner une texture
    private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        Graphics.Blit(source, rt);
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(newWidth, newHeight);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();

        RenderTexture.active = currentRT;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    public void ScanHand()
    {
        if (webcamTexture == null || !webcamTexture.isPlaying) return;

        Texture2D shot = new Texture2D(webcamTexture.width, webcamTexture.height);
        shot.SetPixels(webcamTexture.GetPixels());
        shot.Apply();

        string detectedName = IdentifyCard(shot);

        if (!string.IsNullOrEmpty(detectedName))
        {
            Card detectedCard = ParseCardName(detectedName);
            if (detectedCard != null)
            {
                pokerLogic.AddDetectedCardToPlayer(detectedCard);
            }
        }
        else
        {
            Debug.LogWarning("Aucune carte reconnue. Essayez de mieux cadrer la carte.");
        }

        Destroy(shot); 
    }

    private string IdentifyCard(Texture2D shot)
    {
        string bestMatchName = "";
        float highestScore = 0f;
        Color[] shotPixels = shot.GetPixels();

        foreach (var refCard in referenceData)
        {
            float currentScore = CalculateSimilarity(shotPixels, refCard.Value);

            // Debug pour voir les scores en direct (For real j'va explode)
            Debug.Log($"Score pour {refCard.Key} : {currentScore}");

            if (currentScore > highestScore)
            {
                highestScore = currentScore;
                bestMatchName = refCard.Key;
            }
        }

        Debug.Log($"🏆 Meilleur : {bestMatchName} ({highestScore})");
        return (highestScore >= threshold) ? bestMatchName : null;
    }

    private float CalculateSimilarity(Color[] shotPixels, Color[] refPixels)
    {
        float score = 0f;
        int step = 10;
        int totalCount = 0;

        for (int i = 0; i < refPixels.Length; i += step)
        {
            totalCount++;

            // 1. Différence de luminosité (forme)
            float grayDiff = Mathf.Abs(shotPixels[i].grayscale - refPixels[i].grayscale);

            // 2. Différence de couleur (Teinte)
            // On multiplie par 2 pour que si une carte est rouge et l'autre noire, le score chute lourdement
            float colorDiff = (Mathf.Abs(shotPixels[i].r - refPixels[i].r) +
                               Mathf.Abs(shotPixels[i].g - refPixels[i].g) +
                               Mathf.Abs(shotPixels[i].b - refPixels[i].b)) / 3f;

            // Si la différence est forte, on réduit beaucoup le score
            score += (1f - (grayDiff * 0.4f + colorDiff * 0.6f));
        }

        return score / totalCount;
    }

    private Card ParseCardName(string name)
    {
        string[] parts = name.Split('_');
        if (parts.Length < 2) return null;

        Card card = new Card();
        // Valeur
        if (parts[0].ToUpper() == "AS") card.value = CardValue.As;

        switch (parts[1].ToUpper())
        {
            case "PIQUE": card.suit = CardSuit.Pique; break;
            case "COEUR": card.suit = CardSuit.Coeur; break;
            case "TREFLE": card.suit = CardSuit.Trefle; break;
            case "CARREAU": card.suit = CardSuit.Carreau; break;
        }
        return card;
    }
}
