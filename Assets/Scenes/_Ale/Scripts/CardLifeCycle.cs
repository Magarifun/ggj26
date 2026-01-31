using UnityEngine;

public class CardLifecycle : MonoBehaviour
{
    public System.Action<CardLifecycle> OnPlaced;
    public System.Action<CardLifecycle> OnLostTileAfterPlaced;

    private bool placed;
    private bool lostTriggered;

    public void MarkPlaced()
    {
        if (placed) return;
        placed = true;
        OnPlaced?.Invoke(this);
    }

    /// <summary>
    /// Chiamalo quando una tile di questa card viene distrutta da un'altra card.
    /// </summary>
    public void NotifyLostTile()
    {
        if (!placed) return;        // deve essere già piazzata
        if (lostTriggered) return;  // una sola volta

        lostTriggered = true;
        OnLostTileAfterPlaced?.Invoke(this);
    }
}