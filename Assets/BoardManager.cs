using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Grid")]
    public int columns = 5;
    public int rows = 3;
    public float spacingX = 2.2f;
    public float spacingY = 3.2f;

    [Header("Slot Prefab")]
    public BoardSlot2D slotPrefab;

    public List<BoardSlot2D> Slots { get; private set; } = new List<BoardSlot2D>();

    private void Start()
    {
        GenerateSlots();
    }

    [ContextMenu("Generate Slots")]
    public void GenerateSlots()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        Slots.Clear();

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector3 pos = transform.position + new Vector3(x * spacingX, -y * spacingY, 0f);
                BoardSlot2D slot = Instantiate(slotPrefab, pos, Quaternion.identity, transform);
                slot.name = $"Slot_{x}_{y}";
                Slots.Add(slot);
            }
        }
    }

    public BoardSlot2D GetClosestFreeSlot(Vector3 position, float maxDistance)
    {
        BoardSlot2D best = null;
        float bestDist = float.MaxValue;

        foreach (var slot in Slots)
        {
            if (slot.occupied) continue;

            float d = Vector2.Distance(position, slot.transform.position);
            if (d < bestDist && d <= maxDistance)
            {
                bestDist = d;
                best = slot;
            }
        }

        return best;
    }
}