using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // cardId -> prefab
    private readonly Dictionary<string, GameObject> prefabById = new();

    // cardId -> maxCopies (da CardPoolEntry)
    private readonly Dictionary<string, int> maxCopiesById = new();

    // cardId -> lista copie disponibili
    private readonly Dictionary<string, List<int>> availableCopiesById = new();

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
            lastPoolEvent = "POOL EMPTY -> no more spawnable cards";
            Debug.Log("[CardPoolManager] Non esistono più carte generabili (pool vuoto).");
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
        cardGO.SetActive(true); // IMPORTANT: visibilità attiva
        cardGO.transform.SetParent(handRoot, true);

        // rename
        cardGO.name = $"{id}{copyNumber}";

        // instance info
        var inst = cardGO.GetComponent<CardInstance>();
        if (inst == null) inst = cardGO.AddComponent<CardInstance>();
        inst.cardId = id;
        inst.copyNumber = copyNumber;
        inst.returnedToPoolOnce = false;

        // lifecycle
        var life = cardGO.GetComponent<CardLifecycle>();
        if (life == null) life = cardGO.AddComponent<CardLifecycle>();

        // safety: evita doppie subscription se prefab già aveva component
        life.OnPlaced -= HandlePlaced;
        life.OnLostTileAfterPlaced -= HandleLostTileAfterPlaced;

        life.OnPlaced += HandlePlaced;
        life.OnLostTileAfterPlaced += HandleLostTileAfterPlaced;

        // slot ref
        var slotRef = cardGO.GetComponent<CardHandSlotRef>();
        if (slotRef == null) slotRef = cardGO.AddComponent<CardHandSlotRef>();
        slotRef.slotIndex = slotIndex;

        lastPoolEvent = $"SPAWN -> {id}{copyNumber} (slot {slotIndex})";

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

        // 👇 QUI: respawn con DELAY
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
            if (kv.Value != null && kv.Value.Count > 0)
                generatableIds.Add(kv.Key);
        }
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
            Debug.Log($"[Pool] {id} -> [{copies}] (max={maxCopiesById[id]})");
        }

        Debug.Log($"[Pool] last event: {lastPoolEvent}");
    }
}