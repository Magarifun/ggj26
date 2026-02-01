using Unity.VisualScripting;
using UnityEngine;

public class KeyBubble : MonoBehaviour
{
    public float brownianSpeed;
    public float perlinSpeed;
    private Vector3 center;
    private static KeyBubble instance;
    public static KeyBubble Instance => instance;
    private bool restoring = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        center = transform.position;
    }

    private void Start()
    {
        if (instance != null)
        {
            Destroy(this);
            return;
        }
        instance = this;
    }

    private void BackToBrownian()
    {
        restoring = false;
    }

    // Update is called once per frame
    void Update()
    {
        float venturing = Vector3.Distance(transform.position, center);
        if (restoring || venturing > 0.5f)
        {
            // Vector toward the center
            Vector3 toCenter = (center - transform.position) * 2;

            transform.Translate(brownianSpeed * Time.deltaTime * toCenter);
            if (!restoring)
            {
                restoring = true;
                Invoke(nameof(BackToBrownian), 0.25f);
            }
        }

        // Random Perlin noise vector based on current position
        float perlinAngle = 360f *
            Mathf.PerlinNoise(transform.position.x * Time.time * perlinSpeed, transform.position.y * Time.time * perlinSpeed);
        // 1-unit vector in perlinAngle direction
        Vector3 noise = new(Mathf.Cos(perlinAngle * Mathf.Deg2Rad), Mathf.Sin(perlinAngle * Mathf.Deg2Rad), 0f);
        transform.Translate(brownianSpeed * Time.deltaTime * noise);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("KeyBubble collided with " + collision.gameObject.name);
        if (collision.gameObject.CompareTag("Player"))
        {
            Chase.Instance.UnlockDoor();
            gameObject.SetActive(false);
        }
    }
}
