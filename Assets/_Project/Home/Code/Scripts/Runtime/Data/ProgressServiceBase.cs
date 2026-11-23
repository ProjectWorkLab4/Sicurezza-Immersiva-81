using System;
using System.Collections.Generic;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Logica comune dei servizi dei progressi: tiene la lista dei risultati e calcola migliori, totali e badge.
    /// Le sottoclassi decidono solo DOVE si salva (memoria per la sandbox, PlayerPrefs per il gioco): nessun codice duplicato.
    /// </summary>
    public abstract class ProgressServiceBase : IProgressService
    {

        // Catalogo (badge e conteggio scenari).
        private readonly MissionCatalog catalog;
        // Risultati, dal più vecchio al più recente.
        private readonly List<MissionResult> results = new List<MissionResult>();
        // Vuoto restituito quando uno scenario non ha risultati.
        private static readonly List<MissionResult> EMPTY = new List<MissionResult>();

        /// <inheritdoc/>
        public event Action Changed;

        /// <summary>
        /// Crea il servizio.
        /// </summary>
        /// <param name="newCatalog">Catalogo con le definizioni dei badge (può essere null: nessun badge).</param>
        protected ProgressServiceBase(MissionCatalog newCatalog)
        {
            catalog = newCatalog;
        }

        /// <inheritdoc/>
        public IReadOnlyList<MissionResult> AllResults => results;

        /// <summary>Catalogo associato.</summary>
        protected MissionCatalog Catalog => catalog;

        /// <inheritdoc/>
        public IReadOnlyList<MissionResult> GetResults(string missionId)
        {
            var list = new List<MissionResult>();
            for (int i = 0; i < results.Count; i++)
                if (results[i].MissionId == missionId) list.Add(results[i]);
            return list.Count == 0 ? EMPTY : list;
        }

        /// <inheritdoc/>
        public MissionBest GetBest(string missionId)
        {
            var best = new MissionBest { MissionId = missionId };
            var levels = new HashSet<DifficultyLevel>();
            long lastTime = long.MinValue;
            for (int i = 0; i < results.Count; i++)
            {
                MissionResult r = results[i];
                if (r.MissionId != missionId) continue;
                if (!best.BestScore.HasValue || r.SafetyScore > best.BestScore.Value) best.BestScore = r.SafetyScore;
                if (!best.BestStars.HasValue || r.Stars > best.BestStars.Value) best.BestStars = r.Stars;
                if (r.Outcome != OutcomeType.Negative) levels.Add(r.Level);
                if (r.TimestampUtc >= lastTime)
                {
                    lastTime = r.TimestampUtc;
                    best.LastPlayedLevel = r.Level;
                }
            }
            best.LevelsCompleted = levels.Count;
            return best;
        }

        /// <inheritdoc/>
        public ProgressTotals GetTotals(MissionCatalog cat)
        {
            var totals = new ProgressTotals { Sessions = results.Count };
            var bestPerMission = new Dictionary<string, int>();
            for (int i = 0; i < results.Count; i++)
            {
                MissionResult r = results[i];
                if (r.Outcome == OutcomeType.Positive) totals.Positive++;
                else if (r.Outcome == OutcomeType.Partial) totals.Partial++;
                else totals.Negative++;
                if (!bestPerMission.TryGetValue(r.MissionId, out int score) || r.SafetyScore > score) bestPerMission[r.MissionId] = r.SafetyScore;
            }
            int sum = 0;
            int played = 0;
            MissionCatalog source = cat != null ? cat : catalog;
            if (source != null)
            {
                foreach (MissionDefinition m in source.Playable)
                {
                    totals.ScenariosTotal++;
                    if (bestPerMission.TryGetValue(m.Id, out int best))
                    {
                        played++;
                        sum += best;
                    }
                }
            }
            else
            {
                foreach (KeyValuePair<string, int> pair in bestPerMission) { played++; sum += pair.Value; }
                totals.ScenariosTotal = played;
            }
            totals.ScenariosPlayed = played;
            totals.AverageScore = played > 0 ? Mathf.RoundToInt(sum / (float)played) : 0;
            return totals;
        }

        /// <inheritdoc/>
        public IReadOnlyList<BadgeInfo> GetBadges()
        {
            var list = new List<BadgeInfo>();
            if (catalog == null || catalog.Badges == null) return list;
            for (int i = 0; i < catalog.Badges.Count; i++)
            {
                BadgeDefinition d = catalog.Badges[i];
                if (d == null) continue;
                list.Add(new BadgeInfo { Id = d.Id, Title = d.Title, Description = d.Description, Icon = d.Icon, Unlocked = IsUnlocked(d.Rule) });
            }
            return list;
        }

        /// <inheritdoc/>
        public void RecordResult(MissionResult r)
        {
            if (r == null) return;
            ScoreRules.Normalize(r);
            results.Add(r);
            OnResultsChanged();
            Changed?.Invoke();
        }

        /// <summary>
        /// Sostituisce tutti i risultati (caricamento o dati di prova) SENZA lanciare l'evento.
        /// </summary>
        /// <param name="newResults">Risultati dal più vecchio al più recente.</param>
        protected void SetResults(IEnumerable<MissionResult> newResults)
        {
            results.Clear();
            if (newResults == null) return;
            foreach (MissionResult r in newResults)
            {
                if (r == null) continue;
                ScoreRules.Normalize(r);
                results.Add(r);
            }
            results.Sort((a, b) => a.TimestampUtc.CompareTo(b.TimestampUtc));
        }

        /// <summary>
        /// Chiamato dopo ogni nuovo risultato: le sottoclassi lo usano per salvare.
        /// </summary>
        protected virtual void OnResultsChanged() { }

        /// <summary>
        /// Lancia l'evento Changed (per le sottoclassi che modificano i dati da sole).
        /// </summary>
        protected void RaiseChanged()
        {
            Changed?.Invoke();
        }

        /// <summary>
        /// Valuta una regola di badge sui risultati correnti.
        /// </summary>
        /// <param name="rule">Regola.</param>
        /// <returns>true se la regola è soddisfatta.</returns>
        private bool IsUnlocked(BadgeRule rule)
        {
            switch (rule)
            {
                case BadgeRule.FirstMission: return results.Count >= 1;
                case BadgeRule.ThreeScenarios: return DistinctMissions() >= 3;
                case BadgeRule.FourStars: return Any(r => r.Stars >= 4);
                case BadgeRule.FivePositives: return Count(r => r.Outcome == OutcomeType.Positive) >= 5;
                case BadgeRule.OperationalCompleted: return Any(r => r.Level == DifficultyLevel.Operational && r.Outcome != OutcomeType.Negative);
                case BadgeRule.CrisisCompleted: return Any(r => r.Level == DifficultyLevel.Crisis && r.Outcome != OutcomeType.Negative);
                case BadgeRule.FiveStars: return Any(r => r.Stars >= 5);
                case BadgeRule.FastRun: return Any(r => r.Outcome == OutcomeType.Positive && r.DurationSeconds > 0f && r.DurationSeconds < ScoreRules.FastRunSeconds());
                case BadgeRule.TenSessions: return results.Count >= 10;
                case BadgeRule.AllLevelsOneScenario: return AnyScenarioWithAllLevels();
                case BadgeRule.PerfectDimension: return Any(r => HasDimensionAtLeast(r, 95));
                case BadgeRule.AllScenarios: return catalog != null && AllScenariosPlayed();
                default: return false;
            }
        }

        /// <summary>Numero di scenari diversi giocati.</summary>
        private int DistinctMissions()
        {
            var ids = new HashSet<string>();
            for (int i = 0; i < results.Count; i++) ids.Add(results[i].MissionId);
            return ids.Count;
        }

        /// <summary>true se almeno un risultato soddisfa la condizione.</summary>
        private bool Any(Func<MissionResult, bool> predicate)
        {
            return Count(predicate) > 0;
        }

        /// <summary>Conta i risultati che soddisfano la condizione.</summary>
        private int Count(Func<MissionResult, bool> predicate)
        {
            int n = 0;
            for (int i = 0; i < results.Count; i++) if (predicate(results[i])) n++;
            return n;
        }

        /// <summary>true se almeno una delle dimensioni del risultato raggiunge la soglia.</summary>
        private static bool HasDimensionAtLeast(MissionResult r, int threshold)
        {
            if (r.Dimensions == null) return false;
            for (int i = 0; i < r.Dimensions.Length; i++) if (r.Dimensions[i] >= threshold) return true;
            return false;
        }

        /// <summary>true se uno scenario ha tutti e 3 i livelli conclusi (esito non negativo).</summary>
        private bool AnyScenarioWithAllLevels()
        {
            var ids = new HashSet<string>();
            for (int i = 0; i < results.Count; i++) ids.Add(results[i].MissionId);
            foreach (string id in ids) if (GetBest(id).LevelsCompleted >= 3) return true;
            return false;
        }

        /// <summary>true se ogni scenario giocabile del catalogo ha almeno un risultato.</summary>
        private bool AllScenariosPlayed()
        {
            int total = 0;
            foreach (MissionDefinition m in catalog.Playable)
            {
                total++;
                if (GetResults(m.Id).Count == 0) return false;
            }
            return total > 0;
        }
    }
}
