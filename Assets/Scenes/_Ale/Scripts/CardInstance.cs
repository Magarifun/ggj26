using UnityEngine;

public class CardInstance : MonoBehaviour
{
    public string cardId;
    public int copyNumber;

    private CardPoolManager pool;
    private bool releasedToPool;

    public void Init(CardPoolManager poolManager, string id, int copy)
    {
        pool = poolManager;
        cardId = id;
        copyNumber = copy;
    }

    public void MarkReleasedToPool()
    {
        releasedToPool = true;
    }

    private void OnDestroy()
    {
        // Se viene distrutta SENZA essere stata già "rilasciata" per perdita tile, libera la copia
        if (!releasedToPool && pool != null && !string.IsNullOrEmpty(cardId))
            pool.ReleaseCopy(cardId, copyNumber);
    }
}