using System.Collections;
using System.Collections.Generic;
using SI81.Input;
using SI81.Interaction;
using SI81.Rig;
using SI81.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Punto d'ingresso di Blackout per la UI comune.
/// Sta su MNG_Scenario al posto del ScenarioController di base. Collega gli eventi del gameplay
/// esistente ai soli canali ammessi: fase, suggerimenti, sottotitoli, avvisi e fine missione.
/// Non crea Canvas né pannelli locali (guida §3 caso B, §5, §7).
/// </summary>
[DisallowMultipleComponent]
public sealed class BlackoutScenario : ScenarioController
{
    const string ICONA_AVVISO = "ICN_Warning_Filled";
    const string PORTA_SEGNALETICA = "porta_tagliafuoco_D3";
    const string TITOLO_RILEVATORE = "RILEVATORE CO";
    const string MESSAGGIO_RILEVATORE = "I livelli di CO sono troppo alti";
    const float DURATA_MESSAGGIO_RILEVATORE = 10f;
    const float VIGNETTA_INTENSITA = 0.85f;
    const float VIGNETTA_DURATA = 1f;
    const float TINTA_ROSSA_ALPHA = 0.3f;
    const float TOSSA_INTERVALLO = 20f;
    const float TOSSA_SOGLIA_MOVIMENTO = 0.25f;
    const float TOSSA_VOLUME = 0.7f;
    static readonly Color COLORE_VIGNETTA = new Color(0.5f, 0.01f, 0.01f, 1f);

    [Tooltip("Cervello del blackout presente su MNG_Scenario.")]
    [SerializeField] private GameManager gameManager;

    [Tooltip("SFX della tosse: riprodotto una volta ogni 40 s di immobilità nello Stress, senza danno.")]
    [SerializeField] private AudioClip suonoTosse;

    // Da riattivare quando il telefono sarà interattivo:
    // [Tooltip("Telefono interattivo presente nella scena.")]
    // [SerializeField] private TelefonoInterattivo telefono;

    [Header("Porte del successo")]
    [Tooltip("Trascina qui gli oggetti porta: selezionarne anche una sola basta per finire con successo. " +
             "Se non hanno già un Interactable, lo script ne aggiunge uno con FollowsHand disattivato.")]
    [SerializeField] private GameObject[] porteSuccesso = new GameObject[0];

    private readonly HashSet<string> porteSelezionate = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Interactable, System.Action<Hand>> handlerPorte = new Dictionary<Interactable, System.Action<Hand>>();
    private readonly List<string> erroriGDD = new List<string>();
    private int interazioniErrate;
    private bool blackoutAttivo;
    private readonly Dictionary<Interactable, System.Action<Hand>> handlerErrori = new Dictionary<Interactable, System.Action<Hand>>();
    private readonly Dictionary<Interactable, System.Action<Hand>> handlerMessaggioRilevatore = new Dictionary<Interactable, System.Action<Hand>>();
    private Coroutine fineMessaggioRilevatore;
    private bool messaggioRilevatoreMostrato;
    private Volume volumeVignetta;
    private Vignette vignetta;
    private Coroutine routineVignetta;
    private Canvas overlayTintaRossa;
    private CanvasGroup gruppoTintaRossa;
    private readonly List<UniversalAdditionalCameraData> cameraDataAggiunti = new List<UniversalAdditionalCameraData>();
    private readonly Dictionary<UniversalAdditionalCameraData, bool> postProcessingPrecedenti = new Dictionary<UniversalAdditionalCameraData, bool>();
    private System.Action<bool> handlerQuadro;
    private QuadroElettricoInterattivo quadroElettrico;
    private readonly Dictionary<Interactable, System.Action<Hand>> handlerObiettivo = new Dictionary<Interactable, System.Action<Hand>>();
    private bool obiettivoSegnaleticaCompletato;
    private AudioSource sorgenteTosse;
    private Vector3 ultimaTesta;
    private Vector3 ultimaManoSinistra;
    private Vector3 ultimaManoDestra;
    private bool posizioneRigRilevata;
    private float tempoFermo;

