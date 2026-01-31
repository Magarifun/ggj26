using UnityEngine;

public class Ball : MonoBehaviour
{
    public float rotationFactor;
    private Rigidbody2D body;
    private GameObject sphere;

    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        sphere = transform.Find("Sphere").gameObject;
        rotationFactor = 360.0f / (Mathf.PI * body.transform.localScale.x);
    }

    void Update()
    {
        Vector2 v = body.linearVelocity;
        Vector3 rotationAxis = Vector3.Cross(Vector3.back, v.normalized);
        // Rotate along the movement direction
        sphere.transform.RotateAround(sphere.transform.position, rotationAxis, v.magnitude * rotationFactor * Time.deltaTime);
    }
}
