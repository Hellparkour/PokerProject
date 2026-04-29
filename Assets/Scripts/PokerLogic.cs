using UnityEngine;
using System.Collections.Generic;

public class PokerLogic : MonoBehaviour
{
    [Header("Configuration Visuelle")]
    public GameObject cardPrefab;
    public Transform[] playerCardSlots;    // 5 slots côté joueur
    public Transform[] computerCardSlots;  // 5 slots côté ordinateur

    [Header("Scripts & Animation")]
    public CardFlipper flipper;
    public CameraTraveling camTraveling;

    [Header("État du Jeu")]
    public List<Card> fullDeck = new List<Card>();
    public List<Card> playerHand = new List<Card>();
    public List<Card> computerHand = new List<Card>();

    private List<GameObject> playerCardVisuals = new List<GameObject>();
    private bool isHandComplete = false;

    void Start() => ResetGame();

    public void ResetGame()
    {
        GenerateFullDeck();
        playerHand.Clear();
        computerHand.Clear();

        // Nettoyage de la table
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Player")) { Destroy(obj); } // Si tu as mis un tag
                                                                                                  // Ou plus simple via une liste globale si nécessaire

        playerCardVisuals.Clear();
        isHandComplete = false;
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
        if (isHandComplete || playerHand.Count >= 5) return;

        if (playerHand.Exists(c => c.suit == card.suit && c.value == card.value)) return;

        playerHand.Add(card);
        RemoveCardFromDeck(card.suit, card.value);

        // Instanciation de la carte du joueur
        if (playerCardSlots.Length >= playerHand.Count)
        {
            Transform slot = playerCardSlots[playerHand.Count - 1];
            GameObject newCard = Instantiate(cardPrefab, slot.position, slot.rotation);

            // Applique la texture
            newCard.GetComponent<CardVisualUpdater>()?.SetCardVisual(card);

            // FLIP
            playerCardVisuals.Add(newCard);
        }

        if (playerHand.Count == 5)
        {
            isHandComplete = true;
            FinishRound();
        }
    }

    private void FinishRound()
    {
        // Générer et afficher les cartes de l'ordi (face cachée)
        SpawnComputerHandVisual();

        // Lancer le traveling de la caméra vers le haut (et pivot vers le bas)
        if (camTraveling != null) camTraveling.MoveToTopView();

        // Révéler les cartes du joueur après un léger délai
        Invoke("ExecuteFlip", 0.8f);
    }

    private void SpawnComputerHandVisual()
    {
        for (int i = 0; i < 5; i++)
        {
            if (fullDeck.Count == 0) break;

            // Logique de données
            int randomIndex = Random.Range(0, fullDeck.Count);
            Card cpuCard = fullDeck[randomIndex];
            computerHand.Add(cpuCard);
            fullDeck.RemoveAt(randomIndex);

            // Logique visuelle
            if (computerCardSlots.Length > i)
            {
                Transform slot = computerCardSlots[i];
                GameObject cpuObj = Instantiate(cardPrefab, slot.position, slot.rotation);

                // On met la texture quand même (au cas où), mais PAS dans playerCardVisuals
                cpuObj.GetComponent<CardVisualUpdater>()?.SetCardVisual(cpuCard);
            }
        }
    }

    private void RemoveCardFromDeck(CardSuit s, CardValue v)
    {
        fullDeck.RemoveAll(c => c.suit == s && c.value == v);
    }
}