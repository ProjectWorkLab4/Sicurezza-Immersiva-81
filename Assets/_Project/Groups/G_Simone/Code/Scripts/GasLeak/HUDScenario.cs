using System.Collections;
using System.Collections.Generic;
using SI81.UI;
using UnityEngine;

/// <summary>
/// Thin compatibility layer for the Gas Leak scenario HUD.
///
/// The scenario no longer builds a Canvas: timer, objectives, hints, feedback and end-of-mission
/// panel come from the shared HUD in <c>SCN_Core</c>. The methods below keep the existing
/// gameplay scripts unchanged and route their calls to <see cref="HUDService"/>.
/// </summary>
[DisallowMultipleComponent]
public class HUDScenario : MonoBehaviour
{
    [Tooltip("Mostra un avviso con il titolo dell'esito finale.")]
    [SerializeField] bool mostraAvvisoEsito = true;

    Coroutine _routineSuggerimento;
    bool _esitoVisibile;

    // ---------------------------------------------------------------- API

    /// <summary>The shared HUD owns the mission timer; kept for source compatibility.</summary>
    public void ImpostaTempo(float secondi)
    {
    }

    /// <summary>Hearts are shown by the shared objective/phase panel; kept for compatibility.</summary>
    public void ImpostaCuori(int attuali, int massimo)
    {
    }

    /// <summary>Applies the difficulty label through the shared phase channel.</summary>
    public void ImpostaDifficolta(ImpostazioniDifficolta impostazioni)
    {
        if (impostazioni != null && !string.IsNullOrWhiteSpace(impostazioni.nome))
        {
            HUDService.SetPhase("hud.phase.action");
        }
    }

    /// <summary>Shows a contextual hint for a few seconds.</summary>
    public void MostraSuggerimento(string testo, float durata)
    {
        if (string.IsNullOrWhiteSpace(testo))
        {
            return;
        }

        if (_routineSuggerimento != null)
        {
            StopCoroutine(_routineSuggerimento);
        }

        HUDService.SetHint(testo);
        _routineSuggerimento = StartCoroutine(CicloSuggerimento(Mathf.Max(1f, durata)));
    }

    IEnumerator CicloSuggerimento(float durata)
    {
        yield return new WaitForSeconds(durata);
        HUDService.SetHint(string.Empty);
        _routineSuggerimento = null;
    }

    /// <summary>Shows the guided-level thumbs feedback (-1 down, 0 intermediate, 1 up).</summary>
    public void MostraFeedbackPollice(int livello)
    {
        switch (livello)
        {
            case 1:
                HUDService.ShowActionFeedback("Pollice su", FeedbackKind.Positive);
                break;
            case 0:
                HUDService.ShowActionFeedback("Così così", FeedbackKind.Neutral);
                break;
            default:
                HUDService.ShowActionFeedback("Pollice giù", FeedbackKind.Negative);
                break;
        }
    }

    /// <summary>
    /// The shared end-of-mission screen is driven by GasLeakScenario through EndMission;
    /// this only keeps the old call site working.
    /// </summary>
    public void MostraEsito(EsitoScenario esito, int punteggio, IReadOnlyList<VoceRecap> recap)
    {
        NascondiEsito();

        if (mostraAvvisoEsito)
        {
            HUDService.ShowEventAlert(TitoloEsito(esito));
        }

        _esitoVisibile = true;
    }

    /// <summary>The shared HUD is dismissed by the mission flow.</summary>
    public void NascondiEsito()
    {
        _esitoVisibile = false;
    }

    public bool EsitoVisibile => _esitoVisibile;

    static string TitoloEsito(EsitoScenario esito)
    {
        switch (esito)
        {
            case EsitoScenario.SuccessoCompleto:
                return "Successo completo";
            case EsitoScenario.SuccessoParziale:
                return "Successo parziale";
            case EsitoScenario.FallimentoTempo:
                return "Tempo scaduto";
            case EsitoScenario.FallimentoIntossicazione:
                return "Intossicazione";
            default:
                return "Esito";
        }
    }
}
