using Unity.VisualScripting;
using UnityEngine;

public class KeyBubble : MonoBehaviour
{
    public float brownianSpeed;
    public float perlinSpeed;
    private Vector3 anchor;
    private static KeyBubble instance;
    public static KeyBubble Instance => instance;
    private bool restoring = false;

    public void SetAnchor()
    {
        transform.position = new Vector3(Random.Range(-4.5f, 4.5f), transform.position.y, transform.position.z);
        anchor = transform.position;
    }

    private void Start()
    {
        if (instance != null)
        {
            Destroy(this);
            return;
        }
        instance = this;
        SetAnchor();
    }

    private void BackToBrownian()
    {
        restoring = false;
    }

    // Update is called once per frame
    void Update()
    {
        float venturing = Vector3.Distance(transform.position, anchor);
        if (restoring || venturing > 0.5f)
        {
            // Vector toward the center
            Vector3 toCenter = (anchor - transform.position) * 2;

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
            SetAnchor();
            gameObject.SetActive(false);
        }
    }
}
