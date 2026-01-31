using TMPro;
using UnityEngine;

public class Score : MonoBehaviour
{
    public int points;
    public float acceleration = 5.0f;
    public int minFontSize = 2;
    public int maxFontSize = 14;
    public int scoreForMaxFontSize = 1000;
    private float speed = 1.0f;
    private TextMeshPro label;

    void Start()
    {
        label = GetComponent<TextMeshPro>();
        label.fontSize = Mathf.Clamp(
            maxFontSize * Mathf.Log10(points) / Mathf.Log10(scoreForMaxFontSize), 
            minFontSize, 
            maxFontSize);
        if (points >= 0)
        {
            label.text = $"+{points}";
        }
        else
        {
            label.text = points.ToString();
        }
        
    }

    void Update()
    {
        Vector3 destination = Chase.Instance.chased.transform.position;
        speed += acceleration * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, destination) < 0.1f)
        {
            Chase.Instance.AddScore(points);
            Destroy(gameObject);
        }
    }
}
