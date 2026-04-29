using UnityEngine;

// Suppression des accents pour éviter les bugs d'encodage
public enum CardSuit
{
    Pique,
    Coeur,
    Carreau,
    Trefle
}

public enum CardValue
{
    // On peut mapper les noms aux chiffres directement
    Deux = 2,
    Trois = 3,
    Quatre = 4,
    Cinq = 5,
    Six = 6,
    Sept = 7,
    Huit = 8,
    Neuf = 9,
    Dix = 10,  
    D10 = 10,  
    Valet = 11,
    Reine = 12,
    Roi = 13,
    As = 14
}

[System.Serializable]
public class Card
{
    public CardSuit suit;
    public CardValue value;
}