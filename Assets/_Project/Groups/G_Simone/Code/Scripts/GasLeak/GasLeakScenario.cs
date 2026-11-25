using System;
using System.Collections.Generic;
using SI81.UI;
using UnityEngine;

/// <summary>
/// Punto d’ingresso di Gas Leak per la UI comune.
///
/// Sta su MNG_Scenario al posto del ScenarioController di base. Collega il gameplay esistente
/// ai soli canali ammessi: obiettivi, fase, suggerimenti, sottotitoli, avvisi, feedback,
/// decisione e fine missione. Non crea Canvas né pannelli locali (guida §3 caso B, §5, §7).
/// </summary>
[DisallowMultipleComponent]
public sealed class GasLeakScenario : ScenarioController
{
    const int NUMERO_OBIETTIVI = 4;
    const string ICONA_AVVISO = "ICN_Warning_Filled";

    [Tooltip("Cervello del gameplay presente su MNG_Scenario.")]
    [SerializeField] GestoreScenarioGasLeak gestore;

    [Header("Debug")]
    [Tooltip("Se attivo, l'Inspector di MNG_Scenario mostra tutte le variabili di scenario e gameplay.")]
    [SerializeField] bool debugMode;

    readonly bool[] _obiettiviCompletati = new bool[NUMERO_OBIETTIVI];
    int _sceltaIniziale = -1;
    bool _rilevatoreSegnalato;
    bool _stufettaSegnalata;
    int _finestreSegnalate;
    bool _chiamata112Segnalata;
    string _hintDecisione = "Riconosci i sintomi e usa il rilevatore CO";
    string _hintCorrente = string.Empty;
    readonly Dictionary<OutcomeType, string> _criteriOriginali = new Dictionary<OutcomeType, string>();

    /// <summary>Recupera il gameplay se il campo non è stato assegnato.</summary>
    void Awake()
    {
        if (gestore == null)
        {
            gestore = FindFirstObjectByType<GestoreScenarioGasLeak>();
        }

        SalvaCriteriOriginali();
    }

    /// <summary>Si iscrive all'esito finale del gameplay.</summary>
    void OnEnable()
    {
        if (gestore != null)
        {
            gestore.Terminato += HandleTerminato;
        }
    }

    /// <summary>Si disiscrive dall'esito finale del gameplay.</summary>
    void OnDisable()
    {
        if (gestore != null)
        {
            gestore.Terminato -= HandleTerminato;
        }
    }

    /// <summary>Aggiorna obiettivi e avvisi mentre la missione è in corso.</summary>
    void Update()
    {
        SincronizzaObiettivi();
    }

    /// <summary>Avvia il gameplay e le prime indicazioni della UI comune.</summary>
    protected override void OnScenarioStarted()
    {
        _sceltaIniziale = -1;
        _rilevatoreSegnalato = false;
        _stufettaSegnalata = false;
        _finestreSegnalate = 0;
        _chiamata112Segnalata = false;
        _hintDecisione = "Riconosci i sintomi e usa il rilevatore CO";
        _hintCorrente = string.Empty;
        RipristinaCriteri();
        for (int i = 0; i < _obiettiviCompletati.Length; i++)
        {
            _obiettiviCompletati[i] = false;
        }

        if (gestore != null)
        {
            gestore.ImpostaDifficolta(FromSharedLevel(Level));
            gestore.Avvia();
        }

        SetPhase(PHASE_SCAN);
        Subtitle("Caposquadra", "Due colleghi hanno mal di testa e nausea: può essere monossido.", 6f);
        SetHint("Riconosci i sintomi e usa il rilevatore CO");

        AskDecision(
            "Due colleghi hanno mal di testa e nausea. Cosa fai prima?",
            new[] { "Usa il rilevatore di CO", "Apri subito le finestre", "Chiama la manutenzione" },
            OnDecisioneIniziale,
            20f);
    }

    /// <summary>Chiude la missione per scadenza del tempo con esito negativo.</summary>
    protected override void OnTimerExpired()
    {
        if (gestore != null)
        {
            gestore.Termina(EsitoScenario.FallimentoTempo);
            return;
        }

        HandleTerminato(EsitoScenario.FallimentoTempo);
    }

    /// <summary>Riceve l'esito del gameplay e chiude la missione una sola volta.</summary>
    void HandleTerminato(EsitoScenario esito)
    {
        if (!IsRunning)
        {
            return;
        }

        SincronizzaObiettivi();
        SetPhase(PHASE_RESOLUTION);
        ImpostaTestoCriterio(esito);

        EndMission(ToOutcome(esito), -1, CalcolaDimensioni(esito), CostruisciDebrief(esito));
    }

