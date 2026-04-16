using UnityEngine;
using System.Collections.Generic;

public class PokerLogic : MonoBehaviour
{
    [Header("État du Jeu")]
    public List<Card> fullDeck = new List<Card>();
    public List<Card> playerHand = new List<Card>();
    public List<Card> computerHand = new List<Card>();

    void Start()
    {
        // TODO: Déroulement annexe.
        ResetGame();
    }

    public void ResetGame()
    {
        GenerateFullDeck();
        playerHand.Clear();
        computerHand.Clear();
    }

    // Génère le paquet réel de 52 cartes. (Sans Jokers)
    private void GenerateFullDeck()
    {
        fullDeck.Clear();
        foreach (CardSuit s in System.Enum.GetValues(typeof(CardSuit)))
        {
            foreach (CardValue v in System.Enum.GetValues(typeof(CardValue)))
            {
                Card newCard = new Card();
                newCard.suit = s;
                newCard.value = v;
                fullDeck.Add(newCard);
            }
        }
        Debug.Log("Paquet de 52 cartes prêt.");
    }

    // TODO: faire le Joueur
    public void InitializePlayerHand(List<Card> detectedCards)
    {
        playerHand = detectedCards;

        foreach (Card c in detectedCards)
        {
            RemoveCardFromDeck(c.suit, c.value);
        }

        Debug.Log("Main du joueur enregistré. Cartes restantes dans le deck : " + fullDeck.Count);

        GenerateComputerHand();
    }

    private void GenerateComputerHand()
    {
        // L'ordinateur pioche 5 cartes au hasard parmi celles restantes
        for (int i = 0; i < 5; i++)
        {
            int randomIndex = Random.Range(0, fullDeck.Count);
            computerHand.Add(fullDeck[randomIndex]);
            fullDeck.RemoveAt(randomIndex);
        }
        Debug.Log("Main de l'ordinateur généré.");
    }

    //Enleve la carte spécifique.
    private void RemoveCardFromDeck(CardSuit s, CardValue v)
    {
        fullDeck.RemoveAll(c => c.suit == s && c.value == v);
    }
}