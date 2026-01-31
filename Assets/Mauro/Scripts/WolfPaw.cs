using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WolfPaw : MonoBehaviour
{
    public float duration = 0.5f;
    public float length = 5.0f;
    public float range = 0.5f;
    private float speed;
    private List<GameObject> cards = new();
    private float leftmostCardX = 0f;
    private float rightmostCardX = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        speed = length / duration;
        CardPoolManager cardPool = GameObject.FindFirstObjectByType<CardPoolManager>();
        CardDragAndDrop2D_SnapSortingErase[] allCards = cardPool.GetComponentsInChildren<CardDragAndDrop2D_SnapSortingErase>();
        foreach (CardDragAndDrop2D_SnapSortingErase card in allCards)
        {
            if (card.isPlaced)
            {
                cards.Add(card.gameObject);
                if (cards.Count == 1)
                {
                    leftmostCardX = card.transform.position.x;
                    rightmostCardX = card.transform.position.x;
                }
                else
                {
                    if (card.transform.position.x < leftmostCardX)
                    {
                        leftmostCardX = card.transform.position.x;
                    }
                    if (card.transform.position.x > rightmostCardX)
                    {
                        rightmostCardX = card.transform.position.x;
                    }
                }
            }
        }
        float randomX = Random.Range(leftmostCardX, rightmostCardX);
        transform.position = new Vector3(randomX, transform.position.y, transform.position.z);
        Invoke(nameof(End), duration);
    }

    private void End()
    {
        Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(speed * Time.deltaTime * Vector3.down);
        foreach (GameObject card in cards)
        {
            if (card != null && !card.IsDestroyed())
            {
                float distance = Vector3.Distance(transform.position, card.transform.position);
                if (distance < range)
                {
                    Destroy(card);
                }
            }
        }
    }
}
