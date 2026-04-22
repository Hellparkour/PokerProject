using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CardDetection : MonoBehaviour
{
    public RawImage cameraDisplay;
    private WebCamTexture webcamTexture;

    // Soustrait les cartes que jai en mains.
    [SerializeField] private PokerLogic pokerLogic;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.LogError("Aucune caméra détectée !");
            return;
        }

        string targetCameraName = "Live Streamer CAM 313";
        bool found = false;

        foreach (var device in devices)
        {
            Debug.Log("Caméra détectée : " + device.name);

            if (device.name == targetCameraName)
            {
                webcamTexture = new WebCamTexture(device.name, 1280, 720, 30);
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogError("Caméra cible non trouvée !");
            return;
        }

        if (cameraDisplay != null)
        {
            cameraDisplay.texture = webcamTexture;
        }

        webcamTexture.Play();
    }

    //TODO: Création des phases, probablement un ENUM
    public void ScanHand()
    {
        Color[] pixels = webcamTexture.GetPixels();

        // TODO: La logique de lecture et comparaison.
        Debug.Log("Lecture des cartes via la caméra...");
    }
}