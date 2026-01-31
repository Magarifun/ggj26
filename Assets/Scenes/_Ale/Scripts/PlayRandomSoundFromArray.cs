using UnityEngine;

[DisallowMultipleComponent]
public class PlayRandomSoundFromArray : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip[] clips;

    [Header("Options")]
    [SerializeField] private bool playOnEnable = false;
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private bool randomizePitch = false;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    [SerializeField] private bool randomizeVolume = false;
    [SerializeField] private Vector2 volumeRange = new Vector2(0.85f, 1.0f);

    [Header("Avoid Repeats")]
    [SerializeField] private bool avoidImmediateRepeat = true;

    private int lastIndex = -1;

    private void Awake()
    {
        if (source == null)
            source = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (playOnEnable)
            PlayRandom();
    }

    private void Start()
    {
        if (playOnStart)
            PlayRandom();
    }

    // Call this from UnityEvent / other scripts
    public void PlayRandom()
    {
        if (source == null || clips == null || clips.Length == 0) return;

        int idx = Random.Range(0, clips.Length);

        if (avoidImmediateRepeat && clips.Length > 1)
        {
            int safety = 8;
            while (idx == lastIndex && safety-- > 0)
                idx = Random.Range(0, clips.Length);
        }

        lastIndex = idx;

        float prevPitch = source.pitch;
        float prevVolume = source.volume;

        if (randomizePitch)
            source.pitch = Random.Range(pitchRange.x, pitchRange.y);

        if (randomizeVolume)
            source.volume = Random.Range(volumeRange.x, volumeRange.y);

        source.PlayOneShot(clips[idx]);

        // restore (PlayOneShot doesn't change pitch/volume mid-play, but keeps settings for next)
        source.pitch = prevPitch;
        source.volume = prevVolume;
    }
}