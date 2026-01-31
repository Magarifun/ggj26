using System.Collections.Generic;
using UnityEngine;

public class CardPooler : MonoBehaviour
{
    [System.Serializable]
    public class CardPrefabData
    {
        public GameObject prefab;
        public int maxCopiesInDeck = 3; // fallback se manca CardPoolEntry
    }

    [Header("Pool Prefabs")]
    public List<CardPrefabData> cardPrefabs = new List<CardPrefabData>();

    [Header("Hand Settings")]
    public int handSize = 3;
    public Transform[] handSlots;
    public Transform handRoot;

    private readonly Dictionary<string, int> maxCopies = new();
    private readonly Dictionary<string, int> activeCopies = new();

    private void Start()
    {
        if (handRoot == null) handRoot = transform;

        BuildDeckData();
        FillHand();
    }

    private void BuildDeckData()
    {
        maxCopies.Clear();
        activeCopies.Clear();

        foreach (var data in cardPrefabs)
        {
            if (data.prefab == null) continue;

            string id = GetCardId(data.prefab);

            int max = data.maxCopiesInDeck;
            var entry = data.prefab.GetComponent<CardPoolEntry>();
            if (entry != null) max = entry.maxCopiesInDeck;

            if (!maxCopies.ContainsKey(id))
            {
                maxCopies[id] = max;
                activeCopies[id] = 0;
            }
        }
    }

    private void FillHand()
    {
        for (int i = 0; i < handSize; i++)
            SpawnIntoHandSlot(i);
    }

    private void SpawnIntoHandSlot(int slotIndex)
    {
        if (handSlots == null || slotIndex >= handSlots.Length) return;

        GameObject prefab = GetRandomAvailablePrefab();
        if (prefab == null)
        {
            // ✅ finito il “deck”: non generare più
            Debug.Log($"[CardPooler] Nessuna carta disponibile. Slot {slotIndex} resta vuoto.");
            return;
        }

        Transform slot = handSlots[slotIndex];

        // istanzia in posizione slot, poi metti sotto handRoot
        GameObject cardGO = Instantiate(prefab, slot.position, slot.rotation);
        cardGO.SetActive(true);
        cardGO.transform.SetParent(handRoot, true);

        // incrementa conteggio attivo
        string id = GetCardId(cardGO);
        EnsureIdExists(id);
        activeCopies[id]++;

        // lifecycle
        var life = cardGO.GetComponent<CardLifeCycle>();
        if (life == null) life = cardGO.AddComponent<CardLifeCycle>();

        life.OnPlaced += HandleCardPlaced;
        life.OnLostTilesAfterPlaced += HandleCardLostTiles;

        // memorizza slot origine
        var slotRef = cardGO.GetComponent<CardHandSlotRef>();
        if (slotRef == null) slotRef = cardGO.AddComponent<CardHandSlotRef>();
        slotRef.slotIndex = slotIndex;
    }

    private GameObject GetRandomAvailablePrefab()
    {
        List<GameObject> candidates = new();

        foreach (var data in cardPrefabs)
        {
            if (data.prefab == null) continue;

            string id = GetCardId(data.prefab);
            EnsureIdExists(id);

            if (activeCopies[id] < maxCopies[id])
                candidates.Add(data.prefab);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private void HandleCardPlaced(CardLifeCycle life)
    {
        // refill lo slot di origine
        var slotRef = life.GetComponent<CardHandSlotRef>();
        int slotIndex = slotRef != null ? slotRef.slotIndex : -1;

        if (slotIndex >= 0)
            SpawnIntoHandSlot(slotIndex);
    }

    private void HandleCardLostTiles(CardLifeCycle life)
    {
        // ✅ libera UNA copia di quel tipo (rientra nel pool)
        string id = GetCardId(life.gameObject);
        EnsureIdExists(id);

        activeCopies[id] = Mathf.Max(0, activeCopies[id] - 1);
    }

    private void EnsureIdExists(string id)
    {
        if (!maxCopies.ContainsKey(id))
        {
            // Se succede, significa che hai prefab senza CardPoolEntry o id non coerenti
            Debug.LogWarning($"[CardPooler] ID '{id}' non registrato nel deck. Lo aggiungo con max=0 (non spawnabile).");
            maxCopies[id] = 0;
            activeCopies[id] = 0;
        }
        else if (!activeCopies.ContainsKey(id))
        {
            activeCopies[id] = 0;
        }
    }

    private string GetCardId(GameObject go)
    {
        var entry = go.GetComponent<CardPoolEntry>();
        if (entry != null && !string.IsNullOrEmpty(entry.cardId))
            return entry.cardId;

        // fallback sicuro se manca CardPoolEntry
        return NormalizeName(go.name);
    }

    private string NormalizeName(string n)
    {
        // rimuove "(Clone)" e spazi finali
        return n.Replace("(Clone)", "").Trim();
    }
}