using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Final outcome of the Gas Leak scenario (GGD: win / partial win / lose).</summary>
public enum EsitoScenario
{
    InCorso = 0,
    SuccessoCompleto = 1,
    SuccessoParziale = 2,
    FallimentoTempo = 3,
    FallimentoIntossicazione = 4
}

/// <summary>One line of the end-of-level recap.</summary>
[Serializable]
public class VoceRecap
{
    public string testo;
    public bool corretta;

    public VoceRecap()
    {
    }

    public VoceRecap(string testo, bool corretta)
    {
        this.testo = testo;
        this.corretta = corretta;
    }
}

/// <summary>UnityEvent carrying the final outcome.</summary>
[Serializable]
public class EsitoEvent : UnityEvent<EsitoScenario>
{
}

/// <summary>
/// Brain of the "Gas Leak" scenario (GGD scenario 8, ID 11).
///
/// It owns the difficulty level, the mission timer, the CO concentration, the
/// progressive damage, the colleague's countdown and the final recap.
///
/// The correct procedure can be performed in any order (GGD: free exploration):
/// recognise the symptoms, use the CO detector, open the windows, turn the heater
/// off, make the colleague follow, call 112 and leave the room with him.
///
/// Everything the team needs to re-balance is a serialized field, so it can be
/// changed from the Inspector without recompiling.
/// </summary>
[DisallowMultipleComponent]
public class GestoreScenarioGasLeak : MonoBehaviour
{
    [Header("Difficolta")]
    [Tooltip("Livello usato all'avvio della scena.")]
    [SerializeField] LivelloDifficolta livelloAvvio = LivelloDifficolta.Realistico;

    [Tooltip("Valori di ogni livello. Modificabili a runtime dall'Inspector (in Play mode) o da codice.")]
    [SerializeField] ImpostazioniDifficolta[] livelli = ImpostazioniDifficolta.CreaSetPredefinito();

    [Tooltip("Avvia il livello da solo allo Start della scena.")]
    [SerializeField] bool avviaAutomaticamente = true;

    [Header("Timer degli eventi (secondi)")]
    [Tooltip("Ritardo dall'avvio prima del primo lamento del collega.")]
    [Min(0f)] [SerializeField] float ritardoPrimoLamento = 6f;
    [Tooltip("Ritardo dall'avvio prima dell'allucinazione (le formiche).")]
    [Min(0f)] [SerializeField] float ritardoAllucinazione = 16f;
    [Tooltip("Intervallo tra due lamenti consecutivi del collega.")]
    [Min(1f)] [SerializeField] float intervalloTraLamenti = 14f;

    [Header("Riferimenti di scena")]
    [SerializeField] CountdownTimer timer;
    [SerializeField] RilevatoreCO rilevatore;
    [SerializeField] StufettaGas stufetta;
    [SerializeField] FinestraApribile[] finestre = new FinestraApribile[0];
    [SerializeField] CollegaNPC collega;
    [SerializeField] SaluteGiocatore salute;
    [SerializeField] HUDScenario hud;
    [SerializeField] TelefonoInterattivo telefono;
    [SerializeField] QuadroElettricoInterattivo quadro;
    [SerializeField] AreaUscita areaUscita;
    [SerializeField] EvidenziatoreScenario[] evidenziatori = new EvidenziatoreScenario[0];

    [Header("Testi")]
    [TextArea(2, 4)]
    [SerializeField] string suggerimentoIniziale =
        "Piu' persone con gli stessi sintomi? Potrebbe essere monossido di carbonio.";
    [TextArea(2, 4)]
    [SerializeField] string messaggioTempoScaduto =
        "Tempo scaduto: non sei riuscito a mettere in sicurezza te e il tuo collega.";

    [Header("Eventi")]
    [SerializeField] UnityEvent onAvviato = new UnityEvent();
    [SerializeField] EsitoEvent onTerminato = new EsitoEvent();

    /// <summary>Fired when the scenario starts.</summary>
    public event Action Avviato;

    /// <summary>Fired once with the final outcome.</summary>
    public event Action<EsitoScenario> Terminato;

    // ---------------------------------------------------------------- state

    public EsitoScenario Esito { get; private set; } = EsitoScenario.InCorso;
    public bool InCorso => Esito == EsitoScenario.InCorso;
    public bool AvviatoScenario { get; private set; }

