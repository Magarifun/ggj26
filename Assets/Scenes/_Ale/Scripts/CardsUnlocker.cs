using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CardsUnlocker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CardPoolManager pool;
    [SerializeField] private Camera cam;
    [SerializeField] private GameObject curtain;

    [Header("Spawn points (World)")]
    [SerializeField] private Transform choicePointA;
    [SerializeField] private Transform choicePointB;

    [Header("Background")]
    [SerializeField] private GameObject backgroundPrefab;   // ✅ terzo render
    [SerializeField] private Transform backgroundPoint;     // ✅ terzo transform

    [Header("Optional parent for spawned choices")]
    [SerializeField] private Transform choicesRoot;

    [Header("Click / Raycast")]
    [Tooltip("Layer su cui metti SOLO le carte di scelta (raycast).")]
    [SerializeField] private LayerMask choiceLayer;

    [Tooltip("Setta il layer su tutta la gerarchia delle carte spawnate.")]
    [SerializeField] private bool setLayerRecursively = true;

    [Header("Stop Everything While Choosing")]
    [SerializeField] private bool pauseWithTimeScale = true;
    [SerializeField] private bool pauseAudioListener = false;
    [SerializeField] private MonoBehaviour[] disableWhileChoosing;

    [Header("Hide other cards while choosing")]
    [SerializeField] private Transform[] rootsToHide;
    [SerializeField] private bool disableRootsToo = false;

    [Header("Safety: disable gameplay scripts on spawned choices")]
    [SerializeField] private bool disableDragAndDropOnChoiceCards = true;
    [SerializeField] private bool disableLifecycleOnChoiceCards = true;

    private readonly List<GameObject> spawned = new();
    private bool isChoosing;

    private float prevTimeScale = 1f;
    private bool prevAudioPaused = false;

    private readonly Dictionary<MonoBehaviour, bool> prevEnabledState = new();
    private readonly Dictionary<GameObject, bool> prevActiveState = new();

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

        ClearChoices();

        List<string> locked = pool.GetLockedCardIds();
        if (locked == null || locked.Count == 0)
        {
            Debug.Log("[CardsUnlocker] Nessuna carta locked disponibile.");
            return;
        }

        // ✅ background prima (così sta dietro)
        SpawnBackground();

        // pick 2 diversi (se possibile)
        string idA = PickAndRemoveRandom(locked);
        string idB = (locked.Count > 0) ? PickAndRemoveRandom(locked) : null;

        SpawnChoice(idA, choicePointA);
        if (!string.IsNullOrEmpty(idB))
            SpawnChoice(idB, choicePointB);

        EnterChoiceMode();
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

    // ----------------- Choice mode -----------------

    private void EnterChoiceMode()
    {
        isChoosing = true;

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

        HideRootsChildren();
        KeyBubble.Instance.Display(false);
        DisplayBallLights(false);
        curtain.SetActive(true);

        if (pauseAudioListener)
        {
            prevAudioPaused = AudioListener.pause;
            AudioListener.pause = true;
        }

        if (pauseWithTimeScale)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0.01f;
        }
    }

    private void ExitChoiceMode()
    {
        isChoosing = false;

        if (pauseWithTimeScale)
            Time.timeScale = prevTimeScale;

        if (pauseAudioListener)
            AudioListener.pause = prevAudioPaused;

        RestoreHiddenObjects();
        KeyBubble.Instance.Display(true);
        DisplayBallLights(true);
        curtain.SetActive(false);

        foreach (var kv in prevEnabledState)
        {
            if (kv.Key == null) continue;
            kv.Key.enabled = kv.Value;
        }
        prevEnabledState.Clear();
    }

    // ----------------- Hide / Show -----------------

    private void HideRootsChildren()
    {
        prevActiveState.Clear();

        if (rootsToHide == null || rootsToHide.Length == 0)
            return;

        for (int r = 0; r < rootsToHide.Length; r++)
        {
            var root = rootsToHide[r];
            if (root == null) continue;

            if (disableRootsToo)
                CacheAndSetActive(root.gameObject, false);

            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child == null) continue;

                CacheAndSetActive(child.gameObject, false);
            }
        }
    }

    private void DisplayBallLights(bool state)
    {
        foreach (Ball ball in GameObject.FindObjectsByType<Ball>(FindObjectsSortMode.None))
        {
            ball.GetComponentInChildren<Light2D>(includeInactive: true).enabled = state;
        }
    }

    private void RestoreHiddenObjects()
    {
        foreach (var kv in prevActiveState)
        {
            if (kv.Key == null) continue;
            kv.Key.SetActive(kv.Value);
        }
        prevActiveState.Clear();
    }

    private void CacheAndSetActive(GameObject go, bool active)
    {
        if (go == null) return;
        if (!prevActiveState.ContainsKey(go))
            prevActiveState.Add(go, go.activeSelf);

        go.SetActive(active);
    }

    // ----------------- Spawning -----------------

    private void SpawnBackground()
    {
        if (backgroundPrefab == null) return;

        Transform point = backgroundPoint != null ? backgroundPoint : choicesRoot;
        GameObject bg = Instantiate(backgroundPrefab, point.position, point.rotation, choicesRoot);
        bg.SetActive(true);
        bg.name = "UNLOCK_BG";

        // se vuoi che bg sia cliccabile? no. Quindi NON mettere choiceLayer.
        // ma se hai layer specifico per UI/overlay, lo setti qui.
        spawned.Add(bg);
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
        go.transform.localPosition += Vector3.back;
        go.SetActive(true);
        go.name = $"UNLOCK_CHOICE_{cardId}";

        var tag = go.GetComponent<CardUnlockChoiceTag>();
        if (tag == null) tag = go.AddComponent<CardUnlockChoiceTag>();
        tag.cardId = cardId;

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

        int layerIndex = MaskToFirstLayerIndex(choiceLayer);
        if (layerIndex >= 0)
        {
            if (setLayerRecursively) SetLayerRecursive(go.transform, layerIndex);
            else go.layer = layerIndex;
        }

        spawned.Add(go);
    }

    private void DestroyAllBallsInScene()
{
    // Trova tutti i Ball (inclusi quelli clonati) e distrugge il loro GameObject
    Ball[] balls = GameObject.FindObjectsByType<Ball>(FindObjectsSortMode.None);
    for (int i = 0; i < balls.Length; i++)
    {
        if (balls[i] == null) continue;
        Destroy(balls[i].gameObject);
    }
}

    private void UnlockSelected(string cardId)
    {
        pool.SetCardUnlocked(cardId, true);
        ClearChoices();
        DestroyAllBallsInScene();
    }

    // ----------------- Utils -----------------

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

    [ContextMenu("DEBUG: Show 2 Random Locked Choices")]
    private void DebugShow() => Show2RandomLockedChoices();

    [ContextMenu("DEBUG: Clear Choices")]
    private void DebugClear() => ClearChoices();
}

public class CardUnlockChoiceTag : MonoBehaviour
{
    public string cardId;
}