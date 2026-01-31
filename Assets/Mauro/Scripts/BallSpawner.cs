using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    public GameObject ballPrefab;
    public float spawnInterval = 5.0f;
    public float impulse;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PrepareToSpawn();
    }

    void PrepareToSpawn()
    {
        Invoke(nameof(Spawn), spawnInterval);
    }

    void Spawn()
    {
        GameObject ball = Instantiate(ballPrefab, transform.position, Quaternion.identity);
        ball.GetComponent<Rigidbody2D>().AddForce(Vector2.up * FeelTuning.CompensateForGravity(impulse), ForceMode2D.Impulse);
        PrepareToSpawn();
    }
}
