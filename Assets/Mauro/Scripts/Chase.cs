using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Chase : MonoBehaviour
{
    public Vector3 initialPosition = new(-3.83f, 5.01f, 0);
    public float chasedProgressAtLevelStart;
    public float chaseInitialDelay;
    public float initialChaseDuration;
    public float extraDurationPerLevel;
    public int initialScoreGoal;
    public int extraScoreGoalPerLevel;
    public int[] extraCheckpointAtLevels;
    public ChaseIcon chaser;
    public ChaseIcon chased;
    public ChaseIcon door;
    public float doorProgress;
    public GameObject checkpoint; // kinda used like a prefab
    private List<ChaseIcon> checkpoints = new();
    public TextMeshPro levelLabel;
    public TextMeshPro scoreLabel;
    public WolfPaw wolfPawPrefab;
    public UnityEvent onChaseEnd;
    public UnityEvent onLevelUp;
    private int level;
    private int scoreGoal;
    private int score;
    private static Chase instance;

    void Start()
    {
        checkpoint.SetActive(false);
        transform.position = initialPosition;
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
        }
        else
        {
            chaser.Speed = 0f;
            Invoke(nameof(SetChaserSpeed), chaseInitialDelay);
        }
        chaser.Progress = 0.0f;
        chased.Progress = chasedProgressAtLevelStart;
        if (extraCheckpointAtLevels.Contains(level))
        {
            GameObject newCheckpoint = Instantiate(checkpoint, transform);
            checkpoints.Add(newCheckpoint.GetComponent<ChaseIcon>());
        }
        for (int i = 0; i < checkpoints.Count; i++)
        {
            ChaseIcon cpIcon = checkpoints[i];
            float minDistanceFromOtherIcons = float.MaxValue;
            float progress;
            int attempts = 0;
            do
            {
                attempts++;
                progress = Random.Range(0.25f, 0.75f);
                if (i > 5 || attempts > 100)
                {
                    Debug.LogWarning("Breaking the loop");
                    // Break to avoid infinite loops
                    break;
                }
                for (int j = i - 1; j >= 0; j--)
                {
                    minDistanceFromOtherIcons = Mathf.Min(minDistanceFromOtherIcons,
                        Mathf.Abs(progress - checkpoints[j].ActualProgress));
                }
            } while (minDistanceFromOtherIcons < 0.1f);
            cpIcon.Progress = progress;
            cpIcon.gameObject.SetActive(true);
        }
        door.Progress = doorProgress;
        door.gameObject.SetActive(true);
        KeyBubble.Instance.gameObject.SetActive(true);
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
            onLevelUp?.Invoke();
            SetLevel(level + 1);
        }
        foreach (ChaseIcon cpIcon in checkpoints)
        {
            if (cpIcon.gameObject.activeInHierarchy && chaser.ActualProgress >= cpIcon.ActualProgress)
            {
                SpawnWolfPaw();
                cpIcon.gameObject.SetActive(false);
            }
        }
        if (chased.ActualProgress >= door.ActualProgress && door.gameObject.activeInHierarchy)
        {
            chased.frozen = true;
        }
    }

    public void SpawnWolfPaw()
    {
        Instantiate(wolfPawPrefab, transform.position + Vector3.down * 3, Quaternion.identity);
    }

    public void UnlockDoor()
    {
        door.gameObject.SetActive(false);
        chased.frozen = false;
    }
}