    /// <summary>Costruisce le righe del debrief con azione e conseguenza.</summary>
    List<string> CostruisciDebrief(EsitoScenario esito)
    {
        var righe = new List<string>();

        if (gestore != null && gestore.Recap != null)
        {
            foreach (VoceRecap voce in gestore.Recap)
            {
                if (voce == null || string.IsNullOrWhiteSpace(voce.testo))
                {
                    continue;
                }

                bool corretta = voce.corretta;
                if (voce.testo.IndexOf("sintomi del collega", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    corretta = gestore.CollegaAvvisato || _sceltaIniziale == 0;
                }
                else if (voce.testo.IndexOf("chiamato il 112", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    corretta = gestore.Chiamato112;
                }

                string prefisso = corretta
                    ? DebriefLinePrefix.GOOD
                    : voce.critica ? DebriefLinePrefix.CRITICAL : DebriefLinePrefix.BAD;
                righe.Add(prefisso + FormattaVoce(voce));
            }
        }

        if (righe.Count == 0)
        {
            string conseguenza = esito == EsitoScenario.FallimentoTempo
                ? "tempo scaduto"
                : "area non messa in sicurezza";
            righe.Add(DebriefLinePrefix.CRITICAL + "Non è stato possibile mettere in sicurezza l’area → " + conseguenza);
        }

        return righe;
    }

    /// <summary>Gestisce la prima decisione e aggiorna fase, feedback e suggerimento.</summary>
    void OnDecisioneIniziale(int scelta)
    {
        if (!IsRunning)
        {
            return;
        }

        _sceltaIniziale = scelta;
        SetPhase(PHASE_ACTION);

        switch (scelta)
        {
            case 0:
                Feedback("Scelta corretta: usa il rilevatore", FeedbackKind.Positive);
                _hintDecisione = "Prendi il rilevatore sul tavolo";
                break;
            case 1:
                Feedback("Aerare aiuta, ma serve confermare il CO", FeedbackKind.Neutral);
                _hintDecisione = "Usa anche il rilevatore CO";
                break;
            case 2:
                Feedback("La manutenzione non è un’emergenza", FeedbackKind.Negative);
                _hintDecisione = "In un’emergenza serve il 112";
                break;
            default:
                Feedback("Non hai scelto in tempo", FeedbackKind.Negative);
                _hintDecisione = "Usa il rilevatore CO";
                break;
        }

        AggiornaHint();
    }

    /// <summary>Completa gli obiettivi, segnala le azioni e aggiorna il suggerimento.</summary>
    void SincronizzaObiettivi()
    {
        if (!IsRunning || gestore == null)
        {
            return;
        }

        CompletaObiettivoSe(0, gestore.CollegaAvvisato || _sceltaIniziale == 0);
        CompletaObiettivoSe(1, gestore.RilevatoreUsato);
        CompletaObiettivoSe(2, gestore.FinestreAperte > 0 && gestore.StufettaSpenta);
        CompletaObiettivoSe(3, gestore.Chiamato112 && gestore.UscitoConCollega);

        bool rilevatoreNuovo = gestore.RilevatoreUsato && !_rilevatoreSegnalato;
        bool stufettaNuova = gestore.StufettaSpenta && !_stufettaSegnalata;
        bool finestraNuova = gestore.FinestreAperte > _finestreSegnalate;
        bool chiamataNuova = gestore.Chiamato112 && !_chiamata112Segnalata;

        if (rilevatoreNuovo)
        {
            _rilevatoreSegnalato = true;
            Alert("Monossido di carbonio confermato\nAera, spegni e chiama il 112", ICONA_AVVISO);
        }

        if (stufettaNuova)
        {
            _stufettaSegnalata = true;
            Alert("Stufetta spenta\nLa salita del monossido si ferma", ICONA_AVVISO);
            Feedback("Stufetta spenta", FeedbackKind.Positive);
        }

        if (finestraNuova)
        {
            bool primaFinestra = _finestreSegnalate == 0;
            _finestreSegnalate = gestore.FinestreAperte;
            if (primaFinestra)
            {
                Alert("Finestra aperta\nL’aria si sta pulendo", ICONA_AVVISO);
            }

            Feedback("Finestra aperta", FeedbackKind.Positive);
        }

        if (chiamataNuova)
        {
            _chiamata112Segnalata = true;
            Feedback("112 chiamato", FeedbackKind.Positive);
        }

        AggiornaHint();
    }

    /// <summary>Segna un obiettivo completato e porta il contatore al primo non fatto.</summary>
    void CompletaObiettivoSe(int indice, bool condizione)
    {
        if (indice < 0 || indice >= _obiettiviCompletati.Length || indice >= ObjectiveCount)
        {
            return;
        }

        if (_obiettiviCompletati[indice] || !condizione)
        {
            return;
        }

        _obiettiviCompletati[indice] = true;
        Feedback(TestoObiettivo(indice), FeedbackKind.Positive);

        if (indice < NUMERO_OBIETTIVI - 1)
        {
            int successivo = TrovaProssimoObiettivo();
            if (successivo >= 0)
            {
                SetObjective(successivo);
            }

            SetPhase(PHASE_ACTION);
        }
    }

    /// <summary>Restituisce l’indice del primo obiettivo ancora da completare.</summary>
    int TrovaProssimoObiettivo()
    {
        for (int i = 0; i < _obiettiviCompletati.Length; i++)
        {
            if (!_obiettiviCompletati[i])
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Mostra il suggerimento del passo successivo solo quando cambia.</summary>
    void AggiornaHint()
    {
        if (!IsRunning || gestore == null)
        {
            return;
        }

        string hint = CalcolaHint();
        if (hint == _hintCorrente)
        {
            return;
        }

        _hintCorrente = hint;
        SetHint(hint);
    }

    /// <summary>Calcola il suggerimento in base allo stato reale del gameplay.</summary>
    string CalcolaHint()
    {
        bool nessunaAzione = !_rilevatoreSegnalato && !_stufettaSegnalata && _finestreSegnalate == 0;
        if (nessunaAzione)
        {
            return string.IsNullOrEmpty(_hintDecisione) ? "Usa il rilevatore CO" : _hintDecisione;
        }

        if (gestore.Chiamato112)
        {
            return gestore.Uscito ? "Uscita completata" : "Esci dalla stanza con il collega";
        }

        if (!gestore.CollegaAvvisato)
        {
            return "Avvisa il collega: deve seguirti";
        }

        bool finestre = gestore.FinestreAperte > 0;
        bool stufetta = gestore.StufettaSpenta;

        if (!finestre && stufetta)
        {
            return "Apri le finestre";
        }

        if (finestre && !stufetta)
        {
            return "Spegni la stufetta";
        }

        if (!finestre || !stufetta)
        {
            return "Apri le finestre e spegni la stufetta";
        }

        return _rilevatoreSegnalato ? "Chiama il 112 dal telefono" : "Usa il rilevatore CO";
    }

    /// <summary>Restituisce il testo breve del feedback di un obiettivo.</summary>
    string TestoObiettivo(int indice)
    {
        switch (indice)
        {
            case 0:
                return "Sintomi riconosciuti";
            case 1:
                return "Monossido confermato";
            case 2:
                return "Area aerata e stufetta spenta";
            default:
                return "112 chiamato e collega fuori";
        }
    }

    /// <summary>Calcola le cinque dimensioni usate dalla UI comune per lo SPI.</summary>
    int[] CalcolaDimensioni(EsitoScenario esito)
    {
        bool colleghiRiconosciuti = gestore != null && (gestore.CollegaAvvisato || _sceltaIniziale == 0);
        bool rilevatore = gestore != null && gestore.RilevatoreUsato;
        bool finestre = gestore != null && gestore.FinestreAperte > 0;
        bool stufetta = gestore != null && gestore.StufettaSpenta;
        bool chiamato112 = gestore != null && gestore.Chiamato112;
        bool uscitoConCollega = gestore != null && gestore.UscitoConCollega;
        bool collegaSvenuto = gestore != null && gestore.CollegaSvenuto;
        bool quadro = gestore != null && gestore.QuadroAzionato;
        bool disinfestazione = gestore != null && gestore.ChiamatoDisinfestazione;
        bool manutenzione = gestore != null && gestore.ChiamatoManutenzione;
        int cuori = gestore != null ? gestore.Cuori : 0;

        int sicurezza = 35 + cuori * 15 + (uscitoConCollega ? 25 : 0) - (collegaSvenuto ? 20 : 0);
        int consapevolezza = 20 + (colleghiRiconosciuti ? 15 : 0) + (rilevatore ? 20 : 0)
            + (finestre ? 10 : 0) + (stufetta ? 10 : 0) + (_sceltaIniziale == 0 ? 15 : 0);
        int decisione = 25 + (chiamato112 ? 30 : 0) + (!quadro ? 15 : 0)
            + (!disinfestazione ? 15 : 0) + (!manutenzione ? 15 : 0);
        int esecuzione = 15 + (rilevatore ? 10 : 0) + (finestre ? 20 : 0) + (stufetta ? 20 : 0)
            + (chiamato112 ? 15 : 0) + (uscitoConCollega ? 20 : 0);

        float durata = 180f;
        if (gestore != null && gestore.Impostazioni != null)
        {
            durata = Mathf.Max(1f, gestore.Impostazioni.durataMissione);
        }

        int tempo = Mathf.RoundToInt(100f * (1f - Mathf.Clamp01(ElapsedSeconds / durata)));
        if (esito == EsitoScenario.FallimentoTempo)
        {
            tempo = 0;
        }

        return new[]
        {
            Clamp100(sicurezza),
            Clamp100(consapevolezza),
            Clamp100(decisione),
            Clamp100(esecuzione),
            Clamp100(tempo)
        };
    }

    /// <summary>Limita una dimensione tra 0 e 100.</summary>
    static int Clamp100(int valore)
    {
        return Mathf.Clamp(valore, 0, 100);
    }

    /// <summary>Formatta una voce del riepilogo come azione → conseguenza.</summary>
    static string FormattaVoce(VoceRecap voce)
    {
        string azione = (voce.testo ?? string.Empty).Trim().TrimEnd('.');
        string conseguenza = (voce.conseguenza ?? string.Empty).Trim().TrimEnd('.');

        if (conseguenza.Length == 0)
        {
            return azione;
        }

        return azione + " → " + conseguenza;
    }

    /// <summary>Converte il livello condiviso nel livello del gameplay.</summary>
    static LivelloDifficolta FromSharedLevel(DifficultyLevel level)
    {
        switch (level)
        {
            case DifficultyLevel.Guided:
                return LivelloDifficolta.Guidato;
            case DifficultyLevel.Crisis:
                return LivelloDifficolta.Stress;
            default:
                return LivelloDifficolta.Realistico;
        }
    }

    /// <summary>Converte l'esito del gameplay nell'esito della UI comune.</summary>
    static OutcomeType ToOutcome(EsitoScenario esito)
    {
        switch (esito)
        {
            case EsitoScenario.SuccessoCompleto:
                return OutcomeType.Positive;
            case EsitoScenario.SuccessoParziale:
                return OutcomeType.Partial;
            default:
                return OutcomeType.Negative;
        }
    }

    /// <summary>Salva i testi dei criteri come sono nell'asset della missione.</summary>
    void SalvaCriteriOriginali()
    {
        _criteriOriginali.Clear();
        if (Mission == null || Mission.Criteria == null)
        {
            return;
        }

        foreach (CompletionCriterion criterio in Mission.Criteria)
        {
            if (criterio != null && !_criteriOriginali.ContainsKey(criterio.Outcome))
            {
                _criteriOriginali[criterio.Outcome] = criterio.Text;
            }
        }
    }

    /// <summary>Ripristina i testi originali dei criteri all'inizio di ogni partita.</summary>
    void RipristinaCriteri()
    {
        if (Mission == null || Mission.Criteria == null)
        {
            return;
        }

        foreach (CompletionCriterion criterio in Mission.Criteria)
        {
            if (criterio != null && _criteriOriginali.TryGetValue(criterio.Outcome, out string testo))
            {
                criterio.Text = testo;
            }
        }
    }

    /// <summary>Aggiorna la didascalia dell'esito con quello che è stato davvero fatto o mancato.</summary>
    void ImpostaTestoCriterio(EsitoScenario esito)
    {
        RipristinaCriteri();
        if (Mission == null || Mission.Criteria == null)
        {
            return;
        }

        string testo = TestoCriterio(esito);
        if (string.IsNullOrEmpty(testo))
        {
            return;
        }

        OutcomeType outcome = ToOutcome(esito);
        foreach (CompletionCriterion criterio in Mission.Criteria)
        {
            if (criterio != null && criterio.Outcome == outcome)
            {
                criterio.Text = testo;
                return;
            }
        }
    }

    /// <summary>Restituisce la spiegazione dell'esito in base allo stato reale del gameplay.</summary>
    string TestoCriterio(EsitoScenario esito)
    {
        if (gestore == null)
        {
            return null;
        }

        switch (esito)
        {
            case EsitoScenario.SuccessoCompleto:
                return "Collega fuori e 112 chiamato";

            case EsitoScenario.SuccessoParziale:
                if (!gestore.Chiamato112 && !gestore.UscitoConCollega)
                {
                    return "Usciti senza 112 e senza il collega";
                }

                if (!gestore.Chiamato112)
                {
                    return "Usciti ma senza il 112";
                }

                if (!gestore.UscitoConCollega)
                {
                    return "112 chiamato, ma collega non uscito con te";
                }

                return "Collega e 112 presenti: controlla i danni riportati";

            case EsitoScenario.FallimentoTempo:
                return "Tempo scaduto: area non messa in sicurezza";

            case EsitoScenario.FallimentoIntossicazione:
                return "Intossicazione: esposizione al monossido troppo a lungo";

            default:
                return null;
        }
    }
}
