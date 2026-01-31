using UnityEngine;

public class CardsUnlocker : MonoBehaviour
{
    [Header("Debug Unlock")]
    [Tooltip("Scrivi qui il cardId (CardPoolEntry.cardId) da sbloccare in debug.")]
    [SerializeField] private string debugCardId = "ID_DELLA_CARTA";

    [Tooltip("Se true, prova a fare auto-find del CardPoolManager se non trovato.")]
    [SerializeField] private bool autoFindPoolManager = true;

    [SerializeField] private CardPoolManager poolManager;

    private void Awake()
    {
        if (poolManager == null && autoFindPoolManager)
            poolManager = FindObjectOfType<CardPoolManager>();
    }

    public void Unlock(string cardId)
    {
        if (string.IsNullOrEmpty(cardId))
        {
            Debug.LogWarning("[CardsUnlocker] cardId vuoto.");
            return;
        }

        var pool = poolManager != null ? poolManager : FindObjectOfType<CardPoolManager>();
        if (pool == null)
        {
            Debug.LogError("[CardsUnlocker] CardPoolManager non trovato in scena.");
            return;
        }

        pool.SetCardUnlocked(cardId, true);
        Debug.Log($"[CardsUnlocker] UNLOCK -> {cardId}");
    }

    // =========================================================
    // DEBUG BUTTONS (Inspector Context Menu)
    // =========================================================

    [ContextMenu("DEBUG: Unlock debugCardId")]
    private void DebugUnlockFromField()
    {
        Unlock(debugCardId);
    }

    [ContextMenu("DEBUG: Lock debugCardId")]
    private void DebugLockFromField()
    {
        if (string.IsNullOrEmpty(debugCardId))
        {
            Debug.LogWarning("[CardsUnlocker] cardId vuoto.");
            return;
        }

        var pool = poolManager != null ? poolManager : FindObjectOfType<CardPoolManager>();
        if (pool == null)
        {
            Debug.LogError("[CardsUnlocker] CardPoolManager non trovato in scena.");
            return;
        }

        pool.SetCardUnlocked(debugCardId, false);
        Debug.Log($"[CardsUnlocker] LOCK -> {debugCardId}");
    }
}