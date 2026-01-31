using UnityEngine;
using UnityEngine.InputSystem;

public class CardDragAndDrop2D : MonoBehaviour
{
    [Header("References")]
    public Transform cardDragger;        // child con Collider2D
    public Camera cam;
    public LayerMask draggableLayer;

    [Header("Snap")]
    public BoardManager2D board;
    public float snapDistance = 1.5f;
    public float fixedZ = 0f;

    private Collider2D draggerCollider;
    private bool isDragging;
    private Vector3 offset;

    private BoardSlot2D currentSlot;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;

        if (cardDragger == null)
        {
            Transform found = transform.Find("CardDragger");
            if (found != null) cardDragger = found;
        }

        if (cardDragger != null)
            draggerCollider = cardDragger.GetComponent<Collider2D>();

        if (draggerCollider == null)
            Debug.LogError("CardDragger deve avere un Collider2D.");

        fixedZ = transform.position.z;
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
            {
                isDragging = true;

                // libera slot attuale SOLO di questa card
                if (currentSlot != null)
                {
                    currentSlot.occupied = false;
                    currentSlot = null;
                }

                Vector3 mouseWorld3D = new Vector3(mouseWorld.x, mouseWorld.y, fixedZ);
                offset = transform.position - mouseWorld3D;
                offset.z = 0f;
            }
        }

        // DRAG
        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            Vector3 mouseWorld3D = new Vector3(mouseWorld.x, mouseWorld.y, fixedZ);
            Vector3 target = mouseWorld3D + offset;
            target.z = fixedZ;

            transform.position = target;
        }

        // UP
        if (isDragging && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
            SnapThisCardOnly();
        }
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
}