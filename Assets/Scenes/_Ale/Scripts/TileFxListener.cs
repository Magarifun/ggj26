using UnityEngine;

public class TileFxListener : MonoBehaviour
{
    private void OnEnable()
    {
        if (CardTileEventManager.I == null) return;

        CardTileEventManager.I.OnTileHoverEnter += HoverEnter;
        CardTileEventManager.I.OnTileHoverExit  += HoverExit;
        CardTileEventManager.I.OnTileGrabbed    += Grabbed;
        CardTileEventManager.I.OnTilePlaced     += Placed;
    }

    private void OnDisable()
    {
        if (CardTileEventManager.I == null) return;

        CardTileEventManager.I.OnTileHoverEnter -= HoverEnter;
        CardTileEventManager.I.OnTileHoverExit  -= HoverExit;
        CardTileEventManager.I.OnTileGrabbed    -= Grabbed;
        CardTileEventManager.I.OnTilePlaced     -= Placed;
    }

    private void HoverEnter(CardDragAndDrop2D_SnapSortingErase card, Collider2D tile)
    {
        // esempio highlight
        var sr = tile.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
    }

    private void HoverExit(CardDragAndDrop2D_SnapSortingErase card, Collider2D tile)
    {
        // esempio unhighlight (qui fai come vuoi)
    }

    private void Grabbed(CardDragAndDrop2D_SnapSortingErase card)
    {
        // esempio suono / fx
        // Debug.Log("Grabbed: " + card.name);
    }

    private void Placed(CardDragAndDrop2D_SnapSortingErase card, BoardSlot2D slot)
    {
        // esempio: quando piazzata
        // Debug.Log($"Placed {card.name} on slot {(slot ? slot.name : "null")}");
    }
}