    /// <summary>Recupera il gameplay se i campi non sono stati assegnati.</summary>
    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        sorgenteTosse = gameObject.AddComponent<AudioSource>();
        sorgenteTosse.playOnAwake = false;
        sorgenteTosse.loop = false;
        sorgenteTosse.spatialBlend = 0f;
        sorgenteTosse.volume = TOSSA_VOLUME;

        PreparaPorteSuccesso();
        PreparaAzioniErrate();
        PreparaObiettivoSegnaletica();
    }

    /// <summary>
    /// La selezione di porta_tagliafuoco_D3 completa "Segui la segnaletica luminosa" e passa
    /// all'obiettivo "Evacua l'edificio". Si riabbona a runtime senza modificare la scena.
    /// </summary>
    private void PreparaObiettivoSegnaletica()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform porta = null;

        foreach (Transform candidato in transforms)
        {
            if (GameManager.DoorNameMatches(candidato.name, PORTA_SEGNALETICA))
            {
                porta = candidato;
                break;
            }
        }

        if (porta == null)
        {
            Debug.LogWarning($"[Blackout] Porta '{PORTA_SEGNALETICA}' non trovata: l'obiettivo segnaletica non avanzerà.", this);
            return;
        }

        Interactable[] presenti = porta.GetComponentsInChildren<Interactable>(true);
        if (presenti.Length == 0)
        {
            Interactable creato = porta.gameObject.AddComponent<Interactable>();
            creato.FollowsHand = false;
            presenti = new[] { creato };
        }

        foreach (Interactable interactable in presenti)
        {
            if (interactable == null || handlerObiettivo.ContainsKey(interactable))
            {
                continue;
            }

            System.Action<Hand> handler = hand => HandlePortaSegnaleticaSelezionata();
            interactable.SelectEntered += handler;
            handlerObiettivo[interactable] = handler;
        }
    }

    /// <summary>Avanza di un solo passo, la prima volta che viene selezionata la porta D3.</summary>
    private void HandlePortaSegnaleticaSelezionata()
    {
        if (!IsRunning || obiettivoSegnaleticaCompletato) return;

        obiettivoSegnaleticaCompletato = true;
        CompleteObjective();
        Debug.Log($"[Blackout] Obiettivo segnaletica completato selezionando '{PORTA_SEGNALETICA}'.");
    }

    /// <summary>
    /// Stress: ogni 40 secondi senza movimento del rig, tosse e vignettatura rossa, senza danni.
    /// Il timer si azzera quando il rig si muove dentro la scena.
    /// </summary>
    private void Update()
    {
        if (!IsRunning || Level != DifficultyLevel.Crisis || suonoTosse == null)
        {
            return;
        }

        RigRegistration rig = RigRegistration.Instance;
        if (rig == null || rig.Head == null)
        {
            return;
        }

        Vector3 testa = rig.Head.position;
        Vector3 sinistra = rig.LeftHand != null ? rig.LeftHand.position : Vector3.zero;
        Vector3 destra = rig.RightHand != null ? rig.RightHand.position : Vector3.zero;

        if (!posizioneRigRilevata)
        {
            ultimaTesta = testa;
            ultimaManoSinistra = sinistra;
            ultimaManoDestra = destra;
            posizioneRigRilevata = true;
            tempoFermo = 0f;
            return;
        }

        float sogliaAlQuadrato = TOSSA_SOGLIA_MOVIMENTO * TOSSA_SOGLIA_MOVIMENTO;
        if ((testa - ultimaTesta).sqrMagnitude > sogliaAlQuadrato ||
            (sinistra - ultimaManoSinistra).sqrMagnitude > sogliaAlQuadrato ||
            (destra - ultimaManoDestra).sqrMagnitude > sogliaAlQuadrato)
        {
            ultimaTesta = testa;
            ultimaManoSinistra = sinistra;
            ultimaManoDestra = destra;
            tempoFermo = 0f;
            return;
        }

        tempoFermo += Time.deltaTime;
        if (tempoFermo < TOSSA_INTERVALLO)
        {
            return;
        }

        tempoFermo = 0f;
        sorgenteTosse.PlayOneShot(suonoTosse);
        MostraVignettaErrore();
        Debug.Log("[Blackout] Tosse per immobilità prolungata (Stress): solo feedback, nessun danno.");
    }

    /// <summary>Rimuove le iscrizioni alle porte, poi lascia pulire la classe base.</summary>
    protected override void OnDestroy()
    {
        foreach (KeyValuePair<Interactable, System.Action<Hand>> coppia in handlerPorte)
        {
            if (coppia.Key != null)
            {
                coppia.Key.SelectEntered -= coppia.Value;
            }
        }

        handlerPorte.Clear();

        foreach (KeyValuePair<Interactable, System.Action<Hand>> coppia in handlerErrori)
        {
            if (coppia.Key != null)
            {
                coppia.Key.SelectEntered -= coppia.Value;
            }
        }

        handlerErrori.Clear();

        if (fineMessaggioRilevatore != null)
        {
            StopCoroutine(fineMessaggioRilevatore);
            fineMessaggioRilevatore = null;
        }

        foreach (KeyValuePair<Interactable, System.Action<Hand>> coppia in handlerMessaggioRilevatore)
        {
            if (coppia.Key != null)
            {
                coppia.Key.SelectEntered -= coppia.Value;
            }
        }

        handlerMessaggioRilevatore.Clear();

        foreach (KeyValuePair<Interactable, System.Action<Hand>> coppia in handlerObiettivo)
        {
            if (coppia.Key != null)
            {
                coppia.Key.SelectEntered -= coppia.Value;
            }
        }

        handlerObiettivo.Clear();

        if (routineVignetta != null)
        {
            StopCoroutine(routineVignetta);
            routineVignetta = null;
        }

        if (volumeVignetta != null)
        {
            Destroy(volumeVignetta.gameObject);
            volumeVignetta = null;
            vignetta = null;
        }

        if (overlayTintaRossa != null)
        {
            Destroy(overlayTintaRossa.gameObject);
            overlayTintaRossa = null;
            gruppoTintaRossa = null;
        }

        foreach (KeyValuePair<UniversalAdditionalCameraData, bool> coppia in postProcessingPrecedenti)
        {
            if (coppia.Key != null)
            {
                coppia.Key.renderPostProcessing = coppia.Value;
            }
        }

        postProcessingPrecedenti.Clear();

        foreach (UniversalAdditionalCameraData data in cameraDataAggiunti)
        {
            if (data != null)
            {
                Destroy(data);
            }
        }

        cameraDataAggiunti.Clear();

        if (sorgenteTosse != null)
        {
            Destroy(sorgenteTosse);
            sorgenteTosse = null;
        }

        if (quadroElettrico != null && handlerQuadro != null)
        {
            quadroElettrico.CambioStato -= handlerQuadro;
        }

        base.OnDestroy();
    }

    /// <summary>
    /// Iscrive le porte del successo assegnate dall'Inspector: non cerca più per nome, così funzionano
    /// anche quando la porta ha un nome diverso da quello previsto. Se manca un Interactable lo crea.
    /// </summary>
    private void PreparaPorteSuccesso()
    {
        if (porteSuccesso == null || porteSuccesso.Length == 0)
        {
            Debug.LogWarning("[Blackout] Nessuna porta del successo assegnata: il successo non può essere attivato.", this);
            return;
        }

        foreach (GameObject porta in porteSuccesso)
        {
            if (porta == null)
            {
                continue;
            }

            string nomePorta = porta.name;
            Interactable[] presenti = porta.GetComponentsInChildren<Interactable>(true);

            if (presenti.Length == 0)
            {
                Interactable creato = porta.AddComponent<Interactable>();
                creato.FollowsHand = false; // la porta non segue la mano: serve solo il segnale di selezione
                presenti = new[] { creato };
            }

            foreach (Interactable interactable in presenti)
            {
                if (interactable == null || handlerPorte.ContainsKey(interactable))
                {
                    continue;
                }

                System.Action<Hand> handler = hand => HandlePortaSelezionata(nomePorta);
                interactable.SelectEntered += handler;
                handlerPorte[interactable] = handler;
            }

            Debug.Log($"[Blackout] Porta '{nomePorta}' pronta per il successo (Interactable: {presenti.Length}).", this);
        }
    }

    private static Transform TrovaPerNome(Transform[] transforms, string nome)
    {
        foreach (Transform candidate in transforms)
        {
            if (candidate != null && string.Equals(candidate.name, nome, System.StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Iscrive le azioni non corrette del GDD rilevabili in scena: uso del rilevatore di gas
    /// e riaccensione del quadro. "Non seguire la segnaletica" non ha un trigger: va segnalato
    /// con <see cref="SegnalaAzioneErrata"/> da un futuro trigger area.
    /// </summary>
    private void PreparaAzioniErrate()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform rilevatore = TrovaPerNome(transforms, "RilevatoreDiGas");

        if (rilevatore != null)
        {
            Interactable[] presenti = rilevatore.GetComponentsInChildren<Interactable>(true);

            if (presenti.Length == 0)
            {
                Interactable creato = rilevatore.gameObject.AddComponent<Interactable>();
                creato.FollowsHand = false;
                presenti = new[] { creato };
            }

            foreach (Interactable interactable in presenti)
            {
                if (interactable == null || handlerErrori.ContainsKey(interactable))
                {
                    continue;
                }

                System.Action<Hand> handler = hand => RegistraErroreGDD("Hai usato il rilevatore di gas con l'allarme attivo");
                interactable.SelectEntered += handler;
                handlerErrori[interactable] = handler;

                if (!handlerMessaggioRilevatore.ContainsKey(interactable))
                {
                    System.Action<Hand> handlerMessaggio = hand => HandleRilevatoreSelezionato();
                    interactable.SelectEntered += handlerMessaggio;
                    handlerMessaggioRilevatore[interactable] = handlerMessaggio;
                }
            }
        }

        quadroElettrico = FindFirstObjectByType<QuadroElettricoInterattivo>();

        if (quadroElettrico != null && handlerQuadro == null)
        {
            handlerQuadro = acceso =>
            {
                if (acceso)
                {
                    RegistraErroreGDD("Hai riaccesso il quadro elettrico: scintilla e danno");
                }
            };

            quadroElettrico.CambioStato += handlerQuadro;
        }
    }

    /// <summary>
    /// Prima selezione del rilevatore: mostra il messaggio sul sottotitolo comune, poi disattiva
    /// l'oggetto dopo 10 secondi. Le selezioni successive durante il messaggio vengono ignorate.
    /// </summary>
    private void HandleRilevatoreSelezionato()
    {
        if (messaggioRilevatoreMostrato)
        {
            return;
        }

        messaggioRilevatoreMostrato = true;
        Subtitle(TITOLO_RILEVATORE, MESSAGGIO_RILEVATORE, DURATA_MESSAGGIO_RILEVATORE);

        if (fineMessaggioRilevatore != null)
        {
            StopCoroutine(fineMessaggioRilevatore);
        }

        fineMessaggioRilevatore = StartCoroutine(DisattivaRilevatoreDopo(DURATA_MESSAGGIO_RILEVATORE));
    }

    /// <summary>Chiude l'interazione con il rilevatore allo scadere dei 10 secondi.</summary>
    private IEnumerator DisattivaRilevatoreDopo(float secondi)
    {
        yield return new WaitForSeconds(secondi);

        foreach (Interactable interactable in handlerMessaggioRilevatore.Keys)
        {
            if (interactable == null)
            {
                continue;
            }

            interactable.Selectable = false;
            interactable.Grabbable = false;
        }

        fineMessaggioRilevatore = null;
    }

    /// <summary>Ripristina il rilevatore quando ricomincia la missione.</summary>
    private void RiattivaRilevatore()
    {
        if (fineMessaggioRilevatore != null)
        {
            StopCoroutine(fineMessaggioRilevatore);
            fineMessaggioRilevatore = null;
        }

        messaggioRilevatoreMostrato = false;

        foreach (Interactable interactable in handlerMessaggioRilevatore.Keys)
        {
            if (interactable == null)
            {
                continue;
            }

            interactable.Selectable = true;
            interactable.Grabbable = true;
        }
    }

    /// <summary>Registra un'azione non corretta del GDD, per esempio da un trigger area.</summary>
    public void SegnalaAzioneErrata(string descrizione)
    {
        RegistraErroreGDD(descrizione);
    }

    private void RegistraErroreGDD(string descrizione)
    {
        // Prima del blackout non esiste una crisi da gestire: nessun errore, punteggio o feedback.
        if (!blackoutAttivo)
        {
            return;
        }

        // Feedback visivo a ogni interazione sbagliata, anche se l'errore era già stato registrato
        // o la missione non è ancora partita.
        MostraVignettaErrore();
        Debug.Log($"[Blackout] Interazione errata: {descrizione} (running={IsRunning})");

        if (!IsRunning || string.IsNullOrEmpty(descrizione))
        {
            return;
        }

        interazioniErrate++;

        if (!erroriGDD.Contains(descrizione))
        {
            erroriGDD.Add(descrizione);
            Debug.Log($"[Blackout] Azione non corretta registrata: {descrizione}");
        }

        // Gli errori sono registrati a tutti i livelli; cambia solo la soglia di fallimento:
        // Realistico tre errori, Stress anche uno solo, Guidato nessuna soglia.
        // Contano le interazioni errate, anche ripetute.
        bool fallisce =
            (Level == DifficultyLevel.Operational && interazioniErrate >= 3) ||
            (Level == DifficultyLevel.Crisis && interazioniErrate >= 1);

        if (fallisce)
        {
            Debug.Log($"[Blackout] {interazioniErrate} interazioni errate: missione fallita ({Level}).");
            ChiudiMissione();
        }
    }

    private void MostraVignettaErrore()
    {
        if (volumeVignetta == null)
        {
            CreaVignetta();
        }

        if (vignetta == null && gruppoTintaRossa == null)
        {
            return;
        }

        if (routineVignetta != null)
        {
            StopCoroutine(routineVignetta);
        }

        routineVignetta = StartCoroutine(LampoVignetta());
    }

    private void CreaVignetta()
    {
        GameObject host = new GameObject("Volume_VignettaBlackout");
        host.transform.SetParent(transform, false);

        volumeVignetta = host.AddComponent<Volume>();
        volumeVignetta.isGlobal = true;
        volumeVignetta.priority = 200f;
        volumeVignetta.weight = 1f;
        volumeVignetta.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volumeVignetta.profile.name = "Volume_VignettaBlackout_Profile";

        vignetta = volumeVignetta.profile.Add<Vignette>(true);
        vignetta.color.Override(COLORE_VIGNETTA);
        vignetta.center.Override(new Vector2(0.5f, 0.5f));
        vignetta.smoothness.Override(0.45f);
        vignetta.intensity.Override(0f);

        // Il rig condiviso non ha il post-processing attivo: lo abilita a runtime e lo ripristina in OnDestroy.
        foreach (Camera camera in Camera.allCameras)
        {
            UniversalAdditionalCameraData data = camera.GetComponent<UniversalAdditionalCameraData>();

            if (data == null)
            {
                data = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                cameraDataAggiunti.Add(data);
            }
            else if (!postProcessingPrecedenti.ContainsKey(data))
            {
                postProcessingPrecedenti[data] = data.renderPostProcessing;
            }

            data.renderPostProcessing = true;
        }

        // Tinta rossa a schermo intero: non dipende dal post-processing della URP.
        GameObject overlayHost = new GameObject("Overlay_TintaRossaBlackout");
        overlayHost.transform.SetParent(transform, false);

        overlayTintaRossa = overlayHost.AddComponent<Canvas>();
        overlayTintaRossa.renderMode = RenderMode.ScreenSpaceCamera;
        overlayTintaRossa.worldCamera = Camera.main;
        overlayTintaRossa.planeDistance = 0.3f;
        overlayTintaRossa.sortingOrder = 1000;

        gruppoTintaRossa = overlayHost.AddComponent<CanvasGroup>();
        gruppoTintaRossa.alpha = 0f;
        gruppoTintaRossa.blocksRaycasts = false;
        gruppoTintaRossa.interactable = false;

        GameObject immagineHost = new GameObject("TintaRossa");
        immagineHost.transform.SetParent(overlayHost.transform, false);

        Image immagine = immagineHost.AddComponent<Image>();
        immagine.color = new Color(0.8f, 0f, 0f, 1f);
        immagine.raycastTarget = false;

        RectTransform rect = immagine.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private IEnumerator LampoVignetta()
    {
        float elapsed = 0f;

        while (elapsed < VIGNETTA_DURATA)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(elapsed / VIGNETTA_DURATA);

            if (vignetta != null)
            {
                vignetta.intensity.Override(VIGNETTA_INTENSITA * k);
            }

            if (gruppoTintaRossa != null)
            {
                if (overlayTintaRossa != null && overlayTintaRossa.worldCamera == null)
                {
                    overlayTintaRossa.worldCamera = Camera.main;
                }

                gruppoTintaRossa.alpha = TINTA_ROSSA_ALPHA * k;
            }

            yield return null;
        }

        if (vignetta != null)
        {
            vignetta.intensity.Override(0f);
        }

        if (gruppoTintaRossa != null)
        {
            gruppoTintaRossa.alpha = 0f;
        }

        routineVignetta = null;
    }

    /// <summary>Una sola porta selezionata basta per chiudere con successo (le due porte non possono essere selezionate insieme).</summary>
    private void HandlePortaSelezionata(string nomePorta)
    {
        if (!IsRunning || porteSelezionate.Contains(nomePorta))
        {
            return;
        }

        porteSelezionate.Add(nomePorta);
        Debug.Log($"[Blackout] Porta '{nomePorta}' selezionata: esito di successo.");

        // Il procedimento corretto del Blackout è uno solo: evacuare l'edificio. Selezionare una porta di
        // successo significa aver evacuato, quindi il punteggio pieno non dipende dalle altre azioni.
        var righe = new List<string>
        {
            DebriefLinePrefix.GOOD + $"Hai aperto la porta {nomePorta}"
        };

        foreach (string errore in erroriGDD)
        {
            righe.Add(DebriefLinePrefix.BAD + errore + " → azione non corretta");
        }

        righe.Add(DebriefLinePrefix.GOOD + "Evacua l'edificio: successo completo");

        SetPhase(PHASE_RESOLUTION);
        EndMission(OutcomeType.Positive, 100, new[] { 100, 100, 100, 100, 100 }, righe);
    }

    /// <summary>Si iscrive all'evento del blackout.</summary>
    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.OnBlackout += HandleBlackout;
        }
    }

    /// <summary>Si disiscrive dall'evento del blackout.</summary>
    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnBlackout -= HandleBlackout;
        }
    }

    /// <summary>Avvia la missione e mostra le prime indicazioni.</summary>
    protected override void OnScenarioStarted()
    {
        erroriGDD.Clear();
        interazioniErrate = 0;
        blackoutAttivo = false;
        tempoFermo = 0f;
        posizioneRigRilevata = false;
        porteSelezionate.Clear();
        obiettivoSegnaleticaCompletato = false;
        RiattivaRilevatore();

        if (gameManager != null)
        {
            gameManager.AggiornaContorniGuidati(Level == DifficultyLevel.Guided);
        }

        AvviaTimerScenario();

        SetPhase(PHASE_SCAN);

        if (Level == DifficultyLevel.Guided)
        {
            SetHint("Segui le luci di emergenza verso l'uscita");
        }
    }

    /// <summary>Chiude la missione per scadenza del tempo con esito negativo.</summary>
    protected override void OnTimerExpired()
    {
        ChiudiMissione();
    }

    /// <summary>Mostra l'avviso di blackout quando il gameplay spegne le luci.</summary>
    private void HandleBlackout()
    {
        if (!IsRunning)
        {
            return;
        }

        blackoutAttivo = true;

        Alert("Blackout in corso\nL'ufficio è al buio", ICONA_AVVISO);

        if (Level == DifficultyLevel.Guided)
        {
            SetHint("Segui le frecce verdi verso l'uscita d'emergenza");
        }
    }

    /// <summary>
    /// Avvia il timer della HUD seguendo il GDD: Guidato senza conto alla rovescia,
    /// Realistico (Operational) 3 minuti, Stress (Crisis) 1,5 minuti. Allo scadere la HUD
    /// chiama già OnTimerExpired, che chiude con esito Negativo e mostra la schermata di fallimento.
    /// Un successo con la porta ferma il timer tramite EndMission.
    /// </summary>
    private void AvviaTimerScenario()
    {
        int secondi;

        switch (Level)
        {
            case DifficultyLevel.Guided:
                secondi = 0; // cronometro, nessun limite
                break;
            case DifficultyLevel.Crisis:
                secondi = 90; // 1,5 minuti
                break;
            default: // DifficultyLevel.Operational (Realistico)
                secondi = 180;
                break;
        }

        HUDService.StartTimer(secondi);
    }

    /// <summary>Chiude la missione per scadenza del tempo con esito negativo.</summary>
    private void ChiudiMissione()
    {
        if (!IsRunning)
        {
            return;
        }

        SetPhase(PHASE_RESOLUTION);
        EndMission(OutcomeType.Negative, -1, CalcolaDimensioni(true), CostruisciDebrief(true));
    }

    /// <summary>Calcola le cinque dimensioni usate dalla UI comune per lo SPI.</summary>
    private int[] CalcolaDimensioni(bool scaduto)
    {
        int erroriTotali = erroriGDD.Count;
        int sicurezza = 100 - erroriTotali * 25 - (scaduto ? 25 : 0);
        int consapevolezza = 100 - erroriTotali * 20;
        int decisione = 100 - erroriTotali * 25;
        int esecuzione = 100 - erroriTotali * 20;
        int tempo = scaduto ? 0 : Clamp100(100 - Mathf.RoundToInt(ElapsedSeconds * 0.2f));

        return new[]
        {
            Clamp100(sicurezza),
            Clamp100(consapevolezza),
            Clamp100(decisione),
            Clamp100(esecuzione),
            Clamp100(tempo)
        };
    }

    /// <summary>Costruisce le righe del debrief con azione e conseguenza.</summary>
    private List<string> CostruisciDebrief(bool scaduto)
    {
        var righe = new List<string>();

        foreach (string errore in erroriGDD)
        {
            righe.Add(DebriefLinePrefix.BAD + errore + " → Evacua l'edificio");
        }

        if (righe.Count == 0)
        {
            righe.Add(DebriefLinePrefix.GOOD + "Nessuna azione non corretta registrata");
        }

        if (scaduto)
        {
            righe.Add(DebriefLinePrefix.CRITICAL + "Tempo scaduto → area non messa in sicurezza");
        }

        return righe;
    }

    /// <summary>Limita una dimensione tra 0 e 100.</summary>
    private static int Clamp100(int valore)
    {
        return Mathf.Clamp(valore, 0, 100);
    }
}
