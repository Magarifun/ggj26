using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class KeyBubble : MonoBehaviour
{
    public KeyCode cheat = KeyCode.K;
    public float brownianSpeed;
    public float perlinSpeed;
    private Vector3 anchor;
    private static KeyBubble instance;
    public static KeyBubble Instance => instance;
    private bool restoring = false;

    [Header("PostFX Punch (URP Volume)")]
    [SerializeField] private string globalVolumeName = "Global Volume";
    [SerializeField] private float punchDuration = 0.18f;
    [SerializeField, Range(0f, 100f)] private float intensityPeak = 30f;
    [SerializeField] private AnimationCurve punchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Runner che resta attivo anche quando KeyBubble si disattiva
    private class CoroutineHost : MonoBehaviour { }

    public void SetAnchor()
    {
        transform.position = new Vector3(Random.Range(-4.5f, 4.5f), transform.position.y, transform.position.z);
        anchor = transform.position;
    }

    private void Start()
    {
        if (instance != null)
        {
            Destroy(this);
            return;
        }
        instance = this;
        SetAnchor();
    }

    private void BackToBrownian()
    {
        restoring = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(cheat))
        {
            PickKey();
        }

        float venturing = Vector3.Distance(transform.position, anchor);
        if (restoring || venturing > 0.5f)
        {
            Vector3 toCenter = (anchor - transform.position) * 2;
            transform.Translate(brownianSpeed * Time.deltaTime * toCenter);

            if (!restoring)
            {
                restoring = true;
                Invoke(nameof(BackToBrownian), 0.25f);
            }
        }

        float perlinAngle = 360f *
            Mathf.PerlinNoise(transform.position.x * Time.time * perlinSpeed,
                              transform.position.y * Time.time * perlinSpeed);

        Vector3 noise = new(Mathf.Cos(perlinAngle * Mathf.Deg2Rad), Mathf.Sin(perlinAngle * Mathf.Deg2Rad), 0f);
        transform.Translate(brownianSpeed * Time.deltaTime * noise);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("KeyBubble collided with " + collision.gameObject.name);
        if (collision.gameObject.CompareTag("Player"))
        {
            PickKey();
        }
    }

  private void PickKey()
{
    // ✅ FX prima di spegnere la bubble
    TriggerScreenSpaceLensFlarePunch();

    // ✅ SFX
    PlayPickupSfx();

    Chase.Instance.UnlockDoor();
    SetAnchor();
    gameObject.SetActive(false);
}

private void PlayPickupSfx()
{
    var sfx = GetComponent<PlayRandomSoundFromArray>();
    if (sfx == null) sfx = GetComponentInChildren<PlayRandomSoundFromArray>(true);
    if (sfx == null) return;

    // Cambia QUI col nome esatto del metodo nel tuo script:
    sfx.PlayRandom(); 
}

    public void Display(bool state)
    {
        GetComponentInChildren<MeshRenderer>().enabled = state;
    }

    // -------------------- FX --------------------

    private void TriggerScreenSpaceLensFlarePunch()
    {
        GameObject gv = GameObject.Find(globalVolumeName);
        if (gv == null) return;

        Volume volume = gv.GetComponentInChildren<Volume>(true);
        if (volume == null || volume.profile == null) return;

        if (!volume.profile.TryGet(out ScreenSpaceLensFlare ssFlare) || ssFlare == null) return;

        // runner persistente sul volume GO
        var host = gv.GetComponent<CoroutineHost>();
        if (host == null) host = gv.AddComponent<CoroutineHost>();

        host.StopAllCoroutines();
        host.StartCoroutine(PunchLensFlareIntensity(ssFlare, punchDuration, intensityPeak, punchCurve));
    }

    private System.Collections.IEnumerator PunchLensFlareIntensity(
        ScreenSpaceLensFlare ssFlare,
        float dur,
        float peak,
        AnimationCurve curve)
    {
        // salva stato
        bool prevOverride = ssFlare.intensity.overrideState;
        float baseValue = ssFlare.intensity.value;

        ssFlare.intensity.overrideState = true;

        float half = Mathf.Max(0.01f, dur) * 0.5f;

        // up
        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            float u = t / half;
            float e = curve != null ? curve.Evaluate(u) : u;
            ssFlare.intensity.value = Mathf.Lerp(baseValue, peak, e);
            yield return null;
        }

        // down
        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            float u = t / half;
            float e = curve != null ? curve.Evaluate(u) : u;
            ssFlare.intensity.value = Mathf.Lerp(peak, baseValue, e);
            yield return null;
        }

        // restore
        ssFlare.intensity.value = baseValue;
        ssFlare.intensity.overrideState = prevOverride;
    }
}