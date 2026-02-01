using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Small loop tweener for "interactable" hint animations (scale/rotation ping-pong).
/// Stops when CardDragAndDrop2D_SnapSortingErase.isPlaced becomes true.
/// </summary>
[ExecuteAlways]
public class CardInteractionTweener : MonoBehaviour
{
    public enum TweenType
    {
        None,
        PulseScalePingPong,
        PulseRotationPingPong
    }

    [Header("Placement Gate")]
    [Tooltip("Where to look for CardDragAndDrop2D_SnapSortingErase.")]
    [SerializeField] private bool searchInParents = false;

    [Tooltip("Optional explicit reference. If null, it will auto-find.")]
    [SerializeField] private CardDragAndDrop2D_SnapSortingErase dragDrop;

    [Header("Tween")]
    [SerializeField] private TweenType tweenType = TweenType.PulseScalePingPong;

    [Tooltip("Loops per second. 1 = one full back-and-forth per second (approx).")]
    [Min(0.01f)]
    [SerializeField] private float speed = 1.5f;

    [Tooltip("Ease shape. X: 0..1. Y: 0..1.")]
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Scale Pulse")]
    [Tooltip("Scale multiplier added on top of the base scale (e.g. 0.08 = +8%).")]
    [SerializeField] private float scaleAmplitude = 0.08f;

    [Header("Rotation Pulse")]
    [Tooltip("Degrees added/subtracted around Z (2D).")]
    [SerializeField] private float rotationAmplitudeDeg = 4f;

    [Header("Stop Behaviour")]
    [Tooltip("When placed, reset scale/rotation back to original.")]
    [SerializeField] private bool resetOnStop = true;

    [Tooltip("When placed, disable this component (recommended).")]
    [SerializeField] private bool disableOnStop = true;

    // cached base transform values
    private Vector3 baseLocalScale;
    private Quaternion baseLocalRotation;

    // time tracking
    private float t;
#if UNITY_EDITOR
    private double lastEditorTime;
#endif

    private void OnEnable()
    {
        CacheBase();
        ResolveReference();
#if UNITY_EDITOR
        lastEditorTime = EditorApplication.timeSinceStartup;
#endif
    }

    private void OnDisable()
    {
        // Optional: keep the current state or restore when disabled.
        // If you always want restore on disable, uncomment:
        // RestoreBase();
    }

    private void OnValidate()
    {
        speed = Mathf.Max(0.01f, speed);
        ResolveReference();
        // Avoid snapping to wrong base when tweaking fields:
        if (!Application.isPlaying)
            CacheBase();
    }

    private void CacheBase()
    {
        baseLocalScale = transform.localScale;
        baseLocalRotation = transform.localRotation;
    }

    private void ResolveReference()
    {
        if (dragDrop != null) return;

        dragDrop = searchInParents
            ? GetComponentInParent<CardDragAndDrop2D_SnapSortingErase>()
            : GetComponent<CardDragAndDrop2D_SnapSortingErase>();
    }

    private float GetDeltaTime()
    {
        if (Application.isPlaying) return Time.unscaledDeltaTime;

#if UNITY_EDITOR
        double now = EditorApplication.timeSinceStartup;
        float dt = (float)(now - lastEditorTime);
        lastEditorTime = now;
        // In edit mode dt can be huge when tabbing; clamp to keep stable.
        return Mathf.Clamp(dt, 0f, 0.05f);
#else
        return 0f;
#endif
    }

    private void Update()
    {
        // Always keep ref fresh if user adds component after
        if (dragDrop == null) ResolveReference();

        // If placed => stop
        if (dragDrop != null && dragDrop.isPlaced || dragDrop.isDragging)
        {
            if (resetOnStop) RestoreBase();

            if (disableOnStop)
                enabled = false;

            return;
        }

        // No tween selected
        if (tweenType == TweenType.None) return;

        // Advance time
        float dt = GetDeltaTime();
        if (dt <= 0f) return;

        t += dt * speed;

        // ping-pong 0..1
        float u = Mathf.PingPong(t, 1f);
        float e = ease != null ? ease.Evaluate(u) : u;

        ApplyTween(e);
    }

    private void ApplyTween(float e)
    {
        switch (tweenType)
        {
            case TweenType.PulseScalePingPong:
            {
                float mul = 1f + (scaleAmplitude * (e * 2f - 1f)); // -amp..+amp
                transform.localScale = baseLocalScale * mul;
                break;
            }

            case TweenType.PulseRotationPingPong:
            {
                float deg = rotationAmplitudeDeg * (e * 2f - 1f); // -amp..+amp
                transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, deg);
                break;
            }
        }
    }

    private void RestoreBase()
    {
        transform.localScale = baseLocalScale;
        transform.localRotation = baseLocalRotation;
    }
}