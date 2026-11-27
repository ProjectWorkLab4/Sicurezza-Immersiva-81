using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Openable window (GGD: "Finestre - Apertura e chiusura").
///
/// The player interacts with the handle (poke / ray through the Meta XR Interaction
/// SDK) and the sash rotates around its hinge. While at least one window is open the
/// CO in the room rises more slowly (handled by <see cref="GestoreScenarioGasLeak"/>).
/// </summary>
[DisallowMultipleComponent]
public class FinestraApribile : MonoBehaviour
{
    [Header("Stato")]
    [SerializeField] bool apertaAllAvvio = false;

    [Header("Riferimenti")]
    [SerializeField] GestoreScenarioGasLeak gestore;
    [Tooltip("Maniglia della finestra (VRInteractableButton sopra un Poke/Ray/Grab interactable).")]
    [SerializeField] VRInteractableButton maniglia;

    [Header("Anta")]
    [Tooltip("Transform dell'anta. Il suo pivot deve stare sulla cerniera.")]
    [SerializeField] Transform anta;
    [Tooltip("Angolo di apertura in gradi (positivo = antiorario attorno all'asse Y locale).")]
    [SerializeField] float angoloAperto = 80f;
    [Tooltip("Velocità di apertura in gradi al secondo.")]
    [SerializeField] float velocita = 120f;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip suonoApertura;
    [SerializeField] AudioClip suonoChiusura;

    [Header("Eventi")]
    [SerializeField] UnityEvent onAperta = new UnityEvent();
    [SerializeField] UnityEvent onChiusa = new UnityEvent();

    /// <summary>Fired with the new state (true = open).</summary>
    public event System.Action<bool> CambioStato;

    public bool Aperta { get; private set; }

    Quaternion _rotazioneChiusa;
    Quaternion _rotazioneAperta;
    Quaternion _target;

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
        Aperta = apertaAllAvvio;

        if (anta != null)
        {
            _target = Aperta ? _rotazioneAperta : _rotazioneChiusa;
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

    /// <summary>Opens the window.</summary>
    public void Apri()
    {
        ImpostaAperta(true);
    }

    /// <summary>Closes the window.</summary>
    public void Chiudi()
    {
        ImpostaAperta(false);
    }

    /// <summary>Switches the window to the opposite state.</summary>
    public void Toggle()
    {
        ImpostaAperta(!Aperta);
    }

    /// <summary>Sets the window state.</summary>
    public void ImpostaAperta(bool aperta)
    {
        if (aperta == Aperta)
        {
            return;
        }

        Aperta = aperta;
        _target = aperta ? _rotazioneAperta : _rotazioneChiusa;

        if (audioSource != null)
        {
            AudioClip clip = aperta ? suonoApertura : suonoChiusura;

            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        if (aperta)
        {
            onAperta.Invoke();
        }
        else
        {
            onChiusa.Invoke();
        }

        CambioStato?.Invoke(aperta);
        gestore?.SegnalaFinestra(aperta);
    }

    void HandleManiglia(VRInteractableButton pulsante)
    {
        Toggle();
    }
}
