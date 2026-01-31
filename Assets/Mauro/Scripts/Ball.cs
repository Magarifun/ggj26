using UnityEngine;

public class Ball : MonoBehaviour
{
    public Material shockwaveMaterial;
    public float explosionDuration = 0.3f;
    public float maxExplosionRadius = 1f;
    private float rotationFactor;
    private Rigidbody2D body;
    private GameObject sphere;
    private bool exploding = false;
    private float explosionProgress = 0f;
    private Vector3 area = Vector3.zero;
    private float timeSinceInArea;
    private float birthY;

    void Start()
    {
        birthY = transform.position.y;
        body = GetComponent<Rigidbody2D>();
        sphere = transform.Find("Sphere").gameObject;
        rotationFactor = 360.0f / (Mathf.PI * body.transform.localScale.x);
        CheckArea();
    }

    private void CheckArea()
    {
        if (Vector3.Distance(transform.position, area) > 0.5f)
        {
            area = transform.position;
            timeSinceInArea = 0f;
        }
        else
        {
            timeSinceInArea += Time.deltaTime;
            if (timeSinceInArea > 2f && body.linearVelocity.magnitude < 0.5f)
            {
                Explode();
            }
        }
    }

    void Update()
    {
        if (transform.position.y < birthY - 10f)
        {
            Destroy(this);
        }
        if (exploding)
        {
            explosionProgress += Time.deltaTime / explosionDuration;
            if (explosionProgress >= 1f)
            {
                Destroy(gameObject);
            }
            else
            {
                float currentRadius = Mathf.Lerp(0f, maxExplosionRadius, explosionProgress);
                transform.localScale = new Vector3(currentRadius, currentRadius, currentRadius);
            }
        }
        else
        {
            Vector2 v = body.linearVelocity;
            Vector3 rotationAxis = Vector3.Cross(Vector3.back, v.normalized);
            // Rotate along the movement direction
            sphere.transform.RotateAround(sphere.transform.position, rotationAxis, v.magnitude * rotationFactor * Time.deltaTime);
            CheckArea();
        }
    }

    void Explode()
    {
        sphere.GetComponent<Renderer>().material = shockwaveMaterial;
        exploding = true;
    }
}