    public LivelloDifficolta Livello { get; private set; }
    public ImpostazioniDifficolta Impostazioni { get; private set; }

    public float CO { get; private set; }
    public float TempoEsposizione { get; private set; }
    public int Cuori { get; private set; }
    public int CuoriIniziali { get; private set; }
    public int Punteggio { get; private set; }

    public bool RilevatoreUsato { get; private set; }
    public bool StufettaSpenta { get; private set; }
    public bool CollegaAvvisato { get; private set; }
    public bool Chiamato112 { get; private set; }
    public bool ChiamatoDisinfestazione { get; private set; }
    public bool ChiamatoManutenzione { get; private set; }
    public bool QuadroAzionato { get; private set; }
    public bool Uscito { get; private set; }
    public bool UscitoConCollega { get; private set; }

    public bool CollegaSvenuto => collega != null && collega.Svenuto;

    /// <summary>Number of open windows (0..finestre.Length).</summary>
    public int FinestreAperte { get; private set; }

    /// <summary>True when every window of the room is open.</summary>
    public bool TutteFinestreAperte => finestre.Length > 0 && FinestreAperte >= finestre.Length;

    public IReadOnlyList<VoceRecap> Recap => _recap;

    readonly List<VoceRecap> _recap = new List<VoceRecap>();

    float _tempoScenario;
    float _tempoCollega;
    float _prossimoLamento;
    bool _primoLamento;
    bool _allucinazione;
    int _cuoriPersi;
    bool _timerCollegato;

    // ---------------------------------------------------------------- lifecycle

    void Awake()
    {
        if (timer == null)
        {
            timer = FindFirstObjectByType<CountdownTimer>(FindObjectsInactive.Include);
        }

        if (salute == null)
        {
            salute = FindFirstObjectByType<SaluteGiocatore>();
        }
    }

    void OnEnable()
    {
        if (timer != null && !_timerCollegato)
        {
            timer.Expired += HandleTimerScaduto;
            _timerCollegato = true;
        }

        if (telefono != null)
        {
            telefono.ChiamataAvviata += HandleChiamataAvviata;
        }

        if (quadro != null)
        {
            quadro.CambioStato += HandleQuadroCambiato;
        }

        if (areaUscita != null)
        {
            areaUscita.PlayerUscito += HandlePlayerUscito;
        }
    }

    void OnDisable()
    {
        if (timer != null && _timerCollegato)
        {
            timer.Expired -= HandleTimerScaduto;
            _timerCollegato = false;
        }

        if (telefono != null)
        {
            telefono.ChiamataAvviata -= HandleChiamataAvviata;
        }

        if (quadro != null)
        {
            quadro.CambioStato -= HandleQuadroCambiato;
        }

        if (areaUscita != null)
        {
            areaUscita.PlayerUscito -= HandlePlayerUscito;
        }
    }

    void Start()
    {
        ImpostaDifficolta(livelloAvvio);

        if (avviaAutomaticamente)
        {
            Avvia();
        }
    }

    void Update()
    {
        // The HUD mirrors the mission timer every frame, even before the scenario starts.
        if (hud != null && timer != null)
        {
            hud.ImpostaTempo(timer.Remaining);
        }

        if (!AvviatoScenario || !InCorso)
        {
            return;
        }

        float dt = Time.deltaTime;

        _tempoScenario += dt;

        AggiornaCO(dt);
        AggiornaEsposizione(dt);
        AggiornaCollega(dt);
        AggiornaTimerEventi();
    }

    // ---------------------------------------------------------------- public API

    /// <summary>Applies a difficulty level and refreshes HUD, hearts and highlights.</summary>
    public void ImpostaDifficolta(LivelloDifficolta livello)
    {
        Livello = livello;
        Impostazioni = TrovaImpostazioni(livello).Clone();

        Cuori = CuoriIniziali = Mathf.Max(0, Impostazioni.cuori);
        CO = Impostazioni.coIniziale;
        TempoEsposizione = 0f;

        if (salute != null)
        {
            salute.ImpostaCuori(CuoriIniziali);
        }

        if (hud != null)
        {
            hud.ImpostaDifficolta(Impostazioni);
            hud.ImpostaCuori(Cuori, CuoriIniziali);
            hud.ImpostaTempo(Impostazioni.durataMissione);
        }

        ApplicaEvidenziazione(Impostazioni.evidenziaOggetti);
    }

