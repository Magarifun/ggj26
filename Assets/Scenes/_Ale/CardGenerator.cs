using System.Collections.Generic;
using UnityEngine;

public class CardGenerator : MonoBehaviour
{
    [Header("Assign exactly 9 materials here")]
    public Material[] materials;

    [Header("If empty, will auto-get SpriteRenderers from children")]
    public SpriteRenderer[] tiles;

    [Header("How many tiles should start hidden (alpha = 0)")]
    [Range(0, 9)]
    public int hiddenCount = 0;

    private void Awake()
    {
        if (tiles == null || tiles.Length == 0)
        {
            tiles = GetComponentsInChildren<SpriteRenderer>();
        }
    }

    [ContextMenu("Scramble Materials (Sprite Instances)")]
    public void Scramble()
    {
        if (materials == null || materials.Length != 9)
        {
            Debug.LogError("You must assign exactly 9 materials.");
            return;
        }

        if (tiles == null || tiles.Length != 9)
        {
            Debug.LogError("You must have exactly 9 SpriteRenderers (one per tile).");
            return;
        }

        hiddenCount = Mathf.Clamp(hiddenCount, 0, 9);

        // Shuffle materials (unique assignment)
        List<Material> shuffledMaterials = new List<Material>(materials);
        for (int i = 0; i < shuffledMaterials.Count; i++)
        {
            int rand = Random.Range(i, shuffledMaterials.Count);
            (shuffledMaterials[i], shuffledMaterials[rand]) = (shuffledMaterials[rand], shuffledMaterials[i]);
        }

        // Shuffle tile indexes so hidden tiles are random
        List<int> tileIndexes = new List<int>();
        for (int i = 0; i < 9; i++) tileIndexes.Add(i);

        for (int i = 0; i < tileIndexes.Count; i++)
        {
            int rand = Random.Range(i, tileIndexes.Count);
            (tileIndexes[i], tileIndexes[rand]) = (tileIndexes[rand], tileIndexes[i]);
        }

        // Assign instances + alpha control
        for (int i = 0; i < tiles.Length; i++)
        {
            Material instanceMat = new Material(shuffledMaterials[i]);
            tiles[i].material = instanceMat;

            bool shouldHide = tileIndexes.IndexOf(i) < hiddenCount;
            SetSpriteMaterialAlpha(instanceMat, shouldHide ? 0f : 1f);
        }
    }

    private void SetSpriteMaterialAlpha(Material mat, float alpha)
    {
        // Sprite shaders usually use _Color
        if (mat.HasProperty("_Color"))
        {
            Color c = mat.color;
            c.a = alpha;
            mat.color = c;
        }
        else
        {
            Debug.LogWarning($"Material '{mat.name}' has no _Color property, can't set alpha.");
        }
    }
}