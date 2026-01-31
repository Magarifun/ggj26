using UnityEngine;

public class Scorer : MonoBehaviour
{
    public GameObject scorePrefab;
    private static Scorer instance;

    public static Scorer Instance => instance;

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ScorePoints(int points, Component origin) => ScorePoints(points, origin.gameObject);

    public void ScorePoints(int points, GameObject origin)
    {
        GameObject scoreObject = Instantiate(scorePrefab, origin.transform.position, Quaternion.identity, transform);
        scoreObject.name = $"Score {points} pts.";
        Score score = scoreObject.GetComponent<Score>();
        score.points = points;
    }
}
