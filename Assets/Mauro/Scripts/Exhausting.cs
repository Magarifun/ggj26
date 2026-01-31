using UnityEngine;
using UnityEngine.Events;

public class Exhausting : MonoBehaviour
{
    public float exhaustionThreshold = 0.5f;
    public float exhaustionTime = 5.0f;
    public float exhaustionPerHit = 0.2f;
    public float recoveryRate = 1.0f;
    private float exhaustion = 0;
    private bool overheated = false;
    private float overheatedTime = 0;
    public UnityEvent OnOverheating;
    public UnityEvent OnCoolingDown;
    public UnityEvent OnExhausted;

    void Update()
    {
        exhaustion -= recoveryRate * Time.deltaTime;
        exhaustion = Mathf.Clamp01(exhaustion);
        if (overheated && exhaustion < exhaustionThreshold)
        {
            overheated = false;
            overheatedTime = 0f;
            OnCoolingDown?.Invoke();
        }
        else if (!overheated && exhaustion >= exhaustionThreshold)
        {
            overheated = true;
            OnOverheating?.Invoke();
        }
        if (overheated)
        {
            overheatedTime += Time.deltaTime;
            if (overheatedTime >= exhaustionTime)
            {
                Debug.Log($"{gameObject.name} is exhausted");
                OnExhausted?.Invoke();
                Destroy(gameObject);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            exhaustion += exhaustionPerHit;
        }
    }
}
