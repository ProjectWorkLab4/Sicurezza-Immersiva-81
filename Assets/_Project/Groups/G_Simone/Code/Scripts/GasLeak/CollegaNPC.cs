using System.Collections;
using SI81.Rig;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// The colleague NPC (GGD: "Collega NPC - Interazione: poi segue il giocatore").
///
/// Timeline:
/// 1. after a few seconds he starts complaining and swaying (symptoms);
/// 2. later he hallucinates ("vedi le formiche?");
/// 3. when the player interacts with him he starts following the player;
/// 4. if he is not brought outside in time he faints and can no longer follow.
///
/// He uses a NavMeshAgent when a baked NavMesh is available, otherwise he moves
/// directly towards the player (placeholder-friendly).
/// </summary>
[DisallowMultipleComponent]
public class CollegaNPC : MonoBehaviour
{
    [Header("Riferimenti")]
    [SerializeField] GestoreScenarioGasLeak gestore;
    [Tooltip("Testa del giocatore. Se vuoto viene usata quella del rig condiviso.")]
    [SerializeField] Transform giocatore;
    [Tooltip("Punto di interazione (VRInteractableButton sopra un Poke/Ray/Grab interactable).")]
    [SerializeField] VRInteractableButton interazione;
    [Tooltip("Modello da far barcollare / cadere. Se vuoto viene usato questo Transform.")]
    [SerializeField] Transform modello;
    [Tooltip("Testo del fumetto in world space. Opzionale.")]
    [SerializeField] TMP_Text fumetto;
    [SerializeField] Animator animator;

    [Header("Movimento")]
    [SerializeField] bool usaNavMesh = true;
    [SerializeField] float velocita = 1.15f;
    [Tooltip("Distanza alla quale si ferma dal giocatore.")]
    [SerializeField] float distanzaFermo = 1.5f;
    [Tooltip("Oltre questa distanza viene riposizionato dietro al giocatore.")]
    [SerializeField] float distanzaTeletrasporto = 9f;
    [SerializeField] float velocitaRotazione = 240f;

    [Header("Comportamento")]
    [Tooltip("Secondi di attesa prima di iniziare a seguire dopo l'interazione.")]
    [SerializeField] float ritardoAvvioSeguito = 0.8f;
    [Tooltip("Ampiezza dell'oscillamento quando ha i sintomi (gradi).")]
    [SerializeField] float ampiezzaBarcollamento = 7f;
    [Tooltip("Velocita' dell'oscillamento.")]
    [SerializeField] float velocitaBarcollamento = 1.4f;

    [Header("Fumetto")]
    [SerializeField] float durataFumetto = 5f;
    [SerializeField] float altezzaFumetto = 0.45f;
    [Tooltip("Se il testo appare specchiato, attiva questa opzione.")]
    [SerializeField] bool invertiFumetto = false;

    [Header("Testi")]
    [TextArea(1, 3)]
    [SerializeField] string testoIniziale = "Mi gira la testa... non mi sento bene.";
    [TextArea(1, 3)]
    [SerializeField] string testoAllucinazione = "Vedi anche tu le formiche? Sono ovunque...";
    [TextArea(1, 3)]
    [SerializeField] string testoSeguito = "Vengo con te. Aiutami a uscire.";
    [TextArea(1, 3)]
    [SerializeField] string testoSvenuto = "...";
    [TextArea(1, 3)]
    [SerializeField] string[] testiLamento =
    {
        "Che mal di testa...",
        "Mi manca l'aria.",
        "Ho la nausea, mi gira tutto."
    };

    [Header("Audio")]
    [SerializeField] AudioSource voce;
    [SerializeField] AudioClip[] clipLamenti = new AudioClip[0];
    [SerializeField] AudioClip clipAllucinazione;
    [SerializeField] AudioClip clipSvenimento;

    [Header("Eventi")]
    [SerializeField] UnityEvent onSintomi = new UnityEvent();
    [SerializeField] UnityEvent onSegue = new UnityEvent();
    [SerializeField] UnityEvent onSvenuto = new UnityEvent();

    // ---------------------------------------------------------------- state

    /// <summary>True once the player has interacted with him.</summary>
    public bool SegueGiocatore { get; private set; }

    /// <summary>True if he fainted: he can no longer follow the player.</summary>
    public bool Svenuto { get; private set; }

    /// <summary>True once the symptoms started.</summary>
    public bool SintomiIniziati { get; private set; }

    /// <summary>True once he hallucinated.</summary>
    public bool AllucinazioneMostrata { get; private set; }

