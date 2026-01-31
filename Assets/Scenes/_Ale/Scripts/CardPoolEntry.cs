using UnityEngine;

public class CardPoolEntry : MonoBehaviour
{
    [Header("Deck Info")]
    public string cardId = "Card_A";   // ID univoco per tipo (es: "LShape", "Cross", ecc)
    public int maxCopiesInDeck = 3;    // quante copie max può uscire contemporaneamente nel deck
}