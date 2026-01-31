using System.Collections.Generic;
using UnityEngine;

public class CardTileOverlapEraser2D : MonoBehaviour
{
    [Header("Detection")]
    public LayerMask tileLayer;
    public bool includeTriggers = true;

    [Tooltip("Se hai falsi positivi, metti 0.02 - 0.05")]
    public float minOverlapDistance = 0.001f;

    private Collider2D[] myTileColliders;

    private ContactFilter2D filter;
    private readonly List<Collider2D> results = new List<Collider2D>(32);

    private void Awake()
    {
        myTileColliders = GetComponentsInChildren<Collider2D>();

        filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = tileLayer,
            useTriggers = includeTriggers
        };
    }

    public void EraseOverlappedTiles()
    {
        if (myTileColliders == null || myTileColliders.Length == 0)
        {
            Debug.LogWarning("Nessuna tile con Collider2D trovata nella Card.");
            return;
        }

        foreach (var myTile in myTileColliders)
        {
            if (myTile == null) continue;

            results.Clear();
            myTile.Overlap(filter, results);

            foreach (var hit in results)
            {
                if (hit == null) continue;

                // ignora le tile della stessa card
                if (hit.transform.IsChildOf(transform))
                    continue;

                // opzionale: evita casi borderline
                float dist = Vector2.Distance(hit.bounds.center, myTile.bounds.center);
                if (dist > minOverlapDistance)
                {
                    // in pratica se sono vicine ma non davvero sovrapposte, puoi saltare
                    // (se vuoi cancellazione aggressiva, puoi rimuovere questo controllo)
                }

                Destroy(hit.gameObject);
            }
        }
    }
}