    NavMeshAgent _agente;
    Transform _modello;
    Quaternion _rotazioneModello;
    Vector3 _posizioneModello;
    Coroutine _routineFumetto;
    float _timerSway;
    float _timerSeguito;
    bool _inAvvioSeguito;

    void Awake()
    {
        if (gestore == null)
        {
            gestore = FindFirstObjectByType<GestoreScenarioGasLeak>();
        }

        if (giocatore == null && RigRegistration.Instance != null)
        {
            giocatore = RigRegistration.Instance.Head;
        }

        if (giocatore == null && Camera.main != null)
        {
            giocatore = Camera.main.transform;
        }

        if (voce == null)
        {
            voce = GetComponent<AudioSource>();
        }

        _modello = modello != null ? modello : transform;
        _rotazioneModello = _modello.localRotation;
        _posizioneModello = _modello.localPosition;

        _agente = GetComponent<NavMeshAgent>();

        if (_agente != null)
        {
            _agente.speed = velocita;
            _agente.stoppingDistance = Mathf.Max(0.4f, distanzaFermo - 0.4f);
            _agente.updateRotation = false;
        }

        if (fumetto != null)
        {
            fumetto.text = string.Empty;
        }
    }

    void OnEnable()
    {
        if (interazione != null)
        {
            interazione.Selected += HandleInterazione;
        }
    }

    void OnDisable()
    {
        if (interazione != null)
        {
            interazione.Selected -= HandleInterazione;
        }
    }

    void LateUpdate()
    {
        AggiornaFumetto();

        if (Svenuto)
        {
            return;
        }

        if (SegueGiocatore)
        {
            if (_inAvvioSeguito)
            {
                _timerSeguito += Time.deltaTime;

                if (_timerSeguito >= ritardoAvvioSeguito)
                {
                    _inAvvioSeguito = false;
                }
            }

            Segui();
        }
        else if (SintomiIniziati)
        {
            Barcolla();
        }
    }

    // ---------------------------------------------------------------- public API

    /// <summary>Starts the symptoms (called by the scenario manager after a delay).</summary>
    public void IniziaSintomi()
    {
        if (SintomiIniziati || Svenuto)
        {
            return;
        }

        SintomiIniziati = true;
        MostraTesto(testoIniziale);
        Riproduci(clipLamenti != null && clipLamenti.Length > 0 ? clipLamenti[0] : null);

        if (animator != null)
        {
            animator.SetTrigger("Sintomi");
        }

        onSintomi.Invoke();
    }

    /// <summary>Shows a random complaint line.</summary>
    public void Lamento()
    {
        if (Svenuto || SegueGiocatore)
        {
            return;
        }

        if (testiLamento != null && testiLamento.Length > 0)
        {
            MostraTesto(testiLamento[Random.Range(0, testiLamento.Length)]);
        }

        if (clipLamenti != null && clipLamenti.Length > 0)
        {
            Riproduci(clipLamenti[Random.Range(0, clipLamenti.Length)]);
        }
    }

    /// <summary>Shows the hallucination line.</summary>
    public void MostraAllucinazione()
    {
        if (Svenuto)
        {
            return;
        }

        AllucinazioneMostrata = true;
        MostraTesto(testoAllucinazione);
        Riproduci(clipAllucinazione);
    }

    /// <summary>Makes him follow the player. Called by the interaction.</summary>
    public void Chiama()
    {
        if (SegueGiocatore || Svenuto)
        {
            return;
        }

        SegueGiocatore = true;
        _inAvvioSeguito = true;
        _timerSeguito = 0f;

        MostraTesto(testoSeguito);
        Riproduci(clipLamenti != null && clipLamenti.Length > 0 ? clipLamenti[0] : null);

        if (animator != null)
        {
            animator.SetTrigger("Segue");
        }

        gestore?.SegnalaCollegaAvvisato();
        onSegue.Invoke();
    }

    /// <summary>Makes him faint: he stops following and falls to the floor.</summary>
    public void Svenisci()
    {
        if (Svenuto)
        {
            return;
        }

        Svenuto = true;
        SegueGiocatore = false;
        _inAvvioSeguito = false;

        if (_agente != null && _agente.enabled && _agente.isOnNavMesh)
        {
            _agente.isStopped = true;
        }

        MostraTesto(testoSvenuto);
        Riproduci(clipSvenimento);

        if (animator != null)
        {
            animator.SetTrigger("Svenuto");
        }

        StartCoroutine(Cadi());
        onSvenuto.Invoke();
    }

    // ---------------------------------------------------------------- movement

