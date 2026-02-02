using UnityEngine;

public class Flipper : MonoBehaviour
{
    public int score = 50;
    public float impulseFactor;
    private Rigidbody2D body;
    private Animator animator;
    private CardDragAndDrop2D_SnapSortingErase card;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        card = GetComponentInParent<CardDragAndDrop2D_SnapSortingErase>();
        animator = GetComponent<Animator>();
        animator.animatePhysics = false;
    }

    private void Update()
    {
        if (card == null || card.isPlaced)
        {
            animator.animatePhysics = true;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            Vector2 impact = body.GetPointVelocity(collision.contacts[0].point);
            Vector2 normalizedImpact = FeelTuning.CompensateForGravity(impulseFactor) * impact;
            collision.rigidbody.totalForce = Vector2.zero;
            collision.rigidbody.AddForce(-normalizedImpact, ForceMode2D.Impulse);
            Scorer.Instance.ScorePoints(score, this);
        }
    }
}
