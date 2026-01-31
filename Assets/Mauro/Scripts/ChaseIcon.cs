using TMPro;
using UnityEngine;

public class ChaseIcon : MonoBehaviour
{
    public float progressMaxSpeed = 0.2f;
    private float targetProgress = 999; // 0 to 1
    private float actualProgress = 999; // 0 to 1
    private float speed = 0f;

    public float Progress { set { targetProgress = value; } }
    public float Speed { set { speed = value; } }
    public float ActualProgress => actualProgress;


    void Update()
    {
        targetProgress += speed * Time.deltaTime;
        Bounds parentBounds = transform.parent.GetComponent<Collider2D>().bounds;
        float parentWidth = parentBounds.size.x;
        float ownWidth = transform.GetComponent<SpriteRenderer>().size.x;
        if (targetProgress < actualProgress)
        {
            actualProgress = targetProgress;
        }
        else
        {
            actualProgress = Mathf.MoveTowards(actualProgress, targetProgress, progressMaxSpeed * Time.deltaTime);
        }
        float inset = (parentWidth - ownWidth) * actualProgress;
        float minX = parentBounds.min.x;
        transform.position = new(minX + inset + ownWidth / 2, transform.position.y, transform.position.z);
    }
}
