using UnityEngine;
using System.Collections.Generic;

public class CardVisualUpdater : MonoBehaviour
{
    public MeshRenderer faceRenderer;

    private static readonly Dictionary<CardSuit, string> SuitToFileName = new Dictionary<CardSuit, string>
    {
        { CardSuit.Pique, "SPADE" },
        { CardSuit.Coeur, "HEART" },
        { CardSuit.Carreau, "DIAMOND" },
        { CardSuit.Trefle, "CLUB" }
    };

    private static readonly Dictionary<CardValue, string> ValueToFileName = new Dictionary<CardValue, string>
    {
        { CardValue.Deux, "2" }, { CardValue.Trois, "3" }, { CardValue.Quatre, "4" },
        { CardValue.Cinq, "5" }, { CardValue.Six, "6" }, { CardValue.Sept, "7" },
        { CardValue.Huit, "8" }, { CardValue.Neuf, "9" }, { CardValue.Dix, "10" },
        { CardValue.Valet, "J" }, { CardValue.Reine, "Q" }, { CardValue.Roi, "K" }, { CardValue.As, "AS" }
    };

    public void SetCardVisual(Card card)
    {
        if (faceRenderer == null) return;

        string fileName = $"{SuitToFileName[card.suit]}_V_{ValueToFileName[card.value]}";
        Texture2D tex = Resources.Load<Texture2D>("Cards/" + fileName);

        if (tex != null)
        {
            faceRenderer.material.mainTexture = tex;

            // Correction de l'effet miroir (Inverse l'horizontale)
            faceRenderer.material.mainTextureScale = new Vector2(-1, 1);
            faceRenderer.material.mainTextureOffset = new Vector2(1, 0);
        }
        else
        {
            Debug.LogError($"Texture manquante : Resources/Cards/{fileName}");
        }
    }
}