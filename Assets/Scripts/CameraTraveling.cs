using UnityEngine;
using System.Collections;

public class CameraTraveling : MonoBehaviour
{
    [Header("Cible")]
    public Transform targetView; // L'objet vide placé en haut avec Rotation X = 90 (requis)

    [Header("Réglages")]
    public float duration = 1.5f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public void MoveToTopView()
    {
        if (targetView == null) return;
        StopAllCoroutines();
        StartCoroutine(TravelingRoutine());
    }

    private IEnumerator TravelingRoutine()
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = curve.Evaluate(elapsed / duration);

            transform.position = Vector3.Lerp(startPos, targetView.position, t);
            transform.rotation = Quaternion.Slerp(startRot, targetView.rotation, t);

            yield return null;
        }

        transform.position = targetView.position;
        transform.rotation = targetView.rotation;
    }
}