    void Segui()
    {
        if (giocatore == null)
        {
            return;
        }

        Vector3 posizioneGiocatore = giocatore.position;
        Vector3 miaPosizione = transform.position;
        float distanza = Vector3.Distance(
            new Vector3(miaPosizione.x, 0f, miaPosizione.z),
            new Vector3(posizioneGiocatore.x, 0f, posizioneGiocatore.z));

        if (distanza > distanzaTeletrasporto)
        {
            Vector3 dietro = posizioneGiocatore - giocatore.forward * 1.4f;
            dietro.y = miaPosizione.y;

            if (NavMesh.SamplePosition(dietro, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                dietro = hit.position;
            }

            transform.position = dietro;

            if (_agente != null && _agente.enabled && _agente.isOnNavMesh)
            {
                _agente.Warp(dietro);
            }

            return;
        }

        if (_agente != null && _agente.enabled && _agente.isOnNavMesh && usaNavMesh)
        {
            _agente.isStopped = _inAvvioSeguito || distanza <= distanzaFermo;
            _agente.speed = velocita;
            _agente.SetDestination(posizioneGiocatore);
        }
        else if (!_inAvvioSeguito && distanza > distanzaFermo)
        {
            Vector3 destinazione = new Vector3(posizioneGiocatore.x, miaPosizione.y, posizioneGiocatore.z);
            transform.position = Vector3.MoveTowards(transform.position, destinazione, velocita * Time.deltaTime);
        }

        GuardaGiocatore(posizioneGiocatore);
    }

    void GuardaGiocatore(Vector3 posizioneGiocatore)
    {
        Vector3 direzione = posizioneGiocatore - transform.position;
        direzione.y = 0f;

        if (direzione.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion target = Quaternion.LookRotation(direzione.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, velocitaRotazione * Time.deltaTime);
    }

    void Barcolla()
    {
        _timerSway += Time.deltaTime * velocitaBarcollamento;

        float oscillazione = Mathf.Sin(_timerSway) * ampiezzaBarcollamento;
        float beccheggio = Mathf.Sin(_timerSway * 1.7f) * ampiezzaBarcollamento * 0.4f;

        _modello.localRotation = _rotazioneModello * Quaternion.Euler(beccheggio, 0f, oscillazione);
    }

    IEnumerator Cadi()
    {
        Quaternion partenza = _modello.localRotation;
        Quaternion arrivo = _rotazioneModello * Quaternion.Euler(0f, 0f, 88f);
        Vector3 posizioneArrivo = _posizioneModello + new Vector3(0f, -0.45f, 0f);

        float elapsed = 0f;
        const float durata = 1.1f;

        while (elapsed < durata)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / durata);

            _modello.localRotation = Quaternion.Slerp(partenza, arrivo, t);
            _modello.localPosition = Vector3.Lerp(_posizioneModello, posizioneArrivo, t);

            yield return null;
        }

        _modello.localRotation = arrivo;
        _modello.localPosition = posizioneArrivo;
    }

    // ---------------------------------------------------------------- feedback

    void HandleInterazione(VRInteractableButton pulsante)
    {
        Chiama();
    }

    void MostraTesto(string testo)
    {
        if (fumetto == null || string.IsNullOrEmpty(testo))
        {
            return;
        }

        if (_routineFumetto != null)
        {
            StopCoroutine(_routineFumetto);
        }

        fumetto.text = testo;
        fumetto.gameObject.SetActive(true);
        _routineFumetto = StartCoroutine(NascondiFumetto(durataFumetto));
    }

    IEnumerator NascondiFumetto(float durata)
    {
        yield return new WaitForSeconds(durata);

        if (fumetto != null)
        {
            fumetto.text = string.Empty;
        }

        _routineFumetto = null;
    }

    void AggiornaFumetto()
    {
        if (fumetto == null)
        {
            return;
        }

        Vector3 posizione = transform.position + Vector3.up * altezzaFumetto;
        fumetto.transform.position = posizione;

        Transform target = giocatore != null
            ? giocatore
            : (RigRegistration.Instance != null ? RigRegistration.Instance.Head : null);

        if (target == null)
        {
            return;
        }

        Vector3 direzione = invertiFumetto
            ? target.position - posizione
            : posizione - target.position;

        if (direzione.sqrMagnitude < 0.0001f)
        {
            return;
        }

        fumetto.transform.rotation = Quaternion.LookRotation(direzione.normalized, Vector3.up);
    }

    void Riproduci(AudioClip clip)
    {
        if (voce != null && clip != null)
        {
            voce.PlayOneShot(clip);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, distanzaFermo);
    }
}
