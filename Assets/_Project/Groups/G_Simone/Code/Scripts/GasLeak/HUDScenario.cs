using System.Collections.Generic;
using SI81.UI;
using UnityEngine;

/// <summary>
/// Ponte di compatibilità per i vecchi script della UI di Gas Leak.
///
/// Non crea Canvas: suggerimenti, feedback e avvisi passano allo scenario condiviso, che li
/// instrada alla UI comune di SCN_Core (guida §3 caso B e §5). La schermata finale è gestita
/// una sola volta da GasLeakScenario con EndMission.
/// </summary>
[DisallowMultipleComponent]
public class HUDScenario : MonoBehaviour
{
    [Tooltip("ScenarioController su MNG_Scenario. Se vuoto viene cercato in scena.")]
    [SerializeField] ScenarioController scenario;

    /// <summary>Scenario condiviso usato per parlare alla UI comune.</summary>
    ScenarioController Controller
    {
        get
        {
            if (scenario == null)
            {
                scenario = FindFirstObjectByType<ScenarioController>(FindObjectsInactive.Include);
            }

            return scenario;
        }
    }

    /// <summary>Il timer è della UI comune; il metodo resta per compatibilità.</summary>
    public void ImpostaTempo(float secondi)
    {
    }

    /// <summary>I cuori non hanno un canale dedicato; il metodo resta per compatibilità.</summary>
    public void ImpostaCuori(int attuali, int massimo)
    {
    }

    /// <summary>La fase è decisa da GasLeakScenario; il metodo resta per compatibilità.</summary>
    public void ImpostaDifficolta(ImpostazioniDifficolta impostazioni)
    {
    }

    /// <summary>Mostra un suggerimento sul polso tramite lo scenario.</summary>
    public void MostraSuggerimento(string testo, float durata)
    {
        if (string.IsNullOrWhiteSpace(testo))
        {
            return;
        }

        Controller?.SetHint(testo);
    }

    /// <summary>Mostra il feedback del pollice tramite lo scenario.</summary>
    public void MostraFeedbackPollice(int livello)
    {
        switch (livello)
        {
            case 1:
                Controller?.Feedback("Pollice su", FeedbackKind.Positive);
                break;
            case 0:
                Controller?.Feedback("Così così", FeedbackKind.Neutral);
                break;
            default:
                Controller?.Feedback("Pollice giù", FeedbackKind.Negative);
                break;
        }
    }

    /// <summary>Non apre pannelli locali: EndMission gestisce l'esito nella UI comune.</summary>
    public void MostraEsito(EsitoScenario esito, int punteggio, IReadOnlyList<VoceRecap> recap)
    {
    }

    /// <summary>Nessun pannello locale da nascondere: la UI comune gestisce la chiusura.</summary>
    public void NascondiEsito()
    {
    }

    /// <summary>Non esiste un esito locale; la UI comune mostra Debrief e punteggio.</summary>
    public bool EsitoVisibile => false;
}
