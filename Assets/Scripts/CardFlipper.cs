using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CardFlipper : MonoBehaviour
{
    [Header("Réglages Animation")]
    public float duration = 0.6f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Courbe douce par défaut

    public void RevealPlayerHand(List<GameObject> cardObjects)
    {
        StopAllCoroutines(); // Sécurité
        StartCoroutine(RevealSequence(cardObjects));
    }

    private IEnumerator RevealSequence(List<GameObject> cardObjects)
    {
        for (int i = 0; i < cardObjects.Count; i++)
        {
            if (cardObjects[i] != null)
            {
                StartCoroutine(FlipAnimation(cardObjects[i].transform));

                yield return new WaitForSeconds(0.15f);
            }
        }
    }

    private IEnumerator FlipAnimation(Transform target)
    {
        float elapsed = 0f;
        Quaternion startRotation = target.rotation;

        Quaternion endRotation = startRotation * Quaternion.Euler(180, 0, 0);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curvedT = curve.Evaluate(t);

            target.rotation = Quaternion.Slerp(startRotation, endRotation, curvedT);
            yield return null;
        }

        target.rotation = endRotation;
    }
}