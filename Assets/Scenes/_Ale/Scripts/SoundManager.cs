using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class SoundManager : MonoBehaviour
{
    public static SoundManager I { get; private set; }

    [Header("Music Source")]
    [SerializeField] private AudioSource musicSource;

    [Header("Startup Music")]
    [SerializeField] private AudioClip soundtrack;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float startVolume = 1f;
    [SerializeField] private float fadeInTime = 1.0f;

    [Header("Loop")]
    [SerializeField] private bool loop = true;

    [Header("Game Over Pitch")]
    [Tooltip("Pitch finale quando vai in game over. Esempio: 0.65")]
    [SerializeField] private float gameOverTargetPitch = 0.65f;

    [Tooltip("Secondi per arrivare al pitch finale")]
    [SerializeField] private float gameOverPitchTime = 1.0f;

    [Tooltip("Curva di interpolazione (0..1). Se null, lineare.")]
    [SerializeField] private AnimationCurve gameOverPitchCurve =
        AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Coroutine fadeRoutine;
    private Coroutine pitchRoutine;

    private float basePitch = 1f;
    private float baseVolume = 1f;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;

        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
            if (musicSource == null)
                musicSource = gameObject.AddComponent<AudioSource>();
        }

        // setup base
        musicSource.playOnAwake = false;
        musicSource.loop = loop;

        basePitch = musicSource.pitch;
        baseVolume = startVolume;
    }

    private void Start()
    {
        if (!playOnStart) return;

        if (soundtrack != null)
            PlaySoundtrack(soundtrack, fadeInTime, startVolume);
        else if (musicSource.clip != null)
            PlaySoundtrack(musicSource.clip, fadeInTime, startVolume);
    }

    // =========================================================
    // PUBLIC API
    // =========================================================

    /// <summary>
    /// Avvia la soundtrack in loop con fade-in.
    /// </summary>
    public void PlaySoundtrack(AudioClip clip, float fadeInSeconds, float volume = 1f, bool restartIfSame = false)
    {
        if (clip == null) return;

        if (!restartIfSame && musicSource.isPlaying && musicSource.clip == clip)
            return;

        if (debugLogs) Debug.Log($"[SoundManager] PlaySoundtrack: {clip.name}");

        StopCoroutines();

        musicSource.clip = clip;
        musicSource.loop = loop;

        baseVolume = Mathf.Clamp01(volume);
        basePitch = 1f;

        musicSource.pitch = basePitch;
        musicSource.volume = 0f;

        musicSource.Play();

        fadeRoutine = StartCoroutine(FadeVolume(0f, baseVolume, Mathf.Max(0f, fadeInSeconds)));
    }

    /// <summary>
    /// Chiamalo quando vai in game over: la musica “si siede” di pitch.
    /// </summary>
    public void GameOverDownPitch()
    {
        if (debugLogs) Debug.Log("[SoundManager] GameOverDownPitch");

        if (!musicSource.isPlaying) return;

        if (pitchRoutine != null)
        {
            StopCoroutine(pitchRoutine);
            pitchRoutine = null;
        }

        pitchRoutine = StartCoroutine(PitchTo(gameOverTargetPitch, gameOverPitchTime, gameOverPitchCurve));
    }

    /// <summary>
    /// Reset musica a pitch normale (utile per restart run / ritorno al menu).
    /// </summary>
    public void ResetPitch(float time = 0f)
    {
        if (!musicSource) return;

        if (pitchRoutine != null)
        {
            StopCoroutine(pitchRoutine);
            pitchRoutine = null;
        }

        if (time <= 0f)
        {
            musicSource.pitch = 1f;
            return;
        }

        pitchRoutine = StartCoroutine(PitchTo(1f, time, AnimationCurve.Linear(0, 0, 1, 1)));
    }

    /// <summary>
    /// (Opzionale) Fade-out e stop.
    /// </summary>
    public void StopMusic(float fadeOutSeconds = 0.5f)
    {
        if (!musicSource.isPlaying) return;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        fadeRoutine = StartCoroutine(FadeOutAndStop(Mathf.Max(0f, fadeOutSeconds)));
    }

    // =========================================================
    // INTERNAL
    // =========================================================

    private void StopCoroutines()
    {
        if (fadeRoutine != null) { StopCoroutine(fadeRoutine); fadeRoutine = null; }
        if (pitchRoutine != null) { StopCoroutine(pitchRoutine); pitchRoutine = null; }
    }

    private IEnumerator FadeVolume(float from, float to, float seconds)
    {
        if (seconds <= 0f)
        {
            musicSource.volume = to;
            yield break;
        }

        float t = 0f;
        musicSource.volume = from;

        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / seconds);
            musicSource.volume = Mathf.Lerp(from, to, a);
            yield return null;
        }

        musicSource.volume = to;
    }

    private IEnumerator FadeOutAndStop(float seconds)
    {
        float start = musicSource.volume;

        if (seconds <= 0f)
        {
            musicSource.Stop();
            yield break;
        }

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / seconds);
            musicSource.volume = Mathf.Lerp(start, 0f, a);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
    }

    private IEnumerator PitchTo(float targetPitch, float seconds, AnimationCurve curve)
    {
        float from = musicSource.pitch;
        float to = Mathf.Clamp(targetPitch, -3f, 3f);

        if (seconds <= 0f)
        {
            musicSource.pitch = to;
            yield break;
        }

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / seconds);
            float k = (curve != null) ? curve.Evaluate(a) : a;
            musicSource.pitch = Mathf.Lerp(from, to, k);
            yield return null;
        }

        musicSource.pitch = to;
    }
}