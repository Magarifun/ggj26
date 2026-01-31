using UnityEngine;

public class ChaseIcon : MonoBehaviour
{
    public float progressMaxSpeed = 0.2f;
    private float targetProgress = 999; // 0 to 1
    private float actualProgress = 999; // 0 to 1
    private RectTransform rectTransform;
    private float speed = 0f;

    public float Progress { set { targetProgress = value; } }
    public float Speed { set { speed = value; } }
    public float ActualProgress => actualProgress;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        targetProgress += speed * Time.deltaTime;
        float parentWidth = transform.parent.GetComponent<RectTransform>().rect.width;
        float ownWidth = rectTransform.rect.width;
        if (targetProgress < actualProgress)
        {
            actualProgress = targetProgress;
        }
        else
        {
            actualProgress = Mathf.MoveTowards(actualProgress, targetProgress, progressMaxSpeed * Time.deltaTime);
        }
        float inset = (parentWidth - ownWidth) * actualProgress;
        rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, inset, rectTransform.rect.width);
    }
}
