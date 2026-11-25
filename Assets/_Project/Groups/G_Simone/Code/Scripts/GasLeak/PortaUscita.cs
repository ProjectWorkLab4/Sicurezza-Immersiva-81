using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Exit door of the office (GGD: "Porta d'uscita - Apertura (fine livello)").
///
/// The player opens it with the handle (poke / ray through the Meta XR Interaction SDK)
/// and walks through the <see cref="AreaUscita"/> trigger placed behind it.
/// </summary>
[DisallowMultipleComponent]
public class PortaUscita : MonoBehaviour
{
    [Header("Stato")]
    [SerializeField] bool apertaAllAvvio = false;

    [Header("Riferimenti")]
    [SerializeField] GestoreScenarioGasLeak gestore;
    [Tooltip("Maniglia della porta (VRInteractableButton sopra un Poke/Ray/Grab interactable).")]
    [SerializeField] VRInteractableButton maniglia;

    [Header("Anta")]
    [Tooltip("Transform dell'anta. Il suo pivot deve stare sui cardini.")]
    [SerializeField] Transform anta;
    [SerializeField] float angoloAperto = 95f;
    [SerializeField] float velocita = 160f;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip suonoApertura;

    [Header("Eventi")]
    [SerializeField] UnityEvent onAperta = new UnityEvent();

    /// <summary>Fired the first time the door opens.</summary>
    public event System.Action Aperta;

    public bool IsAperta { get; private set; }

    Quaternion _rotazioneChiusa;
    Quaternion _rotazioneAperta;
    Quaternion _target;
    bool _eventoInviato;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (gestore == null)
        {
            gestore = FindFirstObjectByType<GestoreScenarioGasLeak>();
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
        IsAperta = apertaAllAvvio;

        if (anta != null)
        {
            _target = IsAperta ? _rotazioneAperta : _rotazioneChiusa;
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
        if (IsAperta)
        {
            return;
        }

        IsAperta = true;
        _target = _rotazioneAperta;

        if (audioSource != null && suonoApertura != null)
        {
            audioSource.PlayOneShot(suonoApertura);
        }

        if (!_eventoInviato)
        {
            _eventoInviato = true;
            onAperta.Invoke();
            Aperta?.Invoke();
        }
    }

    /// <summary>Closes the door.</summary>
    public void Chiudi()
    {
        IsAperta = false;
        _target = _rotazioneChiusa;
    }

    /// <summary>Switches the door to the opposite state.</summary>
    public void Toggle()
    {
        if (IsAperta)
        {
            Chiudi();
        }
        else
        {
            Apri();
        }
    }

    void HandleManiglia(VRInteractableButton pulsante)
    {
        if (IsAperta)
        {
            Chiudi();
        }
        else
        {
            Apri();
        }
    }
}
