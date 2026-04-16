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

        webcamTexture = new WebCamTexture();

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