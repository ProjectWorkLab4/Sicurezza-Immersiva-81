using System.Collections.Generic;
using SI81.UI;
using UnityEngine;

/// <summary>
/// Gas Leak entry point for the shared scenario/UI flow (step 6 of the rig migration).
///
/// It lives on <c>MNG_Scenario</c>, owns the <c>SO_Mission_GasLeak</c> reference and the spawn
/// point, and bridges the existing <see cref="GestoreScenarioGasLeak"/> gameplay brain to
/// <see cref="ScenarioController"/>. No HUD or Canvas is created here.
/// </summary>
[DisallowMultipleComponent]
public sealed class GasLeakScenario : ScenarioController
{
    [SerializeField] GestoreScenarioGasLeak gestore;

    void Awake()
    {
        if (gestore == null)
        {
            gestore = FindFirstObjectByType<GestoreScenarioGasLeak>();
        }
    }

    void OnEnable()
    {
        if (gestore != null)
        {
            gestore.Terminato += HandleTerminato;
        }
    }

    void OnDisable()
    {
        if (gestore != null)
        {
            gestore.Terminato -= HandleTerminato;
        }
    }

    protected override void OnScenarioStarted()
    {
        if (gestore != null)
        {
            gestore.ImpostaDifficolta(FromSharedLevel(Level));
            gestore.Avvia();
        }

        SetPhase(PHASE_SCAN);
        Subtitle("Caposquadra", "Più persone con gli stessi sintomi? Potrebbe essere monossido di carbonio.", 6f);
        SetHint("Riconosci i sintomi e usa il rilevatore CO");
    }

    protected override void OnTimerExpired()
    {
        if (gestore != null)
        {
            gestore.Termina(EsitoScenario.FallimentoTempo);
        }
        else
        {
            base.OnTimerExpired();
        }
    }

    void HandleTerminato(EsitoScenario esito)
    {
        if (!IsRunning)
        {
            return;
        }

        var lines = new List<string>();

        if (gestore != null && gestore.Recap != null)
        {
            foreach (VoceRecap voce in gestore.Recap)
            {
                if (voce == null || string.IsNullOrWhiteSpace(voce.testo))
                {
                    continue;
                }

                lines.Add((voce.corretta ? DebriefLinePrefix.GOOD : DebriefLinePrefix.BAD) + voce.testo);
            }
        }

        if (lines.Count == 0)
        {
            lines.Add(DebriefLinePrefix.BAD + "Non è stato possibile mettere in sicurezza l'area.");
        }

        EndMission(ToOutcome(esito), gestore != null ? gestore.Punteggio : -1, null, lines);
    }

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
