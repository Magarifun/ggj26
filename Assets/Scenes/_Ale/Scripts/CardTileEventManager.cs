using System;
using UnityEngine;

public class CardTileEventManager : MonoBehaviour
{
    public static CardTileEventManager I { get; private set; }

    // card, tileCollider
    public event Action<CardDragAndDrop2D_SnapSortingErase, Collider2D> OnTileHoverEnter;
    public event Action<CardDragAndDrop2D_SnapSortingErase, Collider2D> OnTileHoverExit;

    // card
    public event Action<CardDragAndDrop2D_SnapSortingErase> OnTileGrabbed;

    // card, snappedSlot (può essere null se vuoi usarlo comunque)
    public event Action<CardDragAndDrop2D_SnapSortingErase, BoardSlot2D> OnTilePlaced;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RaiseHoverEnter(CardDragAndDrop2D_SnapSortingErase card, Collider2D tile)
        => OnTileHoverEnter?.Invoke(card, tile);

    public void RaiseHoverExit(CardDragAndDrop2D_SnapSortingErase card, Collider2D tile)
        => OnTileHoverExit?.Invoke(card, tile);

    public void RaiseGrabbed(CardDragAndDrop2D_SnapSortingErase card)
        => OnTileGrabbed?.Invoke(card);

    public void RaisePlaced(CardDragAndDrop2D_SnapSortingErase card, BoardSlot2D slot)
        => OnTilePlaced?.Invoke(card, slot);
}