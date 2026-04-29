using UnityEngine;

public class CardSelectable : MonoBehaviour
{
    public bool isSelected = false;
    private Vector3 originalLocalPos;
    private bool posSaved = false;

    public void ToggleSelect()
    {
        if (!posSaved)
        {
            originalLocalPos = transform.localPosition;
            posSaved = true;
        }

        isSelected = !isSelected;
        transform.localPosition = isSelected ? originalLocalPos + Vector3.up * 0.2f : originalLocalPos;

        Debug.Log(gameObject.name + " sélectionnée : " + isSelected);
    }
}