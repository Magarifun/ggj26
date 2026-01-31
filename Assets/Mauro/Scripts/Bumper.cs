using System.Linq;
using UnityEngine;

public class Bumper : MonoBehaviour
{
    public float impulse;

    void Start()
    {
        impulse = Mathf.Abs(impulse);
    }

    void Update()
    {
        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            ContactPoint2D contact = collision.contacts[0];
            float normalizedImpulse = impulse * Mathf.Sqrt(Mathf.Abs(Physics2D.gravity.y));
            collision.rigidbody.AddForce(-normalizedImpulse * contact.normal, ForceMode2D.Impulse);
            Scorer.Instance.ScorePoints(10, this);
        }
    }
}
