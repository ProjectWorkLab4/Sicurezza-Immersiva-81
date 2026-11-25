using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Generic hinged door / hatch (used by the electrical panel and other props).
///
/// Unlike <see cref="PortaUscita"/> and <see cref="FinestraApribile"/> it has no
/// side effect on the scenario: it only rotates the panel around its hinge and
/// fires an optional event, so it can be reused for any cabinet.
/// </summary>
[DisallowMultipleComponent]
public class SportelloApribile : MonoBehaviour
{
    [Header("Stato")]
    [SerializeField] bool apertoAllAvvio = false;

    [Header("Riferimenti")]
    [Tooltip("Maniglia dello sportello (VRInteractableButton sopra un Poke/Ray/Grab interactable).")]
    [SerializeField] VRInteractableButton maniglia;

    [Header("Anta")]
    [Tooltip("Transform dell'anta. Il suo pivot deve stare sulla cerniera.")]
    [SerializeField] Transform anta;
    [Tooltip("Angolo di apertura in gradi attorno all'asse Y locale.")]
    [SerializeField] float angoloAperto = 100f;
    [SerializeField] float velocita = 160f;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip suonoApertura;
    [SerializeField] AudioClip suonoChiusura;

    [Header("Eventi")]
    [SerializeField] UnityEvent onAperto = new UnityEvent();
    [SerializeField] UnityEvent onChiuso = new UnityEvent();

    /// <summary>Fired with the new state (true = open).</summary>
    public event System.Action<bool> CambioStato;

    public bool Aperto { get; private set; }

    Quaternion _rotazioneChiusa;
    Quaternion _rotazioneAperta;
    Quaternion _target;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (anta != null)
        {
            _rotazioneChiusa = anta.localRotation;
            _rotazioneAperta = _rotazioneChiusa * Quaternion.Euler(0f, angoloAperto, 0f);
            _target = _rotazioneChiusa;
        }
    }

    void OnEnable()
    {
        if (maniglia != null)
        {
            maniglia.Selected += HandleManiglia;
        }
    }

    void OnDisable()
    {
        if (maniglia != null)
        {
            maniglia.Selected -= HandleManiglia;
        }
    }

    void Start()
    {
        Aperto = apertoAllAvvio;

        if (anta != null)
        {
            _target = Aperto ? _rotazioneAperta : _rotazioneChiusa;
            anta.localRotation = _target;
        }
    }

    void Update()
    {
        if (anta == null)
        {
            return;
        }

        float passo = Mathf.Max(1f, velocita) * Time.deltaTime;
        anta.localRotation = Quaternion.RotateTowards(anta.localRotation, _target, passo);
    }

    /// <summary>Opens the door.</summary>
    public void Apri()
    {
        ImpostaAperto(true);
    }

    /// <summary>Closes the door.</summary>
    public void Chiudi()
    {
        ImpostaAperto(false);
    }

    /// <summary>Switches the door to the opposite state.</summary>
    public void Toggle()
    {
        ImpostaAperto(!Aperto);
    }

    /// <summary>Sets the door state.</summary>
    public void ImpostaAperto(bool aperto)
    {
        if (aperto == Aperto)
        {
            return;
        }

        Aperto = aperto;
        _target = aperto ? _rotazioneAperta : _rotazioneChiusa;

        if (audioSource != null)
        {
            AudioClip clip = aperto ? suonoApertura : suonoChiusura;

            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        if (aperto)
        {
            onAperto.Invoke();
        }
        else
        {
            onChiuso.Invoke();
        }

        CambioStato?.Invoke(aperto);
    }

    void HandleManiglia(VRInteractableButton pulsante)
    {
        Toggle();
    }
}
