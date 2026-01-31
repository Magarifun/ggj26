using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Chase : MonoBehaviour
{
    public float chasedProgressAtLevelStart;
    public float initialDuration;
    public float extraDurationPerLevel;
    public int initialScoreGoal;
    public int extraScoreGoalPerLevel;
    public ChaseIcon chaser;
    public ChaseIcon chased;
    public TextMeshProUGUI levelLabel;
    public TextMeshProUGUI scoreLabel;
    public UnityEvent onChaseEnd;
    private int level;
    private float chaserSpeed;
    private int scoreGoal;
    private int score;
    private static Chase instance;

    void Start()
    {
        if (instance != null)
        {
            Destroy(this);
            return;
        }
        instance = this;
        SetLevel(1);
        OnUpdateScore();
    }

    public static Chase Instance => instance;

    private void SetLevel(int newLevel)
    {
        level = newLevel;
        levelLabel.text = new RomanNumeral(newLevel).ToString();
        score = 0;
        scoreGoal = initialScoreGoal + extraScoreGoalPerLevel * (level - 1);
        chaserSpeed = 1.0f / (initialDuration + extraDurationPerLevel * (level - 1));
        chaser.progress = 0.0f;
    }

    private void OnUpdateScore()
    {
        scoreLabel.text = score + " / " + scoreGoal;
        chased.progress = (1.0f - chasedProgressAtLevelStart) * ((float) score / scoreGoal) + chasedProgressAtLevelStart;
    }

    public void AddScore(int points)
    {
        score += points;
        if (score >= scoreGoal)
        {
            SetLevel(level + 1);
        }
        OnUpdateScore();
    }

    // Update is called once per frame
    void Update()
    {
        chaser.progress += chaserSpeed * Time.deltaTime;
        if (chaser.progress >= chased.progress)
        {
            onChaseEnd?.Invoke();
        }
    }
}
