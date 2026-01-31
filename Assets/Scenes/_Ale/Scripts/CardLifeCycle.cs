using UnityEngine;

public class CardLifecycle : MonoBehaviour
{
    public System.Action<CardLifecycle> OnPlaced;
    public System.Action<CardLifecycle> OnLostTileAfterPlaced;

    private bool placed;
    private bool releasedOnce;
    private int initialTileCount = -1;

    private void Start()
    {
        initialTileCount = GetTileCount();
    }

    public void MarkPlaced()
    {
        if (placed) return;
        placed = true;
        OnPlaced?.Invoke(this);
    }

    // Chiamalo DOPO la distruzione tile (dopo drop)
    public void CheckIfLostTiles()
    {
        if (!placed) return;
        if (releasedOnce) return;

        int now = GetTileCount();
        if (now < initialTileCount)
        {
            releasedOnce = true; // ✅ rilascia una sola volta
            OnLostTileAfterPlaced?.Invoke(this);
        }
    }

    private int GetTileCount()
    {
        return GetComponentsInChildren<SpriteRenderer>(true).Length;
    }
}