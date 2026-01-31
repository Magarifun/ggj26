using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CardDragAndDrop2D_SnapSortingErase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cardDragger;     // child con Collider2D
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask draggableLayer;

    [Header("Board Snap")]
    [SerializeField] private BoardManager2D board;
    [SerializeField] private float snapDistance = 1.5f;

    [Header("Snap While Dragging")]
    [SerializeField] private bool snapWhileDragging = true;
    [SerializeField] private float dragSnapDistance = 2.5f;

    [Header("Tiles Overlap (2D)")]
    [SerializeField] private LayerMask tileLayer;       // layer delle tile (es: Tile2D)
    [SerializeField] private bool includeTriggers = true;

    [Header("Sorting")]
    [SerializeField] private int dragSortingDelta = 1;  // +1 durante drag
    [SerializeField] private int resetSortingOrder = 0; // torna a 0 dopo drop

    [Header("Overlap Precision")]
    [Tooltip("Più alto = elimina meno (più strict). 0.001 è un buon default.")]
    [SerializeField] private float overlapEpsilon = 0.001f;

    [Header("Startup Cleanup")]
    [SerializeField] private bool destroyTilesWithAlphaZeroOnStart = true;

    private Collider2D draggerCollider;
    private bool isDragging;
    private Vector3 offset;
    private float fixedZ;

    private BoardSlot2D currentSlot;

    private ContactFilter2D tileFilter;
    private readonly List<Collider2D> overlapResults = new List<Collider2D>(32);
    private readonly HashSet<Collider2D> toDestroy = new HashSet<Collider2D>();

    private Collider2D[] tileColliders;
    private SpriteRenderer[] tileRenderers;

    private Coroutine dropRoutine;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;

        if (cardDragger == null)
        {
            Transform found = transform.Find("CardDragger");
            if (found != null) cardDragger = found;
        }

        draggerCollider = cardDragger != null ? cardDragger.GetComponent<Collider2D>() : null;
        if (draggerCollider == null)
            Debug.LogError("CardDragger deve avere un Collider2D!");

        fixedZ = transform.position.z;

        tileFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = tileLayer,
            useTriggers = includeTriggers
        };
    }

    private void Start()
    {
        if (board == null)
            board = FindObjectOfType<BoardManager2D>();

        if (destroyTilesWithAlphaZeroOnStart)
            DestroyTilesWithAlphaZero();

        RefreshTileCache();
    }

    private void Update()
    {
        if (cam == null || draggerCollider == null) return;
        if (Mouse.current == null) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector2 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryBeginDrag(mouseWorld);

        if (isDragging && Mouse.current.leftButton.isPressed)
            Drag(mouseWorld);

        if (isDragging && Mouse.current.leftButton.wasReleasedThisFrame)
            EndDrag();
    }

    private void TryBeginDrag(Vector2 mouseWorld)
    {
        RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero, 0f, draggableLayer);
        if (hit.collider == null || hit.collider != draggerCollider)
            return;

        BeginDrag(mouseWorld);
    }

    private void BeginDrag(Vector2 mouseWorld)
    {
        isDragging = true;

        // stop eventuale routine di drop precedente
        if (dropRoutine != null)
        {
            StopCoroutine(dropRoutine);
            dropRoutine = null;
        }

        // libera slot corrente (solo questa card)
        ReleaseCurrentSlot();

        // offset drag
        Vector3 mouseWorld3D = new Vector3(mouseWorld.x, mouseWorld.y, fixedZ);
        offset = transform.position - mouseWorld3D;
        offset.z = 0f;

        // porta davanti
        AddSortingOrder(dragSortingDelta);
    }

    private void Drag(Vector2 mouseWorld)
    {
        Vector3 mouseWorld3D = new Vector3(mouseWorld.x, mouseWorld.y, fixedZ);
        Vector3 target = mouseWorld3D + offset;
        target.z = fixedZ;

        if (!snapWhileDragging || board == null)
        {
            transform.position = target;
            return;
        }

        // SNAP LIVE: anche su slot occupati
        BoardSlot2D slot = GetClosestSlot(target, dragSnapDistance);

        if (slot != null)
        {
            transform.position = new Vector3(slot.transform.position.x, slot.transform.position.y, fixedZ);
        }
        else
        {
            transform.position = target;
        }
    }

    private void EndDrag()
    {
        isDragging = false;

        // snap finale: preferisci libero
        SnapThisCardPreferFree();

        // distruzione dopo update fisica
        dropRoutine = StartCoroutine(DestroyAfterPhysicsUpdate());
    }

   private IEnumerator DestroyAfterPhysicsUpdate()
{
    yield return new WaitForFixedUpdate();

    DestroyTilesStillUnderMe();

    // 🔥 notifica: card piazzata
    GetComponent<CardLifeCycle>()?.MarkPlaced();

    // 🔥 notifica: se ha perso tile, rientra nel pool
    GetComponent<CardLifeCycle>()?.CheckIfLostTiles();

    SetSortingOrder(resetSortingOrder);

    dropRoutine = null;
}

    // ---------- SNAP LOGIC ----------

    private BoardSlot2D GetClosestSlot(Vector3 position, float maxDistance)
    {
        if (board == null || board.Slots == null || board.Slots.Count == 0) return null;

        BoardSlot2D best = null;
        float bestDist = float.MaxValue;

        foreach (var slot in board.Slots)
        {
            if (slot == null) continue;

            float d = Vector2.Distance(position, slot.transform.position);
            if (d < bestDist && d <= maxDistance)
            {
                bestDist = d;
                best = slot;
            }
        }

        return best;
    }

    private void SnapThisCardPreferFree()
    {
        if (board == null) return;

        // prima prova libero
        BoardSlot2D free = board.GetClosestFreeSlot(transform.position, snapDistance);
        if (free != null)
        {
            transform.position = new Vector3(free.transform.position.x, free.transform.position.y, fixedZ);
            free.occupied = true;
            currentSlot = free;
            return;
        }

        // se non libero, resta dove sei (ma già snappato live)
        // opzionale: potresti occupare comunque lo slot più vicino
        BoardSlot2D any = GetClosestSlot(transform.position, snapDistance);
        if (any != null)
        {
            transform.position = new Vector3(any.transform.position.x, any.transform.position.y, fixedZ);
            // NON setto occupied perché era già occupato da qualcun altro
        }
    }

    // ---------- DESTROY LOGIC ----------

    private void DestroyTilesStillUnderMe()
    {
        if (tileColliders == null || tileColliders.Length == 0) return;

        toDestroy.Clear();

        foreach (var myTile in tileColliders)
        {
            if (myTile == null) continue;

            SpriteRenderer mySR = myTile.GetComponent<SpriteRenderer>();
            if (mySR == null) continue;

            overlapResults.Clear();
            myTile.Overlap(tileFilter, overlapResults);

            for (int i = 0; i < overlapResults.Count; i++)
            {
                Collider2D hit = overlapResults[i];
                if (hit == null) continue;

                // ignora tile della stessa card
                if (hit.transform.IsChildOf(transform))
                    continue;

                SpriteRenderer hitSR = hit.GetComponent<SpriteRenderer>();
                if (hitSR == null) continue;

                // deve essere sotto in sortingOrder
                if (hitSR.sortingOrder >= mySR.sortingOrder)
                    continue;

                // deve essere visibile
                if (hitSR.color.a <= 0f)
                    continue;

                // overlap reale (evita adiacenti)
                ColliderDistance2D d = myTile.Distance(hit);
                if (!d.isOverlapped || d.distance > -overlapEpsilon)
                    continue;

                toDestroy.Add(hit);
            }
        }

        foreach (var col in toDestroy)
        {
            if (col == null) continue;
            Destroy(col.gameObject);
        }

        // refresh cache dopo le distruzioni
        RefreshTileCache();
    }

    private void DestroyTilesWithAlphaZero()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);

        foreach (var sr in renderers)
        {
            if (sr == null) continue;

            float alphaSprite = sr.color.a;

            float alphaMat = 1f;
            if (sr.material != null && sr.material.HasProperty("_Color"))
                alphaMat = sr.material.color.a;

            if (alphaSprite <= 0f || alphaMat <= 0f)
            {
                Destroy(sr.gameObject);
            }
        }
    }

    private void RefreshTileCache()
    {
        tileColliders = GetComponentsInChildren<Collider2D>(true);
        tileRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    // ---------- SLOT / SORTING ----------

    private void ReleaseCurrentSlot()
    {
        if (currentSlot == null) return;

        currentSlot.occupied = false;
        currentSlot = null;
    }

    private void AddSortingOrder(int delta)
    {
        if (tileRenderers == null || tileRenderers.Length == 0)
            tileRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        foreach (var sr in tileRenderers)
        {
            if (sr == null) continue;
            sr.sortingOrder += delta;
        }
    }

    private void SetSortingOrder(int value)
    {
        if (tileRenderers == null || tileRenderers.Length == 0)
            tileRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        foreach (var sr in tileRenderers)
        {
            if (sr == null) continue;
            sr.sortingOrder = value;
        }
    }
}