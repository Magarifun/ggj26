using Unity.VisualScripting;
using UnityEngine;

public class KeyBubble : MonoBehaviour
{
    private Vector3 center;
    private static KeyBubble instance;
    public static KeyBubble Instance => instance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (instance != null)
        {
            Destroy(this);
            return;
        }
        instance = this;
        center = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
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