    /// <summary>Starts (or restarts) the scenario with the current difficulty.</summary>
    public void Avvia()
    {
        ImpostaDifficolta(Livello);

        AvviatoScenario = true;
        Esito = EsitoScenario.InCorso;
        Punteggio = 0;

        RilevatoreUsato = false;
        StufettaSpenta = false;
        CollegaAvvisato = false;
        Chiamato112 = false;
        ChiamatoDisinfestazione = false;
        ChiamatoManutenzione = false;
        QuadroAzionato = false;
        Uscito = false;
        UscitoConCollega = false;
        FinestreAperte = 0;

        _recap.Clear();
        _tempoScenario = 0f;
        _tempoCollega = 0f;
        _primoLamento = false;
        _allucinazione = false;
        _cuoriPersi = 0;
        _prossimoLamento = ritardoPrimoLamento;

        if (timer != null)
        {
            timer.StartTimer(Impostazioni.durataMissione);
        }

        if (stufetta != null)
        {
            stufetta.Accendi();
        }

        if (salute != null)
        {
            salute.ImpostaCuori(CuoriIniziali);
            salute.ImpostaLivelloMale(0f, false);
        }

        if (hud != null)
        {
            hud.ImpostaTempo(Impostazioni.durataMissione);
            hud.ImpostaCuori(Cuori, CuoriIniziali);
            hud.NascondiEsito();
        }

        if (Impostazioni.mostraSuggerimenti && !string.IsNullOrEmpty(suggerimentoIniziale))
        {
            hud?.MostraSuggerimento(suggerimentoIniziale, 8f);
        }

        onAvviato.Invoke();
        Avviato?.Invoke();
    }

    /// <summary>Called by the CO detector the first time the player picks it up.</summary>
    public void SegnalaRilevatoreUsato()
    {
        if (RilevatoreUsato)
        {
            return;
        }

        RilevatoreUsato = true;
        hud?.MostraSuggerimento("Il rilevatore conferma la presenza di monossido di carbonio.", 5f);
    }

    /// <summary>Called by the heater when the player turns it off.</summary>
    public void SegnalaStufettaSpenta()
    {
        if (StufettaSpenta)
        {
            return;
        }

        StufettaSpenta = true;
        hud?.MostraSuggerimento("Stufetta spenta: la salita del monossido si ferma.", 4f);
    }

    /// <summary>Called by every window when it opens or closes.</summary>
    public void SegnalaFinestra(bool aperta)
    {
        FinestreAperte = 0;

        foreach (FinestraApribile finestra in finestre)
        {
            if (finestra != null && finestra.Aperta)
            {
                FinestreAperte++;
            }
        }

        if (aperta)
        {
            hud?.MostraSuggerimento("Finestra aperta: il monossido sale piu' lentamente.", 4f);
        }
    }

    /// <summary>Called by the colleague when the player interacts with him.</summary>
    public void SegnalaCollegaAvvisato()
    {
        if (CollegaAvvisato)
        {
            return;
        }

        CollegaAvvisato = true;
        hud?.MostraSuggerimento("Il collega ti seguira'. Portalo fuori con te.", 5f);
    }

