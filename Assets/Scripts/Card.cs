using UnityEngine;

//Code provenant du BlackJack

public enum CardSuit { Pique, Coeur, Carreau, Trefle }
public enum CardValue { Deux = 2, Trois, Quatre, Cinq, Six, Sept, Huit, Neuf, Dix, Valet, Reine, Roi, As }

[System.Serializable]
public class Card
{
    public CardSuit suit; //Symboles
    public CardValue value; //Valeurs
}
