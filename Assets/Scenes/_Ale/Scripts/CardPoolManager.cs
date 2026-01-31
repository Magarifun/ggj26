using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CardPoolManager : MonoBehaviour
{
    [System.Serializable]
    public class CardPrefabData
    {
        public GameObject prefab;
    }

    [System.Serializable]
    public class PoolDebugEntry
    {
        public string cardId;
        public int maxCopies;
        public int availableCount;
        public string availableCopiesString; // "1,3,5"
    }

    [Header("Spawn Timing")]
    [SerializeField] private float spawnDelay = 0.25f;

    [Header("Prefabs Pool")]
    public List<CardPrefabData> prefabs = new();

    [Header("Hand")]
    public int handSize = 3;
    public Transform[] handSlots;
    public Transform handRoot;

    [Header("Live Debug (Inspector)")]
    public bool liveInspectorDebug = true;

    [SerializeField] private List<PoolDebugEntry> poolDebug = new();
    [SerializeField] private string lastPoolEvent = "";

    [Header("Events (Inspector)")]
    public UnityEvent OnSpawnTile;

    // cardId -> prefab
    private readonly Dictionary<string, GameObject> prefabById = new();

    // cardId -> maxCopies (da CardPoolEntry)
    private readonly Dictionary<string, int> maxCopiesById = new();

    // cardId -> lista copie disponibili
    private readonly Dictionary<string, List<int>> availableCopiesById = new();

    // cardId -> unlocked (dinamico)
    private readonly Dictionary<string, bool> unlockedById = new();

    // lista id generabili
    private readonly List<string> generatableIds = new();

    private void Start()
    {
        if (handRoot == null) handRoot = transform;

        BuildPools();
        FillHand();
        UpdateInspectorDebug();
    }

    private void Update()
    {
        if (!liveInspectorDebug) return;
        UpdateInspectorDebug();
    }

    // =========================================================
    // PUBLIC API (UNLOCK)
    // =========================================================

    /// <summary>
    /// Sblocca/Blocca una card runtime. Aggiorna subito il pool.
    /// </summary>
    public void SetCardUnlocked(string cardId, bool value)
    {
        if (string.IsNullOrEmpty(cardId)) return;

        if (!unlockedById.ContainsKey(cardId))
        {
            Debug.LogWarning($"[CardPoolManager] SetCardUnlocked: cardId '{cardId}' non trovato nel pool.");
            return;
        }

        unlockedById[cardId] = value;
        lastPoolEvent = $"UNLOCK -> {cardId} = {value}";
        RefreshGeneratableIds();
        UpdateInspectorDebug();
    }

    /// <summary>
    /// Legge lo stato UNLOCK dal prefab (CardUnlockState) e aggiorna la cache.
    /// Utile se modifichi CardUnlockState direttamente sul prefab o su un "database".
    /// </summary>
    public void RefreshUnlocksFromPrefabs()
    {
        foreach (var kv in prefabById)
        {
            string id = kv.Key;
            var prefab = kv.Value;

            var unlock = prefab != null ? prefab.GetComponent<CardUnlockState>() : null;
            unlockedById[id] = (unlock == null) ? true : unlock.Unlocked;
        }

        lastPoolEvent = "UNLOCK REFRESH FROM PREFABS";
        RefreshGeneratableIds();
        UpdateInspectorDebug();
    }

    // =========================================================
    // DELAYED SPAWN
    // =========================================================

    private IEnumerator SpawnCardInSlotDelayed(int slotIndex)
    {
        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        TrySpawnInSlot(slotIndex);
    }

    // =========================================================
    // POOLS
    // =========================================================

    private void BuildPools()
    {
        prefabById.Clear();
        maxCopiesById.Clear();
        availableCopiesById.Clear();
        unlockedById.Clear();
        generatableIds.Clear();

        foreach (var p in prefabs)
        {
            if (p.prefab == null) continue;

            var entry = p.prefab.GetComponent<CardPoolEntry>();
            if (entry == null || string.IsNullOrEmpty(entry.cardId))
            {
                Debug.LogError($"[CardPoolManager] Prefab '{p.prefab.name}' senza CardPoolEntry o cardId vuoto.");
                continue;
            }

            string id = entry.cardId;

            if (prefabById.ContainsKey(id))
            {
                Debug.LogWarning($"[CardPoolManager] cardId duplicato '{id}', ignoro duplicato.");
                continue;
            }

            int max = Mathf.Max(0, entry.maxCopies);

            prefabById[id] = p.prefab;
            maxCopiesById[id] = max;

            var list = new List<int>(max);
            for (int i = 1; i <= max; i++)
                list.Add(i);

            availableCopiesById[id] = list;

            // ✅ init unlocked dal prefab CardUnlockState
            var unlock = p.prefab.GetComponent<CardUnlockState>();
            unlockedById[id] = (unlock == null) ? true : unlock.Unlocked;
        }

        RefreshGeneratableIds();
    }

    private void FillHand()
    {
        int slotsToFill = Mathf.Min(handSize, handSlots != null ? handSlots.Length : 0);
        for (int i = 0; i < slotsToFill; i++)
            TrySpawnInSlot(i);
    }

    private void TrySpawnInSlot(int slotIndex)
    {
        if (handSlots == null || slotIndex < 0 || slotIndex >= handSlots.Length) return;

        RefreshGeneratableIds();

        if (generatableIds.Count == 0)
        {
            lastPoolEvent = "POOL EMPTY or ALL LOCKED -> no more spawnable cards";
            Debug.Log("[CardPoolManager] Nessuna carta generabile: pool vuoto o tutte bloccate.");
            return;
        }

        string id = generatableIds[Random.Range(0, generatableIds.Count)];
        List<int> available = availableCopiesById[id];

        if (available == null || available.Count == 0)
        {
            RefreshGeneratableIds();
            return;
        }

        int pickIndex = Random.Range(0, available.Count);
        int copyNumber = available[pickIndex];

        // rimuovi dal pool
        available.RemoveAt(pickIndex);

        GameObject prefab = prefabById[id];
        Transform slot = handSlots[slotIndex];

        GameObject cardGO = Instantiate(prefab, slot.position, slot.rotation);
        cardGO.SetActive(true);
        cardGO.transform.SetParent(handRoot, true);

        cardGO.name = $"{id}{copyNumber}";

        var inst = cardGO.GetComponent<CardInstance>();
        if (inst == null) inst = cardGO.AddComponent<CardInstance>();
        inst.cardId = id;
        inst.copyNumber = copyNumber;
        inst.returnedToPoolOnce = false;

        var life = cardGO.GetComponent<CardLifecycle>();
        if (life == null) life = cardGO.AddComponent<CardLifecycle>();

        life.OnPlaced -= HandlePlaced;
        life.OnLostTileAfterPlaced -= HandleLostTileAfterPlaced;

        life.OnPlaced += HandlePlaced;
        life.OnLostTileAfterPlaced += HandleLostTileAfterPlaced;

        var slotRef = cardGO.GetComponent<CardHandSlotRef>();
        if (slotRef == null) slotRef = cardGO.AddComponent<CardHandSlotRef>();
        slotRef.slotIndex = slotIndex;

        lastPoolEvent = $"SPAWN -> {id}{copyNumber} (slot {slotIndex})";

        OnSpawnTile?.Invoke();

        RefreshGeneratableIds();
        UpdateInspectorDebug();
    }

    // =========================================================
    // EVENTS
    // =========================================================

    private void HandlePlaced(CardLifecycle life)
    {
        var slotRef = life.GetComponent<CardHandSlotRef>();
        int slotIndex = slotRef != null ? slotRef.slotIndex : -1;

        if (slotIndex < 0) return;

        StartCoroutine(SpawnCardInSlotDelayed(slotIndex));
    }

    private void HandleLostTileAfterPlaced(CardLifecycle life)
    {
        var inst = life.GetComponent<CardInstance>();
        if (inst == null) return;

        if (inst.returnedToPoolOnce) return;
        inst.returnedToPoolOnce = true;

        ReturnCopyToPool(inst.cardId, inst.copyNumber);
    }

    // =========================================================
    // RETURN TO POOL
    // =========================================================

    private void ReturnCopyToPool(string cardId, int copyNumber)
    {
        if (string.IsNullOrEmpty(cardId)) return;

        if (!availableCopiesById.ContainsKey(cardId)) return;
        if (!maxCopiesById.ContainsKey(cardId)) return;

        int max = maxCopiesById[cardId];

        if (copyNumber < 1 || copyNumber > max) return;

        var list = availableCopiesById[cardId];

        if (list.Contains(copyNumber))
            return;

        if (list.Count >= max)
            return;

        list.Add(copyNumber);

        lastPoolEvent = $"RETURN -> {cardId}{copyNumber} back to pool";
        Debug.Log($"[CardPoolManager] Copia restituita al pool: {cardId}{copyNumber}");

        RefreshGeneratableIds();
        UpdateInspectorDebug();
    }

    // =========================================================
    // DEBUG / HELPERS
    // =========================================================

    private void RefreshGeneratableIds()
    {
        generatableIds.Clear();

        foreach (var kv in availableCopiesById)
        {
            string id = kv.Key;

            // ✅ filtro UNLOCKED
            if (unlockedById.TryGetValue(id, out bool unlocked) && !unlocked)
                continue;

            if (kv.Value != null && kv.Value.Count > 0)
                generatableIds.Add(id);
        }
    }

    public GameObject GetPrefabById(string cardId)
{
    if (string.IsNullOrEmpty(cardId)) return null;
    return prefabById.TryGetValue(cardId, out var p) ? p : null;
}

public List<string> GetLockedCardIds()
{
    var locked = new List<string>();

    foreach (var kv in unlockedById)
    {
        if (!kv.Value) // false = locked
            locked.Add(kv.Key);
    }

    return locked;
}

    private void UpdateInspectorDebug()
    {
        poolDebug.Clear();

        foreach (var kv in availableCopiesById)
        {
            string id = kv.Key;
            List<int> list = kv.Value;

            int max = maxCopiesById.ContainsKey(id) ? maxCopiesById[id] : 0;

            PoolDebugEntry e = new PoolDebugEntry();
            e.cardId = id;
            e.maxCopies = max;
            e.availableCount = list != null ? list.Count : 0;

            if (list == null || list.Count == 0)
                e.availableCopiesString = "EMPTY";
            else
                e.availableCopiesString = string.Join(",", list);

            poolDebug.Add(e);
        }
    }

    [ContextMenu("Debug: Print Pools (Console)")]
    private void DebugPrintConsole()
    {
        foreach (var kv in availableCopiesById)
        {
            string id = kv.Key;
            string copies = kv.Value.Count > 0 ? string.Join(",", kv.Value) : "EMPTY";
            bool unlocked = !unlockedById.ContainsKey(id) || unlockedById[id];
            Debug.Log($"[Pool] {id} -> [{copies}] (max={maxCopiesById[id]}) unlocked={unlocked}");
        }

        Debug.Log($"[Pool] last event: {lastPoolEvent}");
    }
}