    /// <summary>Called by the phone for every placed call.</summary>
    public void SegnalaChiamata(string nomeContatto, string numero)
    {
        string pulito = SoloCifre(numero);

        if (pulito == "112")
        {
            if (Chiamato112)
            {
                return;
            }

            Chiamato112 = true;
            hud?.MostraSuggerimento("Il 112 ti dice di aerare e di uscire subito.", 6f);
        }
        else if (!string.IsNullOrEmpty(nomeContatto) &&
                 nomeContatto.IndexOf("disinfest", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            ChiamatoDisinfestazione = true;
        }
        else if (!string.IsNullOrEmpty(nomeContatto) &&
                 nomeContatto.IndexOf("manutenz", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            ChiamatoManutenzione = true;
        }
    }

    /// <summary>Called when the player walks through the exit door.</summary>
    public void SegnalaUscita(bool conCollega)
    {
        if (!InCorso)
        {
            return;
        }

        Uscito = true;
        UscitoConCollega = conCollega;

        if (conCollega && Chiamato112)
        {
            Termina(EsitoScenario.SuccessoCompleto);
        }
        else
        {
            Termina(EsitoScenario.SuccessoParziale);
        }
    }

    /// <summary>Ends the scenario. Calling it twice has no effect.</summary>
    public void Termina(EsitoScenario esito)
    {
        if (!InCorso)
        {
            return;
        }

        Esito = esito;

        if (timer != null)
        {
            timer.Pause();
        }

        Punteggio = CalcolaPunteggio(esito);
        CostruisciRecap(esito);

        if (salute != null && esito == EsitoScenario.FallimentoIntossicazione)
        {
            salute.Svenimento();
        }

        if (hud != null)
        {
            hud.MostraEsito(esito, Punteggio, _recap);
        }

        if (esito == EsitoScenario.FallimentoTempo && !string.IsNullOrEmpty(messaggioTempoScaduto))
        {
            hud?.MostraSuggerimento(messaggioTempoScaduto, 6f);
        }

        onTerminato.Invoke(esito);
        Terminato?.Invoke(esito);
    }

    /// <summary>Returns the settings of a level (never null).</summary>
    public ImpostazioniDifficolta TrovaImpostazioni(LivelloDifficolta livello)
    {
        if (livelli != null)
        {
            foreach (ImpostazioniDifficolta impostazioni in livelli)
            {
                if (impostazioni != null && impostazioni.livello == livello)
                {
                    return impostazioni;
                }
            }

            foreach (ImpostazioniDifficolta impostazioni in livelli)
            {
                if (impostazioni != null)
                {
                    return impostazioni;
                }
            }
        }

        return new ImpostazioniDifficolta { livello = livello, nome = livello.ToString() };
    }

    // ---------------------------------------------------------------- simulation

    void AggiornaCO(float dt)
    {
        float tasso = stufetta != null && stufetta.Accesa ? Impostazioni.salitaCO : 0f;

        if (finestre.Length > 0)
        {
            float frazione = (float)FinestreAperte / finestre.Length;
            float moltiplicatore = Mathf.Lerp(1f, Impostazioni.moltiplicatoreFinestreAperte, frazione);
            tasso *= moltiplicatore;
        }

        CO = Mathf.Clamp(CO + tasso * dt, 0f, 999f);
    }

    void AggiornaEsposizione(float dt)
    {
        if (CO < Impostazioni.sogliaEsposizione)
        {
            // Aria di nuovo respirabile: l'esposizione non avanza.
            if (salute != null)
            {
                salute.ImpostaLivelloMale(0f, false);
            }

            return;
        }

        TempoEsposizione += dt;

        bool battito = TempoEsposizione >= Impostazioni.sogliaBattito;
        float male = 0f;

        if (TempoEsposizione >= Impostazioni.sogliaBattito)
        {
            male = Mathf.InverseLerp(Impostazioni.sogliaBattito,
                Mathf.Max(Impostazioni.sogliaBattito + 1f, Impostazioni.sogliaVignettatura),
                TempoEsposizione) * 0.45f;
        }

        if (TempoEsposizione >= Impostazioni.sogliaVignettatura)
        {
            float oltre = TempoEsposizione - Impostazioni.sogliaVignettatura;
            male = 0.45f + Mathf.Clamp01(oltre / Mathf.Max(1f, Impostazioni.intervalloPerditaCuore)) * 0.55f;

            if (Impostazioni.cuori > 0)
            {
                int dovuti = Mathf.FloorToInt(oltre / Mathf.Max(1f, Impostazioni.intervalloPerditaCuore));

                while (_cuoriPersi < dovuti)
                {
                    _cuoriPersi++;
                    PerdiCuore();
                }
            }
            else if (Impostazioni.UsaFeedbackPollice && hud != null && !_polliceGiuMostrato)
            {
                _polliceGiuMostrato = true;
                hud.MostraFeedbackPollice(-1);
            }
        }

        if (salute != null)
        {
            salute.ImpostaLivelloMale(male, battito);
        }
    }

    bool _polliceGiuMostrato;

    void AggiornaCollega(float dt)
    {
        if (collega == null || collega.Svenuto)
        {
            return;
        }

        if (CO < Impostazioni.sogliaEsposizione)
        {
            return;
        }

        _tempoCollega += dt;

        if (_tempoCollega >= Impostazioni.tempoSvenimento)
        {
            collega.Svenisci();
        }
    }

    void AggiornaTimerEventi()
    {
        if (collega == null)
        {
            return;
        }

        if (!_primoLamento && _tempoScenario >= ritardoPrimoLamento)
        {
            _primoLamento = true;
            collega.IniziaSintomi();
            _prossimoLamento = _tempoScenario + intervalloTraLamenti;
        }
        else if (_primoLamento && _tempoScenario >= _prossimoLamento)
        {
            _prossimoLamento = _tempoScenario + intervalloTraLamenti;
            collega.Lamento();
        }

        if (!_allucinazione && _tempoScenario >= ritardoAllucinazione)
        {
            _allucinazione = true;
            collega.MostraAllucinazione();
        }
    }

    void PerdiCuore()
    {
        if (Cuori <= 0)
        {
            return;
        }

        Cuori--;

        salute?.MostraDanno();
        hud?.ImpostaCuori(Cuori, CuoriIniziali);

        if (Cuori <= 0)
        {
            Termina(EsitoScenario.FallimentoIntossicazione);
        }
    }

    // ---------------------------------------------------------------- callbacks

    void HandleTimerScaduto()
    {
        Termina(EsitoScenario.FallimentoTempo);
    }

    void HandleChiamataAvviata(int indice)
    {
        if (telefono == null)
        {
            return;
        }

        TelefonoInterattivo.Contatto contatto = telefono.PerIndice(indice);

        if (contatto != null)
        {
            SegnalaChiamata(contatto.nome, contatto.numero);
        }
    }

    void HandleQuadroCambiato(bool acceso)
    {
        if (QuadroAzionato)
        {
            return;
        }

        QuadroAzionato = true;
        hud?.MostraSuggerimento("Il quadro elettrico non ferma il monossido: non e' compito del lavoratore.", 5f);
    }

    void HandlePlayerUscito(bool conCollega)
    {
        SegnalaUscita(conCollega);
    }

    // ---------------------------------------------------------------- helpers

    void ApplicaEvidenziazione(bool attivo)
    {
        if (evidenziatori == null)
        {
            return;
        }

        foreach (EvidenziatoreScenario evidenziatore in evidenziatori)
        {
            if (evidenziatore != null)
            {
                evidenziatore.ImpostaColore(Impostazioni.coloreEvidenziazione);
                evidenziatore.ImpostaAttivo(attivo);
            }
        }
    }

    int CalcolaPunteggio(EsitoScenario esito)
    {
        switch (esito)
        {
            case EsitoScenario.SuccessoCompleto:
                return Impostazioni.punteggioCompleto;
            case EsitoScenario.SuccessoParziale:
                return Impostazioni.punteggioParziale;
            default:
                return Impostazioni.punteggioFallimento;
        }
    }

    void CostruisciRecap(EsitoScenario esito)
    {
        _recap.Clear();

        AggiungiVoce("Hai riconosciuto i sintomi del collega.", CollegaAvvisato);
        AggiungiVoce("Hai usato il rilevatore di CO.", RilevatoreUsato);
        AggiungiVoce("Hai aperto le finestre.", FinestreAperte > 0);
        AggiungiVoce("Hai spento la stufetta.", StufettaSpenta);
        AggiungiVoce("Hai chiamato il 112.", Chiamato112);
        AggiungiVoce("Sei uscito dalla stanza con il collega.", UscitoConCollega);

        if (QuadroAzionato)
        {
            AggiungiVoce("Hai agito sul quadro elettrico: non serve a fermare il CO.", false);
        }

        if (ChiamatoDisinfestazione)
        {
            AggiungiVoce("Hai chiamato la disinfestazione: tempo perso.", false);
        }

        if (ChiamatoManutenzione)
        {
            AggiungiVoce("Hai chiamato la manutenzione: non e' un'emergenza.", false);
        }

        if (CollegaSvenuto)
        {
            AggiungiVoce("Il collega e' svenuto prima di uscire.", false);
        }

        if (esito == EsitoScenario.FallimentoTempo)
        {
            AggiungiVoce("Il tempo a disposizione e' terminato.", false);
        }
        else if (esito == EsitoScenario.FallimentoIntossicazione)
        {
            AggiungiVoce("Hai perso tutti i cuori per l'intossicazione.", false);
        }
    }

    void AggiungiVoce(string testo, bool corretta)
    {
        _recap.Add(new VoceRecap(testo, corretta));
    }

    static string SoloCifre(string valore)
    {
        if (string.IsNullOrEmpty(valore))
        {
            return string.Empty;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder(valore.Length);

        foreach (char carattere in valore)
        {
            if (char.IsDigit(carattere))
            {
                builder.Append(carattere);
            }
        }

        return builder.ToString();
    }
}
