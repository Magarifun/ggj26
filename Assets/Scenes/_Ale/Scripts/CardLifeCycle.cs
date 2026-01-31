using UnityEngine;

public class CardLifeCycle : MonoBehaviour
{
    public System.Action<CardLifeCycle> OnPlaced;
    public System.Action<CardLifeCycle> OnLostTilesAfterPlaced;

    private bool placed = false;
    private bool reintroducedOnce = false;
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

    // chiamalo dopo la cancellazione tile (dopo drop)
    public void CheckIfLostTiles()
    {
        if (!placed) return;
        if (reintroducedOnce) return;

        int now = GetTileCount();
        if (now < initialTileCount)
        {
            reintroducedOnce = true;               // ✅ una sola volta
            OnLostTilesAfterPlaced?.Invoke(this);
        }
    }

    private int GetTileCount()
    {
        return GetComponentsInChildren<SpriteRenderer>(true).Length;
    }
}