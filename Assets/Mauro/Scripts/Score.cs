using TMPro;
using UnityEngine;

public class Score : MonoBehaviour
{
    public int points;
    public float speed = 5.0f;
    private TextMeshPro label;

    void Start()
    {
        label = GetComponent<TextMeshPro>();
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
        Vector3 destination = Camera.main.ScreenToWorldPoint(Chase.Instance.chased.transform.position);
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, destination) < 0.1f)
        {
            Chase.Instance.AddScore(points);
            Destroy(gameObject);
        }
    }
}
