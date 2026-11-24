using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space "VR screen" used to print short texts in front of the player.
/// The panel follows the head while visible and hides itself after a few seconds.
/// Add this component to an empty GameObject in the scene, or just call
/// <see cref="GetOrCreate"/> / <see cref="Show(string)"/> from another script:
/// a panel is created automatically when missing.
/// </summary>
[DisallowMultipleComponent]
public class VRMessageDisplay : MonoBehaviour
{
    struct Message
    {
        public string Title;
        public string Body;
        public float Duration;
    }

    [Header("Ancoraggio")]
    [Tooltip("Transform seguito dal pannello (di norma la testa / Main Camera). Se vuoto viene usata Camera.main.")]
    [SerializeField] Transform followTarget;
    [SerializeField] bool followHead = true;
    [Tooltip("Distanza in metri dal viso del giocatore.")]
    [SerializeField] float distance = 0.65f;
    [SerializeField] Vector3 offset = new Vector3(0f, 0.02f, 0f);
    [SerializeField] float followSharpness = 10f;

    [Header("Pannello")]
    [SerializeField] Vector2 panelSizeMeters = new Vector2(0.62f, 0.30f);
    [Tooltip("Risoluzione virtuale del canvas: quanti pixel corrispondono a un metro.")]
    [SerializeField] int pixelsPerMeter = 1000;
    [SerializeField] int sortingOrder = 200;
    [SerializeField] Color backgroundColor = new Color(0.04f, 0.05f, 0.09f, 0.92f);
    [SerializeField] Color accentColor = new Color(1f, 0.62f, 0.12f, 1f);

    [Header("Testi")]
    [SerializeField] string defaultTitle = "";
    [SerializeField] float defaultDuration = 4f;
    [SerializeField] Color titleColor = Color.white;
    [SerializeField] Color bodyColor = new Color(0.88f, 0.90f, 0.94f);

    [Header("Coda dei messaggi")]
    [Tooltip("Quanti messaggi possono restare in attesa mentre uno e' gia' visibile.")]
    [SerializeField] int maxQueuedMessages = 4;

    /// <summary>Most recently created/available instance, if any.</summary>
    public static VRMessageDisplay Instance { get; private set; }

    public bool IsVisible => _visible;

    Canvas _canvas;
    CanvasGroup _group;
    TextMeshProUGUI _titleLabel;
    TextMeshProUGUI _bodyLabel;
    readonly Queue<Message> _queue = new Queue<Message>();
    Coroutine _hideRoutine;
    bool _visible;

    /// <summary>
    /// Returns the display already in the scene, or creates a new one.
    /// </summary>
    public static VRMessageDisplay GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        VRMessageDisplay existing = FindFirstObjectByType<VRMessageDisplay>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject host = new GameObject("VRMessageDisplay");
        return host.AddComponent<VRMessageDisplay>();
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        EnsurePanel();
        _canvas.enabled = false;
        _group.alpha = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>Shows a body text with the default title and duration.</summary>
    public void Show(string body)
    {
        Show(defaultTitle, body, defaultDuration);
    }

    /// <summary>Shows a titled text with the default duration.</summary>
    public void Show(string title, string body)
    {
        Show(title, body, defaultDuration);
    }

    /// <summary>Shows a titled text; <paramref name="duration"/> is in seconds.</summary>
    public void Show(string title, string body, float duration)
    {
        Message message = new Message
        {
            Title = title,
            Body = body,
            Duration = Mathf.Max(0.1f, duration)
        };

        if (!_visible)
        {
            Present(message);
            return;
        }

        if (_queue.Count >= maxQueuedMessages)
        {
            _queue.Dequeue();
        }

        _queue.Enqueue(message);
    }

    /// <summary>Hides the panel immediately (optionally clearing queued messages).</summary>
    public void Hide(bool clearQueue = true)
    {
        if (clearQueue)
        {
            _queue.Clear();
        }

        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        if (_canvas != null && _canvas.enabled)
        {
            _hideRoutine = StartCoroutine(FadeOut(0.15f));
        }
    }

    void Present(Message message)
    {
        EnsurePanel();

        _titleLabel.text = message.Title ?? string.Empty;
        _bodyLabel.text = message.Body ?? string.Empty;

        _visible = true;
        _canvas.enabled = true;
        _group.alpha = 1f;

        PlaceAtTarget(instant: true);

        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
        }

        _hideRoutine = StartCoroutine(HideAfter(message.Duration));
    }

    IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        yield return FadeOut(0.18f);
        _hideRoutine = null;

        if (_queue.Count > 0)
        {
            Present(_queue.Dequeue());
        }
    }

    IEnumerator FadeOut(float duration)
    {
        float start = _group.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(start, 0f, elapsed / duration);
            yield return null;
        }

        _group.alpha = 0f;
        _visible = false;
        _canvas.enabled = false;
        _hideRoutine = null;
    }

    void LateUpdate()
    {
        if (!followHead || !_visible)
        {
            return;
        }

        PlaceAtTarget(instant: false);
    }

    void PlaceAtTarget(bool instant)
    {
        Transform target = followTarget != null
            ? followTarget
            : (Camera.main != null ? Camera.main.transform : null);

        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + target.rotation * (Vector3.forward * distance + offset);
        Quaternion desiredRotation = target.rotation;

        if (instant)
        {
            transform.SetPositionAndRotation(desiredPosition, desiredRotation);
            return;
        }

        float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, blend);
    }

    void EnsurePanel()
    {
        if (_canvas != null)
        {
            return;
        }

        GameObject panel = new GameObject("MessagePanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        panel.transform.SetParent(transform, false);

        _canvas = panel.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = sortingOrder;

        _group = panel.GetComponent<CanvasGroup>();
        _group.interactable = false;
        _group.blocksRaycasts = false;

        RectTransform panelRect = (RectTransform)panel.transform;
        panelRect.sizeDelta = panelSizeMeters * Mathf.Max(1, pixelsPerMeter);
        panelRect.localScale = Vector3.one * (1f / Mathf.Max(1, pixelsPerMeter));

        Image background = CreateImage(panelRect, "Background", backgroundColor);
        Stretch(background.rectTransform);

        Image accent = CreateImage(panelRect, "Accent", accentColor);
        RectTransform accentRect = accent.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.sizeDelta = new Vector2(0f, 6f);
        accentRect.anchoredPosition = Vector2.zero;

        _titleLabel = CreateText(panelRect, "Title", 34f, titleColor, FontStyles.Bold);
        RectTransform titleRect = _titleLabel.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(-48f, 60f);
        titleRect.anchoredPosition = new Vector2(0f, -16f);
        _titleLabel.alignment = TextAlignmentOptions.Left;

        _bodyLabel = CreateText(panelRect, "Body", 24f, bodyColor, FontStyles.Normal);
        RectTransform bodyRect = _bodyLabel.rectTransform;
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(24f, 18f);
        bodyRect.offsetMax = new Vector2(-24f, -84f);
        _bodyLabel.alignment = TextAlignmentOptions.TopLeft;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, float size, Color color, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = size;
        label.color = color;
        label.fontStyle = style;
        label.text = string.Empty;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        return label;
    }
}
