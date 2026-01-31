using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Switcher : MonoBehaviour
{
    public string switcherGroup = "Switchers";
    public int threshold = 3;
    public int score = 500;
    public bool toggler = true;
    public Sprite onSprite;
    public Sprite offSprite;
    private bool isOn = false;
    private float countDownToUnlock = -1;
    public UnityEvent OnCompleteSwitcherGroup;

    void Start()
    {
        Refresh();
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        OnTouchCollider2D(collider);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        OnTouchCollider2D(collision.collider);
    }

    private void OnTouchCollider2D(Collider2D collider)
    {
        if (IsLocked)
        {
            // Locked, don't interact
            return;
        }
        if (collider.CompareTag("Player"))
        {
            if (React())
            {
                Switcher[] all = FindObjectsByType<Switcher>(FindObjectsSortMode.None);
                List<Switcher> myGroup = new();
                foreach (Switcher switcher in all)
                {
                    if (switcher.switcherGroup == switcherGroup && switcher.isOn && !switcher.IsLocked)
                    {
                        myGroup.Add(switcher);
                    }
                }
                if (myGroup.Count >= threshold)
                {
                    foreach (Switcher switcher in myGroup)
                    {
                        switcher.Lock();
                        Scorer.Instance.ScorePoints(score, switcher);
                    }
                    OnCompleteSwitcherGroup?.Invoke();
                }
            }
        }
    }

    public bool IsLocked => countDownToUnlock > 0;

    public void Lock()
    {
        countDownToUnlock = 0.5f;
    }

    public bool React()
    {
        if (toggler)
        {
            return Toggle();
        }
        else
        {
            return Switch(true);
        }
    }

    public bool Switch(bool state)
    {
        bool wasOn = isOn;
        isOn = state;
        Refresh();
        return !wasOn && isOn;
    }

    public bool Toggle() 
    {
        isOn = !isOn;
        Refresh();
        return isOn;
    }

    private void Refresh()
    {
        Sprite sprite = isOn ? onSprite : offSprite;
        if (sprite != null)
        {
            GetComponent<SpriteRenderer>().sprite = sprite;
        }
    }

    void Update()
    {
        if (IsLocked)
        {
            countDownToUnlock -= Time.deltaTime;
            if (!IsLocked)
            {
                Switch(false);
            }
        }
    }
}
