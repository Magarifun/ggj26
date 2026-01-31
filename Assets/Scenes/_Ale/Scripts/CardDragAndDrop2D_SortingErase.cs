using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CardDragAndDrop2D_SnapSortingErase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cardDragger;
    [SerializeField] private Transform snapAnchor;
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask draggableLayer;

    [Header("Board Snap")]
    [SerializeField] private BoardManager2D board;
    [SerializeField] private float snapDistance = 1.5f;

    [Header("Snap While Dragging")]
    [SerializeField] private bool snapWhileDragging = true;
    [SerializeField] private float dragSnapDistance = 2.5f;

    [Header("Snap Limits (World Space)")]
    [SerializeField] private bool useSnapLimits = true;
    [SerializeField] private Vector2 snapAreaMin = new Vector2(-5, -3);
    [SerializeField] private Vector2 snapAreaMax = new Vector2(5, 3);

    [Header("Limits Behaviour")]
    [SerializeField] private bool hardClampToLimits = true;

    [Header("Limits Tolerance")]
    [SerializeField] private float insideTolerance = 0.02f;

    [Header("Tiles Overlap (2D)")]
    [SerializeField] private LayerMask tileLayer;
    [SerializeField] private bool includeTriggers = true;

    [Header("Sorting")]
    [SerializeField] private int dragSortingDelta = 1;
    [SerializeField] private int resetSortingOrder = 0;

    [Header("Overlap Precision")]
    [SerializeField] private float overlapEpsilon = 0.001f;

    [Header("Startup Cleanup")]
    [SerializeField] private bool destroyTilesWithAlphaZeroOnStart = true;

    [Header("Lock After Placed")]
    [SerializeField] private bool lockAfterPlaced = true;

    [Header("Disable Level-3 Colliders While Dragging")]
    [Tooltip("Disabilita i Collider2D a profondità 3 sotto la Card: Card(0) > CardVariant(1) > TileX(2) > COMPONENT(3)")]
    [SerializeField] private bool disableLevel3CollidersWhileDragging = true;

    [Header("Debug")]
    [SerializeField] private bool drawSnapAreaGizmos = true;

    // ===== DEBUG LIVE (Inspector) =====
    [Header("DEBUG LIVE (Inspector)")]
    [SerializeField] private bool debugLive = true;

    [SerializeField] private bool dbg_hasTileBounds;
    [SerializeField] private bool dbg_predictedFullyInside;
    [SerializeField] private bool dbg_wallActiveThisFrame;

    [SerializeField] private Vector2 dbg_snapMin;
    [SerializeField] private Vector2 dbg_snapMax;

    [SerializeField] private Vector2 dbg_tileBoundsMin;
    [SerializeField] private Vector2 dbg_tileBoundsMax;

    [SerializeField] private Vector2 dbg_predictedBoundsMin;
    [SerializeField] private Vector2 dbg_predictedBoundsMax;

    [SerializeField] private float dbg_tolerance;

    // ===== internal =====
    private Collider2D draggerCollider;
    private bool isDragging;
    private Vector3 offset;
    private float fixedZ;

    public bool isPlaced = false;
    private bool wallEngaged = false;

    private ContactFilter2D tileFilter;
    private readonly List<Collider2D> overlapResults = new List<Collider2D>(32);
    private readonly HashSet<Collider2D> toDestroy = new HashSet<Collider2D>();
    private readonly HashSet<CardLifecycle> affectedCards = new HashSet<CardLifecycle>();

    private Collider2D[] tileColliders;
    private SpriteRenderer[] tileRenderers;

    private Collider2D[] level3ChildColliders;

    private Coroutine dropRoutine;

    // --- EVENTS STATE ---
    private Collider2D hoveredTile;
    private BoardSlot2D lastSnappedSlot;

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
            Debug.LogError("[CardDragAndDrop] CardDragger deve avere un Collider2D!");

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
        CacheLevel3ChildColliders();
    }

    private void Update()
    {
        if (cam == null || draggerCollider == null) return;
        if (Mouse.current == null) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector2 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);

        // Hover events: solo quando non stai trascinando
        if (!isDragging)
            UpdateHover(mouseWorld);
        else
            ClearHoverIfAny();

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryBeginDrag(mouseWorld);

        if (isDragging && Mouse.current.leftButton.isPressed)
            Drag(mouseWorld);

        if (isDragging && Mouse.current.leftButton.wasReleasedThisFrame)
            EndDrag();
    }

    private void TryBeginDrag(Vector2 mouseWorld)
    {
        if (lockAfterPlaced && isPlaced) return;

        RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero, 0f, draggableLayer);
        if (hit.collider == null || hit.collider != draggerCollider)
            return;

        BeginDrag(mouseWorld);
    }

    private void BeginDrag(Vector2 mouseWorld)
    {
        if (lockAfterPlaced && isPlaced) return;

        isDragging = true;
        wallEngaged = false;

        if (dropRoutine != null)
        {
            StopCoroutine(dropRoutine);
            dropRoutine = null;
        }

        Vector3 mouseWorld3D = new Vector3(mouseWorld.x, mouseWorld.y, fixedZ);
        offset = transform.position - mouseWorld3D;
        offset.z = 0f;

        AddSortingOrder(dragSortingDelta);

        // Disabilita collider depth 3 (Card > CardVariant > TileX > COMPONENT) mentre trascini
        if (disableLevel3CollidersWhileDragging)
            SetLevel3CollidersEnabled(false);

        // Hover off mentre trascini
        ClearHoverIfAny();

        // EVENT: grabbed
        CardTileEventManager.I?.RaiseGrabbed(this);
    }

    private void Drag(Vector2 mouseWorld)
    {
        Vector3 mouseWorld3D = new Vector3(mouseWorld.x, mouseWorld.y, fixedZ);
        Vector3 target = mouseWorld3D + offset;
        target.z = fixedZ;

        if (useSnapLimits && hardClampToLimits)
        {
            if (!wallEngaged)
            {
                if (WouldBeFullyInside(target))
                    wallEngaged = true;
            }

            if (wallEngaged)
                target = ClampCardPositionToSnapArea(target);
        }

        if (!snapWhileDragging || board == null || !IsWithinSnapArea(target))
        {
            transform.position = target;
            return;
        }

        BoardSlot2D slot = GetClosestSlot(target, dragSnapDistance);
        if (slot != null)
        {
            MoveCardToSlotPosition(slot.transform.position);

            if (useSnapLimits && hardClampToLimits && wallEngaged)
                transform.position = ClampCardPositionToSnapArea(transform.position);

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
        lastSnappedSlot = null;

        if (useSnapLimits && hardClampToLimits)
            transform.position = ClampCardPositionToSnapArea(transform.position);

        bool snapped = SnapToClosestSlotAllowOccupied_WithinLimits();
        Physics2D.SyncTransforms();

        if (snapped)
        {
            GetComponent<CardLifecycle>()?.MarkPlaced();

            if (lockAfterPlaced)
            {
                isPlaced = true;

                if (draggerCollider != null)
                    draggerCollider.enabled = false;
            }

            // EVENT: placed
            CardTileEventManager.I?.RaisePlaced(this, lastSnappedSlot);
        }

        // Riabilita collider depth 3 dopo il drag
        if (disableLevel3CollidersWhileDragging)
            SetLevel3CollidersEnabled(true);

        dropRoutine = StartCoroutine(DestroyAfterPhysicsUpdate());
    }

    private IEnumerator DestroyAfterPhysicsUpdate()
    {
        yield return new WaitForFixedUpdate();

        DestroyTilesStillUnderMe();

        Physics2D.SyncTransforms();
        SetSortingOrder(resetSortingOrder);

        dropRoutine = null;
    }

    // ---------- HOVER EVENTS ----------

    private void UpdateHover(Vector2 mouseWorld)
    {
        // Hover solo prima di essere piazzata
        if (lockAfterPlaced && isPlaced)
        {
            ClearHoverIfAny();
            return;
        }

        Collider2D hit = Physics2D.OverlapPoint(mouseWorld, tileLayer);

        // Se includeTriggers = false, ignoro trigger
        if (hit != null && !includeTriggers && hit.isTrigger)
            hit = null;

        // Deve essere un tile della MIA card
        if (hit != null && !hit.transform.IsChildOf(transform))
            hit = null;

        // Ignora tile invisibili
        if (hit != null)
        {
            var sr = hit.GetComponent<SpriteRenderer>();
            if (sr != null && sr.color.a <= 0f)
                hit = null;
        }

        if (hit == hoveredTile) return;

        if (hoveredTile != null)
            CardTileEventManager.I?.RaiseHoverExit(this, hoveredTile);

        hoveredTile = hit;

        if (hoveredTile != null)
            CardTileEventManager.I?.RaiseHoverEnter(this, hoveredTile);
    }

    private void ClearHoverIfAny()
    {
        if (hoveredTile == null) return;

        CardTileEventManager.I?.RaiseHoverExit(this, hoveredTile);
        hoveredTile = null;
    }

    // ---------- LIMITS ----------

    private bool IsWithinSnapArea(Vector3 worldPos)
    {
        if (!useSnapLimits) return true;

        return worldPos.x >= snapAreaMin.x && worldPos.x <= snapAreaMax.x &&
               worldPos.y >= snapAreaMin.y && worldPos.y <= snapAreaMax.y;
    }

    private bool TryGetTileBounds(out Bounds bounds)
    {
        bounds = new Bounds(transform.position, Vector3.zero);

        if (tileRenderers == null || tileRenderers.Length == 0)
            RefreshTileCache();

        bool hasAny = false;

        foreach (var sr in tileRenderers)
        {
            if (sr == null) continue;
            if (!IsInLayerMask(sr.gameObject.layer, tileLayer)) continue;

            if (!hasAny)
            {
                bounds = sr.bounds;
                hasAny = true;
            }
            else
            {
                bounds.Encapsulate(sr.bounds);
            }
        }

        return hasAny;
    }

    private bool BoundsFullyInsideSnapArea(Bounds b)
    {
        float minX = snapAreaMin.x + insideTolerance;
        float minY = snapAreaMin.y + insideTolerance;
        float maxX = snapAreaMax.x - insideTolerance;
        float maxY = snapAreaMax.y - insideTolerance;

        return b.min.x >= minX && b.max.x <= maxX &&
               b.min.y >= minY && b.max.y <= maxY;
    }

    private bool WouldBeFullyInside(Vector3 desiredCardPos)
    {
        dbg_wallActiveThisFrame = false;

        if (!TryGetTileBounds(out Bounds currentBounds))
        {
            if (debugLive) UpdateDebugBounds(false, currentBounds, currentBounds, false);
            return false;
        }

        Vector3 delta = desiredCardPos - transform.position;
        Bounds predicted = currentBounds;
        predicted.center += delta;

        bool fullyInside = BoundsFullyInsideSnapArea(predicted);

        if (debugLive)
            UpdateDebugBounds(true, currentBounds, predicted, fullyInside);

        dbg_wallActiveThisFrame = fullyInside;
        return fullyInside;
    }

    private Vector3 ClampCardPositionToSnapArea(Vector3 desiredCardPos)
    {
        if (!useSnapLimits) return desiredCardPos;

        if (!TryGetTileBounds(out Bounds tileBounds))
            return desiredCardPos;

        Vector3 delta = desiredCardPos - transform.position;
        Bounds predicted = tileBounds;
        predicted.center += delta;

        float shiftX = 0f;
        float shiftY = 0f;

        if (predicted.min.x < snapAreaMin.x)
            shiftX += snapAreaMin.x - predicted.min.x;
        if (predicted.max.x > snapAreaMax.x)
            shiftX -= predicted.max.x - snapAreaMax.x;

        if (predicted.min.y < snapAreaMin.y)
            shiftY += snapAreaMin.y - predicted.min.y;
        if (predicted.max.y > snapAreaMax.y)
            shiftY -= predicted.max.y - snapAreaMax.y;

        Vector3 clamped = desiredCardPos + new Vector3(shiftX, shiftY, 0f);
        clamped.z = fixedZ;
        return clamped;
    }

    private void UpdateDebugBounds(bool hasBounds, Bounds current, Bounds predicted, bool fullyInside)
    {
        dbg_hasTileBounds = hasBounds;
        dbg_predictedFullyInside = fullyInside;
        dbg_tolerance = insideTolerance;

        dbg_snapMin = snapAreaMin;
        dbg_snapMax = snapAreaMax;

        if (!hasBounds)
        {
            dbg_tileBoundsMin = Vector2.zero;
            dbg_tileBoundsMax = Vector2.zero;
            dbg_predictedBoundsMin = Vector2.zero;
            dbg_predictedBoundsMax = Vector2.zero;
            return;
        }

        dbg_tileBoundsMin = new Vector2(current.min.x, current.min.y);
        dbg_tileBoundsMax = new Vector2(current.max.x, current.max.y);

        dbg_predictedBoundsMin = new Vector2(predicted.min.x, predicted.min.y);
        dbg_predictedBoundsMax = new Vector2(predicted.max.x, predicted.max.y);
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

    private bool SnapToClosestSlotAllowOccupied_WithinLimits()
    {
        if (board == null) return false;
        if (!IsWithinSnapArea(transform.position)) return false;

        BoardSlot2D slot = GetClosestSlot(transform.position, snapDistance);
        if (slot == null) return false;

        lastSnappedSlot = slot;

        MoveCardToSlotPosition(slot.transform.position);

        if (useSnapLimits && hardClampToLimits)
            transform.position = ClampCardPositionToSnapArea(transform.position);

        return true;
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
        affectedCards.Clear();

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
                if (!d.isOverlapped || d.distance > -overlapEpsilon)
                    continue;

                CardLifecycle otherLife = hit.GetComponentInParent<CardLifecycle>();
                if (otherLife != null)
                    affectedCards.Add(otherLife);

                toDestroy.Add(hit);
            }
        }

        foreach (var col in toDestroy)
        {
            if (col == null) continue;
            Destroy(col.gameObject);
        }

        foreach (var life in affectedCards)
        {
            if (life == null) continue;
            life.NotifyLostTile();
        }

        RefreshTileCache();
        CacheLevel3ChildColliders();
    }

    private void DestroyTilesWithAlphaZeroOnStart()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);

        foreach (var sr in renderers)
        {
            if (sr == null) continue;
            if (!IsInLayerMask(sr.gameObject.layer, tileLayer)) continue;

            if (sr.color.a <= 0f)
                Destroy(sr.gameObject);
        }
    }

    private void DestroyTilesWithAlphaZero()
    {
        if (!destroyTilesWithAlphaZeroOnStart) return;
        DestroyTilesWithAlphaZeroOnStart();
    }

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

    // ---------- DISABLE DEPTH-3 CHILD COLLIDERS ----------

    private void CacheLevel3ChildColliders()
    {
        var all = GetComponentsInChildren<Collider2D>(true);
        var list = new List<Collider2D>(all.Length);

        for (int i = 0; i < all.Length; i++)
        {
            var c = all[i];
            if (c == null) continue;

            // Non toccare il collider del dragger (serve per prendere la carta)
            if (c == draggerCollider) continue;

            int depth = GetDepthFromRoot(transform, c.transform);
            if (depth == 3) // CardVariant(1) -> TileX(2) -> COMPONENT(3)
                list.Add(c);
        }

        level3ChildColliders = list.ToArray();
    }

    private int GetDepthFromRoot(Transform root, Transform t)
    {
        int depth = 0;
        Transform cur = t;

        while (cur != null && cur != root)
        {
            depth++;
            cur = cur.parent;
        }

        if (cur == null) return -1; // non discendente di root
        return depth;
    }

    private void SetLevel3CollidersEnabled(bool enabled)
    {
        if (level3ChildColliders == null || level3ChildColliders.Length == 0)
            CacheLevel3ChildColliders();

        for (int i = 0; i < level3ChildColliders.Length; i++)
        {
            var c = level3ChildColliders[i];
            if (c == null) continue;
            c.enabled = enabled;
        }
    }

    // ---------- GIZMOS ----------

    private void OnDrawGizmos()
    {
        if (!drawSnapAreaGizmos) return;
        if (!useSnapLimits) return;

        Gizmos.color = Color.yellow;

        Vector3 min = new Vector3(snapAreaMin.x, snapAreaMin.y, 0f);
        Vector3 max = new Vector3(snapAreaMax.x, snapAreaMax.y, 0f);

        Vector3 size = max - min;
        Vector3 center = min + size * 0.5f;

        Gizmos.DrawWireCube(center, size);
    }
}