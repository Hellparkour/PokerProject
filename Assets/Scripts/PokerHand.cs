using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// On enlève MonoBehaviour car c'est une classe de calcul (Data Class)
public class PokerHand
{
    public List<Card> cards;
    public Combo handCombo;

    public PokerHand(List<Card> handCards)
    {
        this.cards = handCards;
        this.handCombo = EvaluateHand();
    }

    private Combo EvaluateHand()
    {
        // On trie les cartes par valeur pour faciliter les calculs
        var sorted = cards.OrderBy(c => (int)c.value).ToList();

        bool isFlush = cards.GroupBy(c => c.suit).Count() == 1;
        bool isStraight = IsStraight(sorted);

        var groups = cards.GroupBy(c => c.value)
                          .Select(g => g.Count())
                          .OrderByDescending(c => c)
                          .ToArray();

        int maxGroup = groups[0];

        // 1. Quinte Flush & Quinte Flush Royale
        if (isFlush && isStraight)
        {
            if (cards.Any(c => c.value == CardValue.As) && cards.Any(c => c.value == CardValue.Roi))
                return Combo.RoyalFlush;
            return Combo.StraightFlush;
        }

        if (maxGroup == 4) return Combo.FourOfAKind;
        if (maxGroup == 3 && groups.Length >= 2 && groups[1] == 2) return Combo.FullHouse;
        if (isFlush) return Combo.Flush;
        if (isStraight) return Combo.Straight;
        if (maxGroup == 3) return Combo.ThreeOfAKind;
        if (maxGroup == 2 && groups.Length >= 2 && groups[1] == 2) return Combo.TwoPairs;
        if (maxGroup == 2) return Combo.OnePair;
        return Combo.HighCard;
    }

    private bool IsStraight(List<Card> sortedCards)
    {
        int[] values = sortedCards.Select(c => (int)c.value).ToArray();

        bool normalStraight = true;
        for (int i = 0; i < values.Length - 1; i++)
        {
            if (values[i + 1] != values[i] + 1)
            {
                normalStraight = false;
                break;
            }
        }
        if (normalStraight) return true;

        if (values[4] == (int)CardValue.As &&
            values[0] == (int)CardValue.Deux &&
            values[1] == (int)CardValue.Trois &&
            values[2] == (int)CardValue.Quatre &&
            values[3] == (int)CardValue.Cinq)
        {
            return true;
        }

        return false;
    }
}