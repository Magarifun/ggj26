using System.Collections.Generic;
using UnityEngine;

public class CardsUnlocker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CardPoolManager pool;
    [SerializeField] private Camera cam;

    [Header("Spawn points (World)")]
    [SerializeField] private Transform choicePointA;
    [SerializeField] private Transform choicePointB;

    [Header("Optional parent for spawned choices")]
    [SerializeField] private Transform choicesRoot;

    [Header("Click / Raycast")]
    [Tooltip("Layer su cui metti SOLO le carte di scelta (raycast).")]
    [SerializeField] private LayerMask choiceLayer;

    [Tooltip("Setta il layer su tutta la gerarchia delle carte spawnate.")]
    [SerializeField] private bool setLayerRecursively = true;

    [Header("Stop Everything While Choosing")]
    [Tooltip("Se true, Time.timeScale=0 finché non scegli.")]
    [SerializeField] private bool pauseWithTimeScale = true;

    [Tooltip("Pausa anche l'audio globale (AudioListener.pause) finché scegli.")]
    [SerializeField] private bool pauseAudioListener = false;

    [Tooltip("Lista di script da disabilitare mentre sei in scelta (player controller, drag, pool manager, ecc.).")]
    [SerializeField] private MonoBehaviour[] disableWhileChoosing;

    [Header("Safety: disable gameplay scripts on spawned choices")]
    [SerializeField] private bool disableDragAndDropOnChoiceCards = true;
    [SerializeField] private bool disableLifecycleOnChoiceCards = true;

    private readonly List<GameObject> spawned = new();
    private bool isChoosing;

    private float prevTimeScale = 1f;
    private bool prevAudioPaused = false;

    // track per ripristinare gli script (nel caso alcuni fossero già disabilitati)
    private readonly Dictionary<MonoBehaviour, bool> prevEnabledState = new();

    private void Awake()
    {
        if (pool == null) pool = FindObjectOfType<CardPoolManager>();
        if (cam == null) cam = Camera.main;
        if (choicesRoot == null) choicesRoot = transform;
    }

    private void Update()
    {
        if (!isChoosing) return;
        if (cam == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(mouseWorld, choiceLayer);
            if (hit == null) return;

            var tag = hit.GetComponentInParent<CardUnlockChoiceTag>();
            if (tag == null) return;

            UnlockSelected(tag.cardId);
        }
    }

    // =========================================================
    // PUBLIC API
    // =========================================================

    public void Show2RandomLockedChoices()
    {
        if (pool == null)
        {
            Debug.LogError("[CardsUnlocker] CardPoolManager non trovato.");
            return;
        }

        if (choicePointA == null || choicePointB == null)
        {
            Debug.LogError("[CardsUnlocker] Assegna choicePointA e choicePointB.");
            return;
        }

        ClearChoices(); // chiude eventuale scelta precedente

        List<string> locked = pool.GetLockedCardIds();
        if (locked == null || locked.Count == 0)
        {
            Debug.Log("[CardsUnlocker] Nessuna carta locked disponibile.");
            return;
        }

        // pick 2 diversi (se possibile)
        string idA = PickAndRemoveRandom(locked);
        string idB = (locked.Count > 0) ? PickAndRemoveRandom(locked) : null;

        SpawnChoice(idA, choicePointA);
        if (!string.IsNullOrEmpty(idB))
            SpawnChoice(idB, choicePointB);

        // 🔥 STOPPA TUTTO IL RESTO
        EnterChoiceMode();

        Debug.Log($"[CardsUnlocker] Choices shown: {idA}" + (idB != null ? $" / {idB}" : ""));
    }

    public void ClearChoices()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
                Destroy(spawned[i]);
        }
        spawned.Clear();

        if (isChoosing)
            ExitChoiceMode();
    }

    // =========================================================
    // INTERNAL
    // =========================================================

    private void EnterChoiceMode()
    {
        isChoosing = true;

        // disable scripts
        prevEnabledState.Clear();
        if (disableWhileChoosing != null)
        {
            for (int i = 0; i < disableWhileChoosing.Length; i++)
            {
                var mb = disableWhileChoosing[i];
                if (mb == null) continue;

                prevEnabledState[mb] = mb.enabled;
                mb.enabled = false;
            }
        }

        // pause audio
        if (pauseAudioListener)
        {
            prevAudioPaused = AudioListener.pause;
            AudioListener.pause = true;
        }

        // pause time
        if (pauseWithTimeScale)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    private void ExitChoiceMode()
    {
        isChoosing = false;

        // resume time
        if (pauseWithTimeScale)
            Time.timeScale = prevTimeScale;

        // resume audio
        if (pauseAudioListener)
            AudioListener.pause = prevAudioPaused;

        // re-enable scripts to previous state
        foreach (var kv in prevEnabledState)
        {
            if (kv.Key == null) continue;
            kv.Key.enabled = kv.Value;
        }
        prevEnabledState.Clear();
    }

    private void SpawnChoice(string cardId, Transform point)
    {
        if (string.IsNullOrEmpty(cardId)) return;

        GameObject prefab = pool.GetPrefabById(cardId);
        if (prefab == null)
        {
            Debug.LogError($"[CardsUnlocker] Prefab non trovato per cardId '{cardId}'.");
            return;
        }

        GameObject go = Instantiate(prefab, point.position, point.rotation, choicesRoot);
        go.SetActive(true);
        go.name = $"UNLOCK_CHOICE_{cardId}";

        // tag per capire quale id hai cliccato
        var tag = go.GetComponent<CardUnlockChoiceTag>();
        if (tag == null) tag = go.AddComponent<CardUnlockChoiceTag>();
        tag.cardId = cardId;

        // disabilita script gameplay sulle carte scelta
        if (disableDragAndDropOnChoiceCards)
        {
            var drag = go.GetComponentInChildren<CardDragAndDrop2D_SnapSortingErase>(true);
            if (drag != null) drag.enabled = false;
        }

        if (disableLifecycleOnChoiceCards)
        {
            var life = go.GetComponent<CardLifecycle>();
            if (life != null) life.enabled = false;
        }

        // set layer per click
        int layerIndex = MaskToFirstLayerIndex(choiceLayer);
        if (layerIndex >= 0)
        {
            if (setLayerRecursively) SetLayerRecursive(go.transform, layerIndex);
            else go.layer = layerIndex;
        }

        spawned.Add(go);
    }

    private void UnlockSelected(string cardId)
    {
        pool.SetCardUnlocked(cardId, true);
        Debug.Log($"[CardsUnlocker] UNLOCKED -> {cardId}");

        // chiude scelta (distrugge entrambe + resume)
        ClearChoices();
    }

    private static string PickAndRemoveRandom(List<string> list)
    {
        int idx = Random.Range(0, list.Count);
        string v = list[idx];
        list.RemoveAt(idx);
        return v;
    }

    private static void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursive(t.GetChild(i), layer);
    }

    private static int MaskToFirstLayerIndex(LayerMask mask)
    {
        int v = mask.value;
        if (v == 0) return -1;
        for (int i = 0; i < 32; i++)
            if ((v & (1 << i)) != 0) return i;
        return -1;
    }

    // =========================================================
    // DEBUG BUTTONS (Inspector)
    // =========================================================

    [ContextMenu("DEBUG: Show 2 Random Locked Choices")]
    private void DebugShow() => Show2RandomLockedChoices();

    [ContextMenu("DEBUG: Clear Choices")]
    private void DebugClear() => ClearChoices();
}

public class CardUnlockChoiceTag : MonoBehaviour
{
    public string cardId;
}