using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CardDragAndDrop2D_SortingErase : MonoBehaviour
{
    [Header("References")]
    public Transform cardDragger;      
    public Camera cam;
    public LayerMask draggableLayer;

    [Header("Board Snap")]
    public BoardManager2D board;
    public float snapDistance = 1.5f;

    [Header("Tiles Overlap")]
    public LayerMask tileLayer;        
    public bool includeTriggers = true;

    [Header("Sorting")]
    public int dragSortingDelta = 1;   
    public int resetSortingOrder = 0;

    private Collider2D draggerCollider;
    private bool isDragging;
    private Vector3 offset;
    private float fixedZ;

    private BoardSlot2D currentSlot;

    private Collider2D[] myTileColliders;
    private SpriteRenderer[] myTileRenderers;

    private ContactFilter2D filter;
    private readonly List<Collider2D> overlapResults = new List<Collider2D>(32);

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

        myTileColliders = GetComponentsInChildren<Collider2D>(true);
        myTileRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        filter = new ContactFilter2D
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
    }

    private void Update()
    {
        if (cam == null || draggerCollider == null) return;
        if (Mouse.current == null) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector2 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);

        // DOWN
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero, 0f, draggableLayer);

            if (hit.collider != null && hit.collider == draggerCollider)
                BeginDrag(mouseWorld);
        }

        // DRAG
        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            Drag(mouseWorld);
        }

        // UP
        if (isDragging && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            EndDrag();
        }
    }

    private void BeginDrag(Vector2 mouseWorld)
    {
        isDragging = true;

        // libera lo slot corrente (solo questa card)
        if (currentSlot != null)
        {
            currentSlot.occupied = false;
            currentSlot = null;
        }

        Vector3 mouseWorld3D = new Vector3(mouseWorld.x, mouseWorld.y, fixedZ);
        offset = transform.position - mouseWorld3D;
        offset.z = 0f;

        // porta davanti (sortingOrder +1)
        AddSortingOrder(dragSortingDelta);
    }

    private void Drag(Vector2 mouseWorld)
    {
        Vector3 mouseWorld3D = new Vector3(mouseWorld.x, mouseWorld.y, fixedZ);
        Vector3 target = mouseWorld3D + offset;
        target.z = fixedZ;

        transform.position = target;
    }

    private void EndDrag()
    {
        isDragging = false;

        // 1) Snap
        SnapThisCardOnly();

        // 2) Elimina SOLO le tile ancora sotto (overlap reale)
        DestroyTilesStillUnderMe();

        // 3) Reset sortingOrder a 0
        SetSortingOrder(resetSortingOrder);
    }

    private void SnapThisCardOnly()
    {
        if (board == null) return;

        BoardSlot2D slot = board.GetClosestFreeSlot(transform.position, snapDistance);
        if (slot == null) return;

        transform.position = new Vector3(slot.transform.position.x, slot.transform.position.y, fixedZ);

        slot.occupied = true;
        currentSlot = slot;
    }

   private void DestroyTilesStillUnderMe()
{
    HashSet<Collider2D> toDestroy = new HashSet<Collider2D>();

    foreach (var myTile in myTileColliders)
    {
        if (myTile == null) continue;

        SpriteRenderer mySR = myTile.GetComponent<SpriteRenderer>();
        if (mySR == null) continue;

        overlapResults.Clear();
        myTile.Overlap(filter, overlapResults);

        foreach (var hit in overlapResults)
        {
            if (hit == null) continue;

            // ignora tile della stessa card
            if (hit.transform.IsChildOf(transform))
                continue;

            SpriteRenderer hitSR = hit.GetComponent<SpriteRenderer>();
            if (hitSR == null) continue;

            // 1) deve essere sotto in sortingOrder
            bool isBelow = hitSR.sortingOrder < mySR.sortingOrder;

            // 2) deve essere visibile (alpha > 0)
            bool isVisible = hitSR.color.a > 0f;

            if (isBelow && isVisible)
            {
                toDestroy.Add(hit);
            }
        }
    }

    foreach (var col in toDestroy)
    {
        if (col == null) continue;
        Destroy(col.gameObject);
    }
}

    private void AddSortingOrder(int delta)
    {
        for (int i = 0; i < myTileRenderers.Length; i++)
        {
            if (myTileRenderers[i] == null) continue;
            myTileRenderers[i].sortingOrder += delta;
        }
    }

    private void SetSortingOrder(int value)
    {
        for (int i = 0; i < myTileRenderers.Length; i++)
        {
            if (myTileRenderers[i] == null) continue;
            myTileRenderers[i].sortingOrder = value;
        }
    }
}