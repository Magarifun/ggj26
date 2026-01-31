using UnityEngine;

public class CardUnlockState : MonoBehaviour
{
    [SerializeField] private bool unlocked = true;
    public bool Unlocked => unlocked;

    public void SetUnlocked(bool value) => unlocked = value;
}