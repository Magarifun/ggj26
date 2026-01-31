using UnityEngine;

public class Flipper : MonoBehaviour
{
    public KeyCode key = KeyCode.RightControl;
    private Animator animator;
    public float impulseFactor;
    private Rigidbody2D body;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(key))
        {
            animator.SetTrigger("Flip");
        }
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            Vector2 impact = body.GetPointVelocity(collision.contacts[0].point);
            Vector2 normalizedImpact = impulseFactor * Mathf.Sqrt(Mathf.Abs(Physics2D.gravity.y)) * impact;
            collision.rigidbody.totalForce = Vector2.zero;
            collision.rigidbody.AddForce(-normalizedImpact, ForceMode2D.Impulse);
            Scorer.Instance.ScorePoints(100, this);
        }
    }
}
