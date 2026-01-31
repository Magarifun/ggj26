using UnityEngine;

public class CardInstance : MonoBehaviour
{
    public string cardId;
    public int copyNumber;

    // una sola restituzione per card
    public bool returnedToPoolOnce = false;
}