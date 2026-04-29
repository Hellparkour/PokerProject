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
        ResetGame();
    }

    public void ResetGame()
    {
        GenerateFullDeck();
        playerHand.Clear();
        computerHand.Clear();
    }

    private void GenerateFullDeck()
    {
        fullDeck.Clear();
        foreach (CardSuit s in System.Enum.GetValues(typeof(CardSuit)))
        {
            foreach (CardValue v in System.Enum.GetValues(typeof(CardValue)))
            {
                fullDeck.Add(new Card { suit = s, value = v });
            }
        }
    }

    public void AddDetectedCardToPlayer(Card card)
    {
        // 1. Vérifier si on a déjà 5 cartes
        if (playerHand.Count >= 5)
        {
            Debug.Log("Main pleine !");
            return;
        }

        // 2. Vérifier si la carte est déjà en main
        if (playerHand.Exists(c => c.suit == card.suit && c.value == card.value))
        {
            Debug.LogWarning("Carte déjà détectée.");
            return;
        }

        // 3. Ajouter et retirer du deck
        playerHand.Add(card);
        RemoveCardFromDeck(card.suit, card.value);
        Debug.Log($"{card.value} de {card.suit} ajouté ! ({playerHand.Count}/5)");

        if (playerHand.Count == 5)
        {
            GenerateComputerHand();
        }
    }

    private void GenerateComputerHand()
    {
        for (int i = 0; i < 5; i++)
        {
            int randomIndex = Random.Range(0, fullDeck.Count);
            computerHand.Add(fullDeck[randomIndex]);
            fullDeck.RemoveAt(randomIndex);
        }
        Debug.Log("Main de l'ordinateur prête !");
    }

    private void RemoveCardFromDeck(CardSuit s, CardValue v)
    {
        fullDeck.RemoveAll(c => c.suit == s && c.value == v);
    }
}