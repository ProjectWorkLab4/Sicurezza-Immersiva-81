using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Electrical panel (quadro elettrico) that can be switched on and off.
///
/// Poking / grabbing one of the switches (or calling <see cref="Toggle"/>)
/// powers or cuts the circuits connected to the panel: lights, powered objects,
/// status lights and a moving lever, plus a message on the VR screen.
/// </summary>
[DisallowMultipleComponent]
public class QuadroElettricoInterattivo : MonoBehaviour
{
    [Header("Stato iniziale")]
    [SerializeField] bool accesoAllAvvio = false;

    [Header("Interazione")]
    [Tooltip("Interruttori / leva che comandano il quadro.")]
    [SerializeField] VRInteractableButton[] interruttori = new VRInteractableButton[0];

    [Header("Cosa alimenta")]
    [SerializeField] Light[] luci = new Light[0];
    [SerializeField] GameObject[] oggettiAlimentati = new GameObject[0];
    [SerializeField] bool attivaDisattivaOggetti = true;

    [Header("Spie di stato")]
    [SerializeField] Renderer[] spie = new Renderer[0];
    [SerializeField] Material materialeSpiaAccesa;
    [SerializeField] Material materialeSpiaSpenta;

    [Header("Leva")]
    [SerializeField] Transform leva;
    [SerializeField] Vector3 rotazioneLevaAcceso = new Vector3(-25f, 0f, 0f);
    [SerializeField] Vector3 rotazioneLevaSpento = new Vector3(25f, 0f, 0f);
    [SerializeField] float velocitaLeva = 8f;

    [Header("Testi e feedback")]
    [SerializeField] TMP_Text etichettaStato;
    [SerializeField] string testoAcceso = "IMPIANTO: ALIMENTATO";
    [SerializeField] string testoSpento = "IMPIANTO: DISALIMENTATO";

    [Header("Schermo VR")]
    [SerializeField] bool mostraSuSchermoVR = true;
    [SerializeField] VRMessageDisplay display;
    [SerializeField] string titoloMessaggi = "QUADRO ELETTRICO";
    [TextArea(2, 4)]
    [SerializeField] string messaggioAcceso = "Quadro elettrico acceso: l’impianto è alimentato.";
    [TextArea(2, 4)]
    [SerializeField] string messaggioSpento = "Quadro elettrico spento: l’impianto è disalimentato.";
    [SerializeField] float durataMessaggio = 4f;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip suonoAccensione;
    [SerializeField] AudioClip suonoSpegnimento;

    [Header("Eventi")]
    [SerializeField] UnityEvent onAcceso = new UnityEvent();
    [SerializeField] UnityEvent onSpento = new UnityEvent();

    /// <summary>Fired with the new state (true = powered).</summary>
    public event Action<bool> CambioStato;

    public bool Acceso { get; private set; }

    Quaternion _rotazioneTarget;
    bool _inizializzato;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (leva != null)
        {
            _rotazioneTarget = leva.localRotation;
        }
    }

    void OnEnable()
    {
        if (interruttori == null)
        {
            return;
        }

        foreach (VRInteractableButton interruttore in interruttori)
        {
            if (interruttore != null)
            {
                interruttore.Selected += HandleInterruttore;
            }
        }
    }

    void OnDisable()
    {
        if (interruttori == null)
        {
            return;
        }

        foreach (VRInteractableButton interruttore in interruttori)
        {
            if (interruttore != null)
            {
                interruttore.Selected -= HandleInterruttore;
            }
        }
    }

    void Start()
    {
        ApplicaStato(accesoAllAvvio, iniziale: true);
    }

    /// <summary>Switches the panel to the opposite state.</summary>
    public void Toggle()
    {
        ApplicaStato(!Acceso, iniziale: false);
    }

    /// <summary>Powers (true) or cuts (false) the panel.</summary>
    public void ImpostaStato(bool acceso)
    {
        ApplicaStato(acceso, iniziale: false);
    }

    /// <summary>Same as <see cref="ImpostaStato"/>, kept for consistency with Unity UI events.</summary>
    public void SetPower(bool acceso)
    {
        ImpostaStato(acceso);
    }

    void LateUpdate()
    {
        if (leva == null)
        {
            return;
        }

        float blend = 1f - Mathf.Exp(-velocitaLeva * Time.deltaTime);
        leva.localRotation = Quaternion.Slerp(leva.localRotation, _rotazioneTarget, blend);
    }

    void ApplicaStato(bool acceso, bool iniziale)
    {
        bool changed = !_inizializzato || acceso != Acceso;
        _inizializzato = true;

        if (!changed)
        {
            return;
        }

        Acceso = acceso;

        if (luci != null)
        {
            foreach (Light luce in luci)
            {
                if (luce != null)
                {
                    luce.enabled = acceso;
                }
            }
        }

        if (oggettiAlimentati != null && attivaDisattivaOggetti)
        {
            foreach (GameObject oggetto in oggettiAlimentati)
            {
                if (oggetto != null && oggetto.activeSelf != acceso)
                {
                    oggetto.SetActive(acceso);
                }
            }
        }

        if (spie != null && materialeSpiaAccesa != null && materialeSpiaSpenta != null)
        {
            Material materiale = acceso ? materialeSpiaAccesa : materialeSpiaSpenta;

            foreach (Renderer spia in spie)
            {
                if (spia != null)
                {
                    spia.sharedMaterial = materiale;
                }
            }
        }

        if (etichettaStato != null)
        {
            etichettaStato.text = acceso ? testoAcceso : testoSpento;
        }

        if (leva != null)
        {
            _rotazioneTarget = Quaternion.Euler(acceso ? rotazioneLevaAcceso : rotazioneLevaSpento);
        }

        if (!iniziale)
        {
            PlayClip(acceso ? suonoAccensione : suonoSpegnimento);
            ShowOnVRScreen(acceso ? messaggioAcceso : messaggioSpento);

            if (acceso)
            {
                onAcceso.Invoke();
            }
            else
            {
                onSpento.Invoke();
            }

            CambioStato?.Invoke(acceso);
        }
    }

    void HandleInterruttore(VRInteractableButton interruttore)
    {
        Toggle();
    }

    void ShowOnVRScreen(string body)
    {
        if (!mostraSuSchermoVR || string.IsNullOrEmpty(body))
        {
            return;
        }

        VRMessageDisplay target = display != null ? display : VRMessageDisplay.GetOrCreate();
        target.Show(titoloMessaggi, body, durataMessaggio);
    }

    void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
