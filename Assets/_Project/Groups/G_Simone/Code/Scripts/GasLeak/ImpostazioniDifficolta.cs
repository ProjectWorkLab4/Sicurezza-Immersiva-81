using System;
using UnityEngine;

/// <summary>Difficulty levels of the Gas Leak scenario (GGD: Guidato / Realistico / Stress).</summary>
public enum LivelloDifficolta
{
    Guidato = 0,
    Realistico = 1,
    Stress = 2
}

/// <summary>
/// Every tunable value of one difficulty level.
///
/// All the numbers are exposed in the Inspector (see <see cref="GestoreScenarioGasLeak"/>),
/// so the team can re-balance the scenario without touching the code.
/// </summary>
[Serializable]
public class ImpostazioniDifficolta
{
    [Header("Identita")]
    public LivelloDifficolta livello = LivelloDifficolta.Realistico;
    public string nome = "Realistico";

    [Header("Tempo della missione (secondi)")]
    [Tooltip("Durata del timer di livello. Allo scadere il livello e' perso.")]
    [Min(10f)] public float durataMissione = 180f;

    [Header("Monossido di carbonio (CO)")]
    [Tooltip("PPM guadagnati al secondo con la stufetta accesa e tutte le finestre chiuse.")]
    [Min(0f)] public float salitaCO = 8f;
    [Tooltip("Moltiplicatore della salita del CO quando tutte le finestre sono aperte.")]
    [Range(0f, 1f)] public float moltiplicatoreFinestreAperte = 0.35f;
    [Tooltip("Valore di CO presente all'avvio del livello.")]
    [Min(0f)] public float coIniziale = 0f;
    [Tooltip("Sotto questo valore di CO il giocatore non accumula esposizione.")]
    [Min(0f)] public float sogliaEsposizione = 25f;

    [Header("Danno progressivo al giocatore")]
    [Tooltip("Cuori disponibili. 0 = nessun cuore (livello Guidato).")]
    [Min(0)] public int cuori = 3;
    [Tooltip("Secondi di esposizione prima del battito accelerato e della vista offuscata.")]
    [Min(0f)] public float sogliaBattito = 40f;
    [Tooltip("Secondi di esposizione prima della vignettatura.")]
    [Min(0f)] public float sogliaVignettatura = 60f;
    [Tooltip("Secondi tra la perdita di un cuore e la successiva.")]
    [Min(1f)] public float intervalloPerditaCuore = 20f;

    [Header("Collega NPC")]
    [Tooltip("Secondi prima che il collega svenga, se non viene portato fuori.")]
    [Min(5f)] public float tempoSvenimento = 75f;

    [Header("Aiuti e feedback")]
    [Tooltip("Evidenzia rilevatore, finestre, stufetta e collega con il colore #E69F00.")]
    public bool evidenziaOggetti;
    [Tooltip("Mostra i suggerimenti testuali del livello guidato.")]
    public bool mostraSuggerimenti;
    [Tooltip("Mostra i cuori nell'HUD. Se disattivo compare il feedback pollice.")]
    public bool mostraCuori = true;
    [Tooltip("Mostra il feedback pollice su / intermedio / giu' invece dei cuori.")]
    public bool mostraFeedbackPollice;
    [Tooltip("Colore dell'evidenziazione degli oggetti (GGD: #E69F00).")]
    public Color coloreEvidenziazione = new Color(0.902f, 0.624f, 0f, 1f);

    [Header("Punteggio (GDD)")]
    public int punteggioCompleto = 30;
    public int punteggioParziale = 10;
    public int punteggioFallimento = 0;

    /// <summary>True when this level shows the thumbs feedback instead of hearts.</summary>
    public bool UsaFeedbackPollice => mostraFeedbackPollice || cuori <= 0;

    /// <summary>Returns a deep copy of these settings.</summary>
    public ImpostazioniDifficolta Clone()
    {
        return (ImpostazioniDifficolta)MemberwiseClone();
    }

    /// <summary>Default tuning for the three GGD levels.</summary>
    public static ImpostazioniDifficolta[] CreaSetPredefinito()
    {
        return new[]
        {
            new ImpostazioniDifficolta
            {
                livello = LivelloDifficolta.Guidato,
                nome = "Guidato",
                durataMissione = 240f,
                salitaCO = 6f,
                moltiplicatoreFinestreAperte = 0.3f,
                coIniziale = 0f,
                sogliaEsposizione = 25f,
                cuori = 0,
                sogliaBattito = 60f,
                sogliaVignettatura = 90f,
                intervalloPerditaCuore = 30f,
                tempoSvenimento = 150f,
                evidenziaOggetti = true,
                mostraSuggerimenti = true,
                mostraCuori = false,
                mostraFeedbackPollice = true,
                coloreEvidenziazione = new Color(0.902f, 0.624f, 0f, 1f),
                punteggioCompleto = 0,
                punteggioParziale = 0,
                punteggioFallimento = 0
            },
            new ImpostazioniDifficolta
            {
                livello = LivelloDifficolta.Realistico,
                nome = "Realistico",
                durataMissione = 180f,
                salitaCO = 8f,
                moltiplicatoreFinestreAperte = 0.35f,
                coIniziale = 0f,
                sogliaEsposizione = 25f,
                cuori = 3,
                sogliaBattito = 40f,
                sogliaVignettatura = 60f,
                intervalloPerditaCuore = 20f,
                tempoSvenimento = 75f,
                evidenziaOggetti = false,
                mostraSuggerimenti = false,
                mostraCuori = true,
                mostraFeedbackPollice = false,
                coloreEvidenziazione = new Color(0.902f, 0.624f, 0f, 1f),
                punteggioCompleto = 30,
                punteggioParziale = 10,
                punteggioFallimento = 0
            },
            new ImpostazioniDifficolta
            {
                livello = LivelloDifficolta.Stress,
                nome = "Stress",
                durataMissione = 120f,
                salitaCO = 14f,
                moltiplicatoreFinestreAperte = 0.5f,
                coIniziale = 15f,
                sogliaEsposizione = 25f,
                cuori = 3,
                sogliaBattito = 25f,
                sogliaVignettatura = 40f,
                intervalloPerditaCuore = 15f,
                tempoSvenimento = 55f,
                evidenziaOggetti = false,
                mostraSuggerimenti = false,
                mostraCuori = true,
                mostraFeedbackPollice = false,
                coloreEvidenziazione = new Color(0.902f, 0.624f, 0f, 1f),
                punteggioCompleto = 50,
                punteggioParziale = 20,
                punteggioFallimento = 0
            }
        };
    }
}
