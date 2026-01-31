using UnityEngine;

public class ChaseIcon : MonoBehaviour
{
    public float progress = 0.0f; // 0 to 1
    private RectTransform rectTransform;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        // Calculate parent's width (UI panel)
        float parentWidth = transform.parent.GetComponent<RectTransform>().rect.width;
        float ownWidth = rectTransform.rect.width;
        // Set local position based on progress
        float inset = (parentWidth - ownWidth) * progress;
        rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, inset, rectTransform.rect.width);
    }
}
