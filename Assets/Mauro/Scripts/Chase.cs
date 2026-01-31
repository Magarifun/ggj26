using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Chase : MonoBehaviour
{
    public float chasedProgressAtLevelStart;
    public float chaseInitialDelay;
    public float initialChaseDuration;
    public float extraDurationPerLevel;
    public int initialScoreGoal;
    public int extraScoreGoalPerLevel;
    public ChaseIcon chaser;
    public ChaseIcon chased;
    public TextMeshPro levelLabel;
    public TextMeshPro scoreLabel;
    public UnityEvent onChaseEnd;
    private int level;
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
        if (level > 1)
        {
            SetChaserSpeed();
        } else
        {
            chaser.Speed = 0f;
            Invoke(nameof(SetChaserSpeed), chaseInitialDelay);
        }
        chaser.Progress = 0.0f;
        chased.Progress = chasedProgressAtLevelStart;
    }

    private void SetChaserSpeed()
    {
        chaser.Speed = 1.0f / (initialChaseDuration + extraDurationPerLevel * (level - 1));
    }

    private void OnUpdateScore()
    {
        chased.Progress = (1.0f - chasedProgressAtLevelStart) * ((float)score / scoreGoal) + chasedProgressAtLevelStart;
    }

    public void AddScore(int points)
    {
        score += points;
        OnUpdateScore();
    }

    // Update is called once per frame
    void Update()
    {
        int reachedScore = Mathf.RoundToInt(scoreGoal * 
            (chased.ActualProgress - chasedProgressAtLevelStart) / (1.0f - chasedProgressAtLevelStart));
        scoreLabel.text = reachedScore + " / " + scoreGoal;
        if (chaser.ActualProgress >= chased.ActualProgress)
        {
            onChaseEnd?.Invoke();
            chaser.Progress = 0f;
        }
        else if (chased.ActualProgress >= 1.0f)
        {
            SetLevel(level + 1);
        }
    }
}
