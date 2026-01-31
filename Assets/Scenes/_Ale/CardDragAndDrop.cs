using UnityEngine;
using UnityEngine.InputSystem;

public class CardDragAndDrop : MonoBehaviour
{
    public Camera cam;
    public float dragZ = 0f;

    private bool isDragging;
    private Vector3 offset;

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;
    }

    private void Update()
    {
        if (!isDragging) return;
        if (cam == null) return;
        if (Mouse.current == null) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();

        Vector3 mouseWorld = cam.ScreenToWorldPoint(new Vector3(
            mouseScreen.x,
            mouseScreen.y,
            Mathf.Abs(cam.transform.position.z - dragZ)
        ));

        transform.position = mouseWorld + offset;
    }

    public void BeginDrag()
    {
        if (cam == null || Mouse.current == null) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();

        Vector3 mouseWorld = cam.ScreenToWorldPoint(new Vector3(
            mouseScreen.x,
            mouseScreen.y,
            Mathf.Abs(cam.transform.position.z - dragZ)
        ));

        offset = transform.position - mouseWorld;
        isDragging = true;
    }

    public void EndDrag()
    {
        isDragging = false;
    }
}