using SI81.UI;
using UnityEngine;

/// <summary>
/// Backwards-compatible message bridge for the G_Simone scripts.
///
/// It no longer creates a Canvas: messages are forwarded to the shared HUD owned by
/// <c>SCN_Core</c> through <see cref="HUDService"/>. The UnityEvent wiring and the
/// <see cref="Show(string,string,float)"/> calls used by the scenarios are unchanged.
/// </summary>
[DisallowMultipleComponent]
public class VRMessageDisplay : MonoBehaviour
{
    /// <summary>Most recently created/available instance, if any.</summary>
    public static VRMessageDisplay Instance { get; private set; }

    [Tooltip("Titolo predefinito dei messaggi.")]
    [SerializeField] string defaultTitle = string.Empty;
    [Tooltip("Durata predefinita in secondi.")]
    [SerializeField] float defaultDuration = 4f;

    /// <summary>Kept for source compatibility; the shared HUD owns visibility.</summary>
    public bool IsVisible => false;

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

    /// <summary>Shows a titled text through the shared subtitle channel.</summary>
    public void Show(string title, string body, float duration)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return;
        }

        string speaker = string.IsNullOrWhiteSpace(title) ? "Sistema" : title;
        HUDService.ShowSubtitle(speaker, body, Mathf.Max(1f, duration));
    }

    /// <summary>Forwarded to the shared alert channel.</summary>
    public void MostraAvviso(string testo, string icona = null)
    {
        if (!string.IsNullOrWhiteSpace(testo))
        {
            HUDService.ShowEventAlert(testo, icona);
        }
    }
}
