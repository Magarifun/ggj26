using System.Collections.Generic;
using UnityEngine;

public class CardPoolManager : MonoBehaviour
{
    [System.Serializable]
    public class CardPrefabData
    {
        public GameObject prefab;
    }

    [Header("Prefabs Pool")]
    public List<CardPrefabData> prefabs = new();

    [Header("Hand")]
    public int handSize = 3;
    public Transform[] handSlots;
    public Transform handRoot;

    [Header("Exhaustion")]
    [SerializeField] private bool stopSpawningWhenEmpty = true;

    private bool poolExhausted = false;

    private class CardIdState
    {
        public GameObject prefab;
        public int maxConcurrent;
        public bool[] inUse;   // copy 1..max
        public int aliveCount;
    }

    private readonly Dictionary<string, CardIdState> states = new();
    private readonly List<string> generatableIds = new();

    private void Start()
    {
        if (handRoot == null) handRoot = transform;

        BuildStates();
        FillHand();
    }

    private void BuildStates()
    {
        states.Clear();
        generatableIds.Clear();
        poolExhausted = false;

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
            int max = Mathf.Max(0, entry.maxConcurrentCopies);

            if (states.ContainsKey(id))
            {
                Debug.LogWarning($"[CardPoolManager] cardId duplicato '{id}'. Ignoro duplicato.");
                continue;
            }

            states[id] = new CardIdState
            {
                prefab = p.prefab,
                maxConcurrent = max,
                inUse = new bool[max],
                aliveCount = 0
            };
        }
    }

    private void FillHand()
    {
        int slotsToFill = Mathf.Min(handSize, handSlots != null ? handSlots.Length : 0);
        for (int i = 0; i < slotsToFill; i++)
            TrySpawnInSlot(i);
    }

    private void TrySpawnInSlot(int slotIndex)
    {
        if (stopSpawningWhenEmpty && poolExhausted) return;

        if (handSlots == null || slotIndex < 0 || slotIndex >= handSlots.Length) return;

        string id = PickRandomGeneratableId();
        if (string.IsNullOrEmpty(id))
        {
            Debug.Log("[CardPoolManager] Non esistono più carte generabili (tutte al massimo).");

            if (stopSpawningWhenEmpty)
                poolExhausted = true; // ✅ STOP definitivo

            return;
        }

        CardIdState st = states[id];
        if (st.maxConcurrent <= 0)
        {
            // non spawnabile, prova a ricalcolare una volta
            RefreshGeneratableIds();
            return;
        }

        int copyNumber = GetFirstFreeCopyNumber(st);
        if (copyNumber < 1)
        {
            RefreshGeneratableIds();
            Debug.Log("[CardPoolManager] Non esistono più carte generabili (tutte al massimo).");

            if (stopSpawningWhenEmpty)
                poolExhausted = true; // ✅ STOP definitivo

            return;
        }

        Transform slot = handSlots[slotIndex];

        GameObject cardGO = Instantiate(st.prefab, slot.position, slot.rotation);
        cardGO.SetActive(true);
        cardGO.transform.SetParent(handRoot, true);

        // usa copia
        st.inUse[copyNumber - 1] = true;
        st.aliveCount++;

        // rename: cardId+numero
        cardGO.name = $"{id}{copyNumber}";

        // CardInstance
        var inst = cardGO.GetComponent<CardInstance>();
        if (inst == null) inst = cardGO.AddComponent<CardInstance>();
        inst.Init(this, id, copyNumber);

        // Lifecycle
        var life = cardGO.GetComponent<CardLifecycle>();
        if (life == null) life = cardGO.AddComponent<CardLifecycle>();

        life.OnPlaced += HandlePlaced;
        life.OnLostTileAfterPlaced += HandleLostTileAfterPlaced;

        // Slot ref
        var slotRef = cardGO.GetComponent<CardHandSlotRef>();
        if (slotRef == null) slotRef = cardGO.AddComponent<CardHandSlotRef>();
        slotRef.slotIndex = slotIndex;

        RefreshGeneratableIds();
    }

    private void HandlePlaced(CardLifecycle life)
    {
        if (stopSpawningWhenEmpty && poolExhausted) return;

        var slotRef = life.GetComponent<CardHandSlotRef>();
        int slotIndex = slotRef != null ? slotRef.slotIndex : -1;

        if (slotIndex >= 0)
            TrySpawnInSlot(slotIndex);
    }

    private void HandleLostTileAfterPlaced(CardLifecycle life)
    {
        var inst = life.GetComponent<CardInstance>();
        if (inst == null || string.IsNullOrEmpty(inst.cardId)) return;

        ReleaseCopy(inst.cardId, inst.copyNumber);

        // evita doppio rilascio se poi distruggi la card
        inst.MarkReleasedToPool();

        // Se il pool era esaurito, ora potrebbe tornare generabile:
        // Se invece vuoi che rimanga "STOP per sempre", commenta le 2 righe sotto.
        poolExhausted = false;
        RefreshGeneratableIds();
    }

    public void ReleaseCopy(string cardId, int copyNumber)
    {
        if (!states.TryGetValue(cardId, out var st)) return;
        if (copyNumber < 1 || copyNumber > st.maxConcurrent) return;

        int idx = copyNumber - 1;
        if (!st.inUse[idx]) return;

        st.inUse[idx] = false;
        st.aliveCount = Mathf.Max(0, st.aliveCount - 1);
    }

    private int GetFirstFreeCopyNumber(CardIdState st)
    {
        for (int i = 0; i < st.inUse.Length; i++)
            if (!st.inUse[i]) return i + 1;
        return -1;
    }

    private string PickRandomGeneratableId()
    {
        RefreshGeneratableIds();
        if (generatableIds.Count == 0) return null;
        return generatableIds[Random.Range(0, generatableIds.Count)];
    }

    private void RefreshGeneratableIds()
    {
        generatableIds.Clear();

        foreach (var kv in states)
        {
            var st = kv.Value;
            if (st.maxConcurrent <= 0) continue;

            if (st.aliveCount < st.maxConcurrent)
                generatableIds.Add(kv.Key);
        }
    }

    [ContextMenu("Debug: Print Pool State")]
    private void DebugPrint()
    {
        Debug.Log($"[CardPoolManager] exhausted={poolExhausted}");
        foreach (var kv in states)
        {
            var st = kv.Value;
            Debug.Log($"[Pool] {kv.Key}: alive={st.aliveCount}/{st.maxConcurrent}");
        }
    }
}