using SI81.UI;
using UnityEngine;

/// <summary>
/// Ponte dei messaggi per gli script di G_Simone.
///
/// Conserva i vecchi UnityEvent e le vecchie chiamate Show(...), ma instrada il testo allo
/// ScenarioController: sottotitoli con Subtitle e avvisi con Alert della UI comune (guida §5).
/// Non crea Canvas, camera né input.
/// </summary>
[DisallowMultipleComponent]
public class VRMessageDisplay : MonoBehaviour
{
    /// <summary>Ultima istanza disponibile, se presente.</summary>
    public static VRMessageDisplay Instance { get; private set; }

    [Tooltip("ScenarioController su MNG_Scenario. Se vuoto viene cercato in scena.")]
    [SerializeField] ScenarioController scenario;

    [Tooltip("Titolo predefinito dei messaggi.")]
    [SerializeField] string defaultTitle = string.Empty;

    [Tooltip("Durata predefinita in secondi.")]
    [SerializeField] float defaultDuration = 4f;

    /// <summary>Scenario condiviso usato per parlare alla UI comune.</summary>
    ScenarioController Controller
    {
        get
        {
            if (scenario == null)
            {
                scenario = FindFirstObjectByType<ScenarioController>(FindObjectsInactive.Include);
            }

            return scenario;
        }
    }

    /// <summary>La visibilità è della UI comune; la proprietà resta per compatibilità.</summary>
    public bool IsVisible => false;

    /// <summary>Restituisce l'istanza esistente o ne crea una senza UI locale.</summary>
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
        VRMessageDisplay display = host.AddComponent<VRMessageDisplay>();
        Instance = display;
        return display;
    }

    /// <summary>Registra l'istanza quando viene creata.</summary>
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    /// <summary>Toglie l'istanza quando viene distrutta.</summary>
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>Mostra un testo con titolo e durata predefiniti.</summary>
    public void Show(string body)
    {
        Show(defaultTitle, body, defaultDuration);
    }

    /// <summary>Mostra un testo con titolo e durata predefiniti.</summary>
    public void Show(string title, string body)
    {
        Show(title, body, defaultDuration);
    }

    /// <summary>Mostra un sottotitolo tramite lo scenario.</summary>
    public void Show(string title, string body, float duration)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return;
        }

        string speaker = string.IsNullOrWhiteSpace(title) ? "Sistema" : title;
        Controller?.Subtitle(speaker, body, Mathf.Max(1f, duration));
    }

    /// <summary>Mostra un avviso tramite lo scenario.</summary>
    public void MostraAvviso(string testo, string icona = null)
    {
        if (!string.IsNullOrWhiteSpace(testo))
        {
            Controller?.Alert(testo, icona);
        }
    }
}
