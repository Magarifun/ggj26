using UnityEngine;

public class CardDragger2D : MonoBehaviour
{
    private CardDragAndDrop parentDrag;

    private void Awake()
    {
        parentDrag = GetComponentInParent<CardDragAndDrop>();
        if (parentDrag == null)
            Debug.LogError("CardDragAndDrop non trovato nel parent!");
    }

    private void OnMouseDown()
    {
        parentDrag?.BeginDrag();
        Debug.Log("Down");
    }

    private void OnMouseUp()
    {
        parentDrag?.EndDrag();
    }
}