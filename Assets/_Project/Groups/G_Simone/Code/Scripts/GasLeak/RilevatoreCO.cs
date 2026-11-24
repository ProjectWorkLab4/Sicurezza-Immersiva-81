using TMPro;
using UnityEngine;

/// <summary>
/// Portable CO detector (GGD: "Rilevatore di CO portatile, con display").
///
/// The player grabs it with the Interaction SDK grab interactable, the display shows
/// the current CO concentration and the buzzer speeds up as the gas rises.
///
/// Grab / drop is handled by the Meta XR Interaction SDK: this script only listens to
/// the <see cref="VRInteractableButton"/> that wraps the GrabInteractable.
/// </summary>
[DisallowMultipleComponent]
public class RilevatoreCO : MonoBehaviour
{
    [Header("Riferimenti")]
    [SerializeField] GestoreScenarioGasLeak gestore;
    [Tooltip("Testo del display del rilevatore (TMP).")]
    [SerializeField] TMP_Text display;
    [Tooltip("Renderer del display, per il colore di allarme. Opzionale.")]
    [SerializeField] Renderer schermo;
    [Tooltip("Impugnatura: Selected quando il giocatore lo raccoglie.")]
    [SerializeField] VRInteractableButton impugnatura;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip bip;

    [Header("Spia")]
    [SerializeField] Light spia;

    [Header("Soglie (ppm)")]
    [SerializeField] float sogliaAttenzione = 50f;
    [SerializeField] float sogliaAllarme = 150f;
    [Tooltip("Valore di CO al quale il bip raggiunge la frequenza massima.")]
    [SerializeField] float coPerBipMassimo = 400f;

    [Header("Bip")]
    [Tooltip("Secondi tra due bip con aria pulita.")]
    [SerializeField] float intervalloBipLento = 2.4f;
    [Tooltip("Secondi tra due bip con CO molto alto.")]
    [SerializeField] float intervalloBipVeloce = 0.16f;
    [Tooltip("Se attivo il bip suona solo quando il rilevatore e' in mano.")]
    [SerializeField] bool soloSeImpugnato = true;

    [Header("Colori")]
    [SerializeField] Color coloreSicuro = new Color(0.24f, 0.86f, 0.38f);
    [SerializeField] Color coloreAttenzione = new Color(1f, 0.76f, 0.12f);
    [SerializeField] Color coloreAllarme = new Color(1f, 0.18f, 0.12f);

    /// <summary>True while the player is holding the detector.</summary>
    public bool Impugnato { get; private set; }

    /// <summary>Last measured value, in ppm.</summary>
    public float ValoreCorrente { get; private set; }

    float _timerBip;
    bool _primoUsoSegnalato;
    MaterialPropertyBlock _blocco;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

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
    }

    void OnEnable()
    {
        if (impugnatura != null)
        {
            impugnatura.Selected += HandleSelected;
            impugnatura.Deselected += HandleDeselected;
        }
    }

    void OnDisable()
    {
        if (impugnatura != null)
        {
            impugnatura.Selected -= HandleSelected;
            impugnatura.Deselected -= HandleDeselected;
        }
    }

    void Update()
    {
        ValoreCorrente = gestore != null ? gestore.CO : 0f;

        AggiornaDisplay();
        AggiornaBip();
    }

    void AggiornaDisplay()
    {
        Color colore = ColorePerValore(ValoreCorrente);

        if (display != null)
        {
            string stato = ValoreCorrente < sogliaAttenzione
                ? "ARIA PULITA"
                : ValoreCorrente < sogliaAllarme ? "ATTENZIONE" : "CO ALTO";

            display.text = $"CO\n{Mathf.RoundToInt(ValoreCorrente)} ppm\n{stato}";
            display.color = colore;
        }

        if (spia != null)
        {
            spia.color = colore;
        }

        ApplicaColoreSchermo(colore);
    }

    void AggiornaBip()
    {
        bool attivo = !soloSeImpugnato || Impugnato;

        if (!attivo || bip == null || audioSource == null)
        {
            _timerBip = 0f;
            return;
        }

        _timerBip += Time.deltaTime;

        if (_timerBip >= IntervalloBip())
        {
            _timerBip = 0f;
            audioSource.PlayOneShot(bip);
        }
    }

    float IntervalloBip()
    {
        float t = Mathf.InverseLerp(sogliaAttenzione, Mathf.Max(sogliaAttenzione + 1f, coPerBipMassimo), ValoreCorrente);
        return Mathf.Lerp(intervalloBipLento, intervalloBipVeloce, t);
    }

    Color ColorePerValore(float valore)
    {
        if (valore >= sogliaAllarme)
        {
            return coloreAllarme;
        }

        if (valore >= sogliaAttenzione)
        {
            return Color.Lerp(coloreAttenzione, coloreAllarme,
                Mathf.InverseLerp(sogliaAttenzione, Mathf.Max(sogliaAttenzione + 1f, sogliaAllarme), valore));
        }

        return coloreSicuro;
    }

    void ApplicaColoreSchermo(Color colore)
    {
        if (schermo == null)
        {
            return;
        }

        if (_blocco == null)
        {
            _blocco = new MaterialPropertyBlock();
        }

        schermo.GetPropertyBlock(_blocco);
        _blocco.SetColor(BaseColorId, colore);
        _blocco.SetColor(ColorId, colore);
        _blocco.SetColor(EmissionColorId, colore * 1.5f);
        schermo.SetPropertyBlock(_blocco);
    }

    void HandleSelected(VRInteractableButton pulsante)
    {
        Impugnato = true;

        if (!_primoUsoSegnalato)
        {
            _primoUsoSegnalato = true;
            gestore?.SegnalaRilevatoreUsato();
        }
    }

    void HandleDeselected(VRInteractableButton pulsante)
    {
        Impugnato = false;
    }
}
