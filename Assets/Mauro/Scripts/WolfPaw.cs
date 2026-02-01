using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WolfPaw : MonoBehaviour
{
    [Header("Paw")]
    public float duration = 0.5f;
    public float length = 5.0f;
    public float range = 2f;

    private float speed;
    private readonly List<GameObject> cards = new();
    private float leftmostCardX = 0f;
    private float rightmostCardX = 0f;
    private Vector3 downVector;

    [Header("Post FX (Global Volume)")]
    [Tooltip("Nome del GameObject in scena che contiene il Volume.")]
    public string globalVolumeName = "Global Volume";

    [Tooltip("Quanto dura il pulse FX (secondi).")]
    public float fxPulseDuration = 0.18f;

    [Tooltip("Picco massimo dell'intensità della Chromatic Aberration (0..1).")]
    [Range(0f, 1f)]
    public float chromaPeak = 0.6f;

    [Tooltip("Curva del pulse (0..1). Default: ease in/out.")]
    public AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Volume globalVolume;
    private ChromaticAberration chroma;
    private float chromaBase;
    private bool chromaHasOverride;
    private bool chromaOngoing;

    void Start()
    {
        speed = length / duration;

        CacheCards();
        PlaceRandomXOverCards();

        downVector = transform.position;

        // Trigger FX pulse quando spawna/si attiva la paw
        SetupGlobalVolume();
        TriggerChromaticPulse();

        Invoke(nameof(End), duration);
    }

    private void End()
    {
        Destroy(gameObject);
    }

    void Update()
    {
        downVector += (speed * Time.deltaTime * Vector3.down);

        foreach (GameObject card in cards)
        {
            if (card == null) continue;

            // Se non usi VisualScripting, puoi togliere IsDestroyed e usare solo card == null
            if (card.TryGetComponent<Transform>(out var tr))
            {
                float distance = Vector3.Distance(downVector, tr.position);
                if (distance < range)
                {
                    Destroy(card);
                }
            }
        }
    }

    // -----------------------------
    // Cards
    // -----------------------------
    private void CacheCards()
    {
        CardPoolManager cardPool = GameObject.FindFirstObjectByType<CardPoolManager>();
        if (cardPool == null) return;

        CardDragAndDrop2D_SnapSortingErase[] allCards =
            cardPool.GetComponentsInChildren<CardDragAndDrop2D_SnapSortingErase>();

        foreach (var card in allCards)
        {
            if (!card.isPlaced) continue;

            cards.Add(card.gameObject);

            float x = card.transform.position.x;
            if (cards.Count == 1)
            {
                leftmostCardX = x;
                rightmostCardX = x;
            }
            else
            {
                if (x < leftmostCardX) leftmostCardX = x;
                if (x > rightmostCardX) rightmostCardX = x;
            }
        }
    }

    private void PlaceRandomXOverCards()
    {
        if (cards.Count == 0) return;

        float randomX = Random.Range(leftmostCardX, rightmostCardX);
        transform.position = new Vector3(randomX, transform.position.y, transform.position.z);
    }

    // -----------------------------
    // Post FX
    // -----------------------------
    private void SetupGlobalVolume()
    {
        var go = GameObject.Find(globalVolumeName);
        if (go == null)
        {
            Debug.LogWarning($"[WolfPaw] Non trovo GameObject '{globalVolumeName}' in scena.");
            return;
        }

        globalVolume = go.GetComponentInChildren<Volume>(true);
        if (globalVolume == null)
        {
            Debug.LogWarning($"[WolfPaw] Trovato '{globalVolumeName}' ma non contiene un componente Volume.");
            return;
        }

        if (globalVolume.profile == null)
        {
            Debug.LogWarning("[WolfPaw] Il Volume non ha un Profile assegnato.");
            return;
        }

        // Prova a prendere ChromaticAberration dal profile
        if (!globalVolume.profile.TryGet(out chroma) || chroma == null)
        {
            Debug.LogWarning("[WolfPaw] Nel Profile del Volume non c'è ChromaticAberration.");
            return;
        }

        // Salva base (se override attivo) e garantisci override su intensity
        chromaHasOverride = chroma.intensity.overrideState;
        chromaBase = chroma.intensity.value;

        chroma.intensity.overrideState = true;
    }

    private void TriggerChromaticPulse()
    {
        if (chroma == null) return;
        if (chromaOngoing ==false){
            chromaOngoing = true;
            StopCoroutine(nameof(ChromaticPulseCoroutine));
            StartCoroutine(ChromaticPulseCoroutine());
        }
    }

    private System.Collections.IEnumerator ChromaticPulseCoroutine()
    {
        // Pulse: base -> peak -> base
        float half = Mathf.Max(0.01f, fxPulseDuration) * 0.5f;

        // salita
        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            float u = t / half; // 0..1
            float e = pulseCurve != null ? pulseCurve.Evaluate(u) : u;
            chroma.intensity.value = Mathf.Lerp(chromaBase, chromaPeak, e);
            yield return null;
        }

        // discesa
        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            float u = t / half; // 0..1
            float e = pulseCurve != null ? pulseCurve.Evaluate(u) : u;
            chroma.intensity.value = Mathf.Lerp(chromaPeak, chromaBase, e);
            yield return null;
        }

        // ripristino finale
        chroma.intensity.value = chromaBase;
        chroma.intensity.overrideState = chromaHasOverride;
        chromaOngoing = false;
    }
}