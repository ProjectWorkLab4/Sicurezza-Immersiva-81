using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Faulty gas heater (GGD: "Stufetta a gas difettosa, con manopola di spegnimento").
///
/// The player turns the knob with a direct interaction (poke / ray on the Meta XR
/// Interaction SDK interactable) and the heater stops producing CO. The flame, the
/// light and the glowing grid are turned off to give clear visual feedback.
/// </summary>
[DisallowMultipleComponent]
public class StufettaGas : MonoBehaviour
{
    [Header("Stato")]
    [SerializeField] bool accesaAllAvvio = true;

    [Header("Riferimenti")]
    [SerializeField] GestoreScenarioGasLeak gestore;
    [Tooltip("Manopola di spegnimento (VRInteractableButton sopra un Poke/Ray/Grab interactable).")]
    [SerializeField] VRInteractableButton manopola;

    [Header("Animazione manopola")]
    [SerializeField] Transform levaManopola;
    [SerializeField] Vector3 rotazioneAccesa = new Vector3(-35f, 0f, 0f);
    [SerializeField] Vector3 rotazioneSpenta = new Vector3(35f, 0f, 0f);
    [SerializeField] float velocitaManopola = 10f;

    [Header("Fiamma")]
    [SerializeField] ParticleSystem fiamma;
    [SerializeField] Light luceFiamma;
    [SerializeField] float intensitaLuceAccesa = 2.5f;

    [Header("Griglia incandescente")]
    [SerializeField] Renderer[] griglia = new Renderer[0];
    [SerializeField] Material materialeAccesa;
    [SerializeField] Material materialeSpenta;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip suonoSpegnimento;
    [SerializeField] AudioClip suonoAccensione;

    [Header("Eventi")]
    [SerializeField] UnityEvent onSpenta = new UnityEvent();
    [SerializeField] UnityEvent onAccesa = new UnityEvent();

    /// <summary>Fired with the new state (true = burning).</summary>
    public event System.Action<bool> CambioStato;

    public bool Accesa { get; private set; }

    Quaternion _rotazioneTarget;
    bool _primoFrame = true;

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

        if (levaManopola != null)
        {
            _rotazioneTarget = levaManopola.localRotation;
        }
    }

    void OnEnable()
    {
        if (manopola != null)
        {
            manopola.Selected += HandleManopola;
        }
    }

    void OnDisable()
    {
        if (manopola != null)
        {
            manopola.Selected -= HandleManopola;
        }
    }

    void Start()
    {
        ApplicaStato(accesaAllAvvio, iniziale: true);
    }

    void LateUpdate()
    {
        if (levaManopola == null)
        {
            return;
        }

        float blend = 1f - Mathf.Exp(-velocitaManopola * Time.deltaTime);
        levaManopola.localRotation = Quaternion.Slerp(levaManopola.localRotation, _rotazioneTarget, blend);
    }

    /// <summary>Turns the heater on.</summary>
    public void Accendi()
    {
        ApplicaStato(true, iniziale: false);
    }

    /// <summary>Turns the heater off. This stops the CO rise.</summary>
    public void Spegni()
    {
        ApplicaStato(false, iniziale: false);
    }

    /// <summary>Switches the heater to the opposite state.</summary>
    public void Toggle()
    {
        ApplicaStato(!Accesa, iniziale: false);
    }

    void ApplicaStato(bool acceso, bool iniziale)
    {
        bool cambiato = _primoFrame || acceso != Accesa;
        _primoFrame = false;

        if (!cambiato)
        {
            return;
        }

        Accesa = acceso;

        if (fiamma != null)
        {
            if (acceso)
            {
                fiamma.Play(true);
            }
            else
            {
                fiamma.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        if (luceFiamma != null)
        {
            luceFiamma.enabled = acceso;
            luceFiamma.intensity = acceso ? intensitaLuceAccesa : 0f;
        }

        if (griglia != null && materialeAccesa != null && materialeSpenta != null)
        {
            Material materiale = acceso ? materialeAccesa : materialeSpenta;

            foreach (Renderer parte in griglia)
            {
                if (parte != null)
                {
                    parte.sharedMaterial = materiale;
                }
            }
        }

        if (levaManopola != null)
        {
            _rotazioneTarget = Quaternion.Euler(acceso ? rotazioneAccesa : rotazioneSpenta);
        }

        if (!iniziale)
        {
            if (audioSource != null)
            {
                AudioClip clip = acceso ? suonoAccensione : suonoSpegnimento;

                if (clip != null)
                {
                    audioSource.PlayOneShot(clip);
                }
            }

            if (acceso)
            {
                onAccesa.Invoke();
            }
            else
            {
                onSpenta.Invoke();
                gestore?.SegnalaStufettaSpenta();
            }

            CambioStato?.Invoke(acceso);
        }
    }

    void HandleManopola(VRInteractableButton pulsante)
    {
        Toggle();
    }
}
