using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.XR;

public class PokerHand : MonoBehaviour
{
    public List<Card> cards;
    public Combo handCombo;

    public PokerHand(List<Card> handCards)
    {
        cards = handCards;
        handCombo = EvaluateHand();
    }
    private Combo EvaluateHand()
    {
        bool isFlush = cards.GroupBy(c => c.suit).Count() == 1;
        bool isStraight = IsStraight();
        var groups = cards.GroupBy(c => c.value).Select(g => g.Count()).OrderByDescending(c => c).ToArray();
        int maxGroup = groups[0];

        if (isFlush && isStraight)
        {
            if (cards.Any(c => c.value == CardValue.As) && cards.Any(c => c.value == CardValue.Roi))
                return Combo.RoyalFlush;
            return Combo.StraightFlush;
        }
        if (maxGroup == 4) return Combo.FourOfAKind;
        if (maxGroup == 3 && groups.Length > 1 && groups[1] == 2) return Combo.FullHouse;
        if (isFlush) return Combo.Flush;
        if (isStraight) return Combo.Straight;
        if (maxGroup == 3) return Combo.ThreeOfAKind;
        if (maxGroup == 2 && groups.Length > 1 && groups[1] == 2) return Combo.TwoPairs;
        if (maxGroup == 2) return Combo.OnePair;

        return Combo.HighCard; 

    }
    private bool IsStraight()
    {
        var sortedValues = cards.Select(c => (int)c.value).OrderBy(v => v).ToArray();
        for (int i = 1; i < sortedValues.Length; i++)
        {
            if (sortedValues[i] != sortedValues[i - 1] + 1)
                return false;
        }
        return true;
    }
}