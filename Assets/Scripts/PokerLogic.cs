using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PokerLogic : MonoBehaviour
{
    [Header("Configuration Visuelle")]
    public GameObject cardPrefab;
    public Transform[] playerCardSlots;    // 5 slots côté joueur
    public Transform[] computerCardSlots;  // 5 slots côté ordinateur

    [Header("Scripts & UI/Animation")]
    public CardFlipper flipper;
    public CameraTraveling camTraveling;
    public GameObject uiPanel;

    [Header("État du Jeu")]
    public List<Card> fullDeck = new List<Card>();
    public List<Card> playerHand = new List<Card>();
    public List<Card> computerHand = new List<Card>();

    [Header("Sons de Fin de Partie")]
    public AudioSource audioSource;
    public AudioClip soundVictory;
    public AudioClip soundDefeat;

    [HideInInspector] public List<GameObject> playerCardVisuals = new List<GameObject>();
    private List<GameObject> computerCardVisuals = new List<GameObject>();

    private bool isHandComplete = false;
    private bool canSelect = false;
    private bool hasExchanged = false; // Limite à un seul échange

    void Start()
    {
        if (uiPanel != null) uiPanel.SetActive(false);
        ResetGame();
    }

    void Update()
    {
        // On bloque la sélection si l'échange est déjà fait
        if (!canSelect || hasExchanged) return;

        // SÉLECTION CLAVIER (1-5)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) ToggleCardSelection(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) ToggleCardSelection(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) ToggleCardSelection(2);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) ToggleCardSelection(3);
            if (Keyboard.current.digit5Key.wasPressedThisFrame) ToggleCardSelection(4);
        }

        // SÉLECTION CLIC (RAYCAST SUR TRIGGER)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(mousePos);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                {
                    CardSelectable sel = hit.collider.GetComponentInParent<CardSelectable>();
                    if (sel != null) sel.ToggleSelect();
                }
            }
        }
    }

    // PHASE DE SCAN (VUFORIA)

    public void AddDetectedCardToPlayer(Card card)
    {
        if (playerHand.Exists(c => c.suit == card.suit && c.value == card.value)) return;

        // SCAN-TO-REPLACE (1 fois seulement)
        if (isHandComplete && !hasExchanged)
        {
            for (int i = 0; i < playerCardVisuals.Count; i++)
            {
                CardSelectable sel = playerCardVisuals[i].GetComponent<CardSelectable>();
                if (sel != null && sel.isSelected)
                {
                    hasExchanged = true; 
                    ReplacePlayerCardWithScan(i, card);
                    uiPanel.SetActive(false);
                    StartCoroutine(AIRoutine());
                    return;
                }
            }
            return;
        }

        // REMPLISSAGE INITIAL
        if (playerHand.Count < 5)
        {
            playerHand.Add(card);
            RemoveCardFromDeck(card.suit, card.value);

            Transform slot = playerCardSlots[playerHand.Count - 1];
            GameObject newCard = Instantiate(cardPrefab, slot.position, slot.rotation);
            newCard.GetComponent<CardVisualUpdater>()?.SetCardVisual(card);
            playerCardVisuals.Add(newCard);

            if (playerHand.Count == 5)
            {
                isHandComplete = true;
                StartInitialSequence();
            }
        }
    }

    private void ReplacePlayerCardWithScan(int index, Card newCardData)
    {
        playerHand[index] = newCardData;
        RemoveCardFromDeck(newCardData.suit, newCardData.value);

        Vector3 targetPos = playerCardSlots[index].position;
        Quaternion rot = playerCardVisuals[index].transform.rotation;

        Destroy(playerCardVisuals[index]);

        GameObject newObj = Instantiate(cardPrefab, targetPos, rot);
        newObj.GetComponent<CardVisualUpdater>()?.SetCardVisual(newCardData);
        playerCardVisuals[index] = newObj;

        Debug.Log($"Échange unique effectué à l'index {index}. Tour du joueur terminé.");
    }

    // FONCTIONS
    public void OnStay() 
    {
        uiPanel.SetActive(false);
        canSelect = false;
        hasExchanged = true; 
        StartCoroutine(AIRoutine());
    }

    public void OnFold() 
    {
        uiPanel.SetActive(false);
        canSelect = false;
        StartCoroutine(FoldSequence());
    }

    private IEnumerator AIRoutine()
    {
        yield return new WaitForSeconds(1.5f);

        PokerHand aiEval = new PokerHand(computerHand);
        List<int> toSwap = new List<int>();

        if (aiEval.handCombo == Combo.HighCard)
        {
            var lowestIndices = computerHand.Select((c, i) => new { c, i })
                                            .OrderBy(x => (int)x.c.value).Take(3);
            foreach (var item in lowestIndices) toSwap.Add(item.i);
        }

        if (toSwap.Count > 0)
        {
            foreach (int i in toSwap) { if (computerCardVisuals[i] != null) Destroy(computerCardVisuals[i]); }
            yield return new WaitForSeconds(1.0f);
            foreach (int i in toSwap)
            {
                int rnd = Random.Range(0, fullDeck.Count);
                Card newC = fullDeck[rnd];
                fullDeck.RemoveAt(rnd);
                computerHand[i] = newC;
                GameObject obj = Instantiate(cardPrefab, computerCardSlots[i].position, computerCardSlots[i].rotation);
                obj.GetComponent<CardVisualUpdater>()?.SetCardVisual(newC);
                computerCardVisuals[i] = obj;
            }
        }

        yield return new WaitForSeconds(1.0f);
        if (flipper != null) flipper.RevealPlayerHand(computerCardVisuals);
        DetermineWinner();
    }

    public void ResetGame()
    {
        GenerateFullDeck();
        playerHand.Clear();
        computerHand.Clear();
        foreach (GameObject obj in playerCardVisuals) if (obj != null) Destroy(obj);
        foreach (GameObject obj in computerCardVisuals) if (obj != null) Destroy(obj);
        playerCardVisuals.Clear();
        computerCardVisuals.Clear();

        isHandComplete = false;
        canSelect = false;
        hasExchanged = false; 
        if (uiPanel != null) uiPanel.SetActive(false);
    }

    private void GenerateFullDeck()
    {
        fullDeck.Clear();
        foreach (CardSuit s in System.Enum.GetValues(typeof(CardSuit)))
            foreach (CardValue v in System.Enum.GetValues(typeof(CardValue)))
                fullDeck.Add(new Card { suit = s, value = v });
    }

    private void RemoveCardFromDeck(CardSuit s, CardValue v)
    {
        fullDeck.RemoveAll(c => c.suit == s && c.value == v);
    }

    private void DetermineWinner()
    {
        PokerHand p = new PokerHand(playerHand);
        PokerHand a = new PokerHand(computerHand);

        int scoreJoueur = (int)p.handCombo;
        int scoreIA = (int)a.handCombo;

        Debug.Log($"FIN DE PARTIE : Joueur ({p.handCombo}) vs IA ({a.handCombo})");

        // On envoie les scores à la fonction qui gère les sons et l'affichage
        DeterminerResultat(scoreJoueur, scoreIA);

        Invoke("ResetGame", 5f);
    }

    private IEnumerator FoldSequence()
    {
        foreach (GameObject obj in playerCardVisuals) if (obj != null) Destroy(obj);
        playerCardVisuals.Clear();
        yield return new WaitForSeconds(0.5f);
        if (flipper != null) flipper.RevealPlayerHand(computerCardVisuals);
        yield return new WaitForSeconds(3.0f);
        ResetGame();
    }

    private void StartInitialSequence()
    {
        SpawnComputerHandVisual();
        if (camTraveling != null) camTraveling.MoveToTopView();
        Invoke("ExecuteInitialFlip", 0.8f);
    }

    private void ExecuteInitialFlip()
    {
        if (flipper != null) flipper.RevealPlayerHand(playerCardVisuals);
        Invoke("EnableInteraction", 1.2f);
    }

    private void EnableInteraction()
    {
        canSelect = true;
        if (uiPanel != null) uiPanel.SetActive(true);
    }

    private void SpawnComputerHandVisual()
    {
        for (int i = 0; i < 5; i++)
        {
            int rnd = Random.Range(0, fullDeck.Count);
            Card c = fullDeck[rnd];
            computerHand.Add(c);
            fullDeck.RemoveAt(rnd);
            GameObject obj = Instantiate(cardPrefab, computerCardSlots[i].position, computerCardSlots[i].rotation);
            obj.GetComponent<CardVisualUpdater>()?.SetCardVisual(c);
            computerCardVisuals.Add(obj);
        }
    }

    private void ToggleCardSelection(int index)
    {
        if (index >= 0 && index < playerCardVisuals.Count)
            playerCardVisuals[index].GetComponent<CardSelectable>()?.ToggleSelect();
    }

    public void DeterminerResultat(int scoreJoueur, int scoreIA)
    {
        if (scoreJoueur > scoreIA)
        {
            Debug.Log("<color=green>VICTOIRE !</color>");
            PlayResultSound(soundVictory);
        }
        else if (scoreJoueur < scoreIA)
        {
            Debug.Log("<color=red>DÉFAITE...</color>");
            PlayResultSound(soundDefeat);
        }
        else
        {
            Debug.Log("<color=yellow>ÉGALITÉ</color>");
            // PlayResultSound(soundTie);
        }
    }

    private void PlayResultSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}