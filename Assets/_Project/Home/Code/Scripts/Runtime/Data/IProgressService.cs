using System;
using System.Collections.Generic;

namespace SI81.UI
{
    /// <summary>
    /// Servizio dei progressi del giocatore (risultati, migliori, totali, badge). Le schermate lo leggono da
    /// <see cref="UIServices.Progress"/> e si aggiornano sull'evento <see cref="Changed"/>.
    /// </summary>
    public interface IProgressService
    {
        /// <summary>Miglior risultato di uno scenario (mai null: valori vuoti se mai giocato).</summary>
        /// <param name="missionId">Numero dello scenario.</param>
        MissionBest GetBest(string missionId);
        /// <summary>Risultati di uno scenario, dal più vecchio al più recente.</summary>
        /// <param name="missionId">Numero dello scenario.</param>
        IReadOnlyList<MissionResult> GetResults(string missionId);
        /// <summary>Tutti i risultati, dal più vecchio al più recente.</summary>
        IReadOnlyList<MissionResult> AllResults { get; }
        /// <summary>Totali per Riepilogo e Profilo.</summary>
        /// <param name="catalog">Catalogo (per contare gli scenari giocabili).</param>
        ProgressTotals GetTotals(MissionCatalog catalog);
        /// <summary>Badge con stato di sblocco.</summary>
        IReadOnlyList<BadgeInfo> GetBadges();
        /// <summary>Registra un risultato e lancia <see cref="Changed"/>.</summary>
        /// <param name="r">Risultato da salvare.</param>
        void RecordResult(MissionResult r);
        /// <summary>Lanciato quando i dati cambiano.</summary>
        event Action Changed;
    }
}
