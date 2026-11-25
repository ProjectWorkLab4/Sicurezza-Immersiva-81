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

    readonly bool[] _obiettiviCompletati = new bool[NUMERO_OBIETTIVI];
    int _sceltaIniziale = -1;
    bool _rilevatoreSegnalato;

    /// <summary>Recupera il gameplay se il campo non è stato assegnato.</summary>
    void Awake()
    {
        if (gestore == null)
        {
            gestore = FindFirstObjectByType<GestoreScenarioGasLeak>();
        }
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

                string prefisso = voce.corretta
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
                SetHint("Prendi il rilevatore sul tavolo");
                break;
            case 1:
                Feedback("Aerare aiuta, ma serve confermare il CO", FeedbackKind.Neutral);
                SetHint("Usa anche il rilevatore CO");
                break;
            case 2:
                Feedback("La manutenzione non è un’emergenza", FeedbackKind.Negative);
                SetHint("In un’emergenza serve il 112");
                break;
            default:
                Feedback("Non hai scelto in tempo", FeedbackKind.Negative);
                SetHint("Usa il rilevatore CO");
                break;
        }
    }

    /// <summary>Completa gli obiettivi in base allo stato del gameplay.</summary>
    void SincronizzaObiettivi()
    {
        if (!IsRunning || gestore == null)
        {
            return;
        }

        CompletaObiettivoSe(0, gestore.CollegaAvvisato || _sceltaIniziale == 0);
        CompletaObiettivoSe(1, gestore.RilevatoreUsato);
        CompletaObiettivoSe(2, gestore.TutteFinestreAperte && gestore.StufettaSpenta);
        CompletaObiettivoSe(3, gestore.Chiamato112 && gestore.Uscito);

        if (gestore.RilevatoreUsato && !_rilevatoreSegnalato)
        {
            _rilevatoreSegnalato = true;
            Alert("Monossido di carbonio confermato\nAera, spegni e chiama il 112", ICONA_AVVISO);
            SetHint("Apri le finestre e spegni la stufetta");
        }
    }

    /// <summary>Segna un obiettivo completato e mostra il feedback sul polso.</summary>
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
        CompleteObjective();
        Feedback(TestoObiettivo(indice), FeedbackKind.Positive);

        if (indice < NUMERO_OBIETTIVI - 1)
        {
            SetPhase(PHASE_ACTION);
        }
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
                return "112 chiamato e uscita completata";
        }
    }

    /// <summary>Calcola le cinque dimensioni usate dalla UI comune per lo SPI.</summary>
    int[] CalcolaDimensioni(EsitoScenario esito)
    {
        bool colleghiRiconosciuti = gestore != null && (gestore.CollegaAvvisato || _sceltaIniziale == 0);
        bool rilevatore = gestore != null && gestore.RilevatoreUsato;
        bool finestre = gestore != null && gestore.TutteFinestreAperte;
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
}
