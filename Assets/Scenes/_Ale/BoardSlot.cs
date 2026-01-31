using UnityEngine;

public class BoardSlot : MonoBehaviour
{
    public CardDragAndDrop2D occupiedBy;

    public bool IsFree => occupiedBy == null;
}