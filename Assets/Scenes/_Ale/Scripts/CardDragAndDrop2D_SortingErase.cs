using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CardDragAndDrop2D_SnapSortingErase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cardDragger;     // child con Collider2D
    [SerializeField] private Transform snapAnchor;      // child "SnapAnchor" nel prefab
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask draggableLayer;

    [Header("Board Snap")]
    [SerializeField] private BoardManager2D board;
    [SerializeField] private float snapDistance = 1.5f;

    [Header("Snap While Dragging")]
    [SerializeField] private bool snapWhileDragging = true;
    [SerializeField] private float dragSnapDistance = 2.5f;

    [Header("Tiles Overlap (2D)")]
    [SerializeField] private LayerMask tileLayer;
    [SerializeField] private bool includeTriggers = true;

    [Header("Sorting")]
    [SerializeField] private int dragSortingDelta = 1;
    [SerializeField] private int resetSortingOrder = 0;

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

    // ✅ SOLO TILE (filtrate su tileLayer)
    private Collider2D[] tileColliders;
    private SpriteRenderer[] tileRenderers;

    private Coroutine dropRoutine;

    // Cache lifecycle (per pooler)
    private CardLifecycle lifecycle;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;

        if (cardDragger == null)
        {
            Transform found = transform.Find("CardDragger");
            if (found != null) cardDragger = found;
        }

        if (snapAnchor == null)
        {
            Transform found = transform.Find("SnapAnchor");
            if (found != null) snapAnchor = found;
        }

        draggerCollider = cardDragger != null ? cardDragger.GetComponent<Collider2D>() : null;
        if (draggerCollider == null)
            Debug.LogError("CardDragger deve avere un Collider2D!");

        lifecycle = GetComponent<CardLifecycle>(); // può essere null se non c'è (ok)

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

        if (dropRoutine != null)
        {
            StopCoroutine(dropRoutine);
            dropRoutine = null;
        }

        ReleaseCurrentSlot();

        Vector3 mouseWorld3D = new Vector3(mouseWorld.x, mouseWorld.y, fixedZ);
        offset = transform.position - mouseWorld3D;
        offset.z = 0f;

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

        BoardSlot2D slot = GetClosestSlot(target, dragSnapDistance);
        if (slot != null)
        {
            MoveCardToSlotPosition(slot.transform.position);
            Physics2D.SyncTransforms();
        }
        else
        {
            transform.position = target;
        }
    }

    private void EndDrag()
    {
        isDragging = false;

        // ✅ se snappa davvero => è "placed" (pooler respawn)
        bool snapped = SnapThisCardPreferFree();
        Physics2D.SyncTransforms();

        if (snapped)
        {
            // se non c'è già, puoi anche aggiungerlo automaticamente:
            if (lifecycle == null) lifecycle = GetComponent<CardLifecycle>();
            lifecycle?.MarkPlaced();
        }

        dropRoutine = StartCoroutine(DestroyAfterPhysicsUpdate());
    }

    private IEnumerator DestroyAfterPhysicsUpdate()
    {
        yield return new WaitForFixedUpdate();

        DestroyTilesStillUnderMe();

        // dopo aver distrutto tile, controlla se ha perso tile (per reintrodurre nel pool)
        if (lifecycle == null) lifecycle = GetComponent<CardLifecycle>();
        lifecycle?.CheckIfLostTiles();

        SetSortingOrder(resetSortingOrder);

        dropRoutine = null;
    }

    // ---------- SNAP ----------

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

    /// <summary>
    /// Snappa preferendo slot libero. Ritorna TRUE se ha snappato a uno slot valido (libero o qualunque).
    /// </summary>
    private bool SnapThisCardPreferFree()
    {
        if (board == null) return false;

        BoardSlot2D free = board.GetClosestFreeSlot(transform.position, snapDistance);
        if (free != null)
        {
            MoveCardToSlotPosition(free.transform.position);
            free.occupied = true;
            currentSlot = free;
            return true;
        }

        BoardSlot2D any = GetClosestSlot(transform.position, snapDistance);
        if (any != null)
        {
            MoveCardToSlotPosition(any.transform.position);
            return true;
        }

        return false;
    }

    private void MoveCardToSlotPosition(Vector3 slotPosition)
    {
        Vector3 desired = new Vector3(slotPosition.x, slotPosition.y, fixedZ);

        if (snapAnchor == null)
        {
            transform.position = desired;
            return;
        }

        Vector3 delta = desired - snapAnchor.position;
        transform.position += new Vector3(delta.x, delta.y, 0f);
    }

    // ---------- DESTROY ----------

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

                if (hit.transform.IsChildOf(transform))
                    continue;

                SpriteRenderer hitSR = hit.GetComponent<SpriteRenderer>();
                if (hitSR == null) continue;

                if (hitSR.sortingOrder >= mySR.sortingOrder)
                    continue;

                if (hitSR.color.a <= 0f)
                    continue;

                ColliderDistance2D d = myTile.Distance(hit);

                toDestroy.Add(hit);
            }
        }

        foreach (var col in toDestroy)
        {
            if (col == null) continue;
            Destroy(col.gameObject);
        }

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
                Destroy(sr.gameObject);
        }
    }

    // ✅ Cache SOLO tile (layer filter)
    private void RefreshTileCache()
    {
        var allCols = GetComponentsInChildren<Collider2D>(true);
        var cols = new List<Collider2D>(allCols.Length);

        for (int i = 0; i < allCols.Length; i++)
        {
            var c = allCols[i];
            if (c == null) continue;

            if (IsInLayerMask(c.gameObject.layer, tileLayer))
                cols.Add(c);
        }

        tileColliders = cols.ToArray();

        var allSR = GetComponentsInChildren<SpriteRenderer>(true);
        var srs = new List<SpriteRenderer>(allSR.Length);

        for (int i = 0; i < allSR.Length; i++)
        {
            var sr = allSR[i];
            if (sr == null) continue;

            if (IsInLayerMask(sr.gameObject.layer, tileLayer))
                srs.Add(sr);
        }

        tileRenderers = srs.ToArray();
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
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
            RefreshTileCache();

        foreach (var sr in tileRenderers)
        {
            if (sr == null) continue;
            sr.sortingOrder += delta;
        }
    }

    private void SetSortingOrder(int value)
    {
        if (tileRenderers == null || tileRenderers.Length == 0)
            RefreshTileCache();

        foreach (var sr in tileRenderers)
        {
            if (sr == null) continue;
            sr.sortingOrder = value;
        }
    }
}