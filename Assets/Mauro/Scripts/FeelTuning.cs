using UnityEngine;

public class FeelTuning : MonoBehaviour
{
    public float gravity;

    void Start()
    {
        ApplySetup();
        InvokeRepeating(nameof(ApplySetup), 5f, 1f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ApplySetup()
    {
        Physics2D.gravity = new Vector3(0, -Mathf.Abs(gravity), 0);
    }

    public static float CompensateForGravity(float baseValue)
    {
        return baseValue * Mathf.Sqrt(Mathf.Abs(Physics2D.gravity.y));
    }
}
