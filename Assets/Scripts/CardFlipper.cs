using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))] // Force la présence d'un AudioSource
public class CardFlipper : MonoBehaviour
{
    [Header("Réglages Animation")]
    public float duration = 0.6f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio")]
    public AudioClip flipSound; // Glisse ton fichier .wav ou .mp3 ici
    [Range(0, 1)] public float volume = 0.5f;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false; 
    }

    public void RevealPlayerHand(List<GameObject> cardObjects)
    {
        StopAllCoroutines();
        StartCoroutine(RevealSequence(cardObjects));
    }

    private IEnumerator RevealSequence(List<GameObject> cardObjects)
    {
        for (int i = 0; i < cardObjects.Count; i++)
        {
            if (cardObjects[i] != null)
            {
                // On joue le son au début de chaque flip
                PlayFlipSound();

                StartCoroutine(FlipAnimation(cardObjects[i].transform));

                // Petit délai avant la carte suivante
                yield return new WaitForSeconds(0.15f);
            }
        }
    }

    private void PlayFlipSound()
    {
        if (audioSource != null && flipSound != null)
        {
            audioSource.PlayOneShot(flipSound, volume);
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