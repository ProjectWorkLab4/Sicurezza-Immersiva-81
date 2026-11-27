using System;
using System.Collections.Generic;
using System.Linq;
using SI81.UI;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// Tabella delle frasi del debrief (specifica §11.1), letta da un JSON: i testi si modificano senza toccare il codice.
    /// Converte il Registro delle scelte in righe di debrief con prefisso "+ ", "- ", "! ".
    /// </summary>
    [Serializable]
    public class DebriefPhraseTable
    {
        /// <summary>Una frase modello.</summary>
        [Serializable]
        public class Phrase
        {
            /// <summary>ID azione (LG-01...).</summary>
            public string id;
            /// <summary>Frase con segnaposto {t}, {s}, {tipo}, {scelta}, {esito}.</summary>
            public string phrase;
            /// <summary>Riga "Cosa potevi fare" (può essere vuota).</summary>
            public string whatYouCouldDo;
        }

        /// <summary>Frasi della tabella.</summary>
        public List<Phrase> phrases = new List<Phrase>();
        /// <summary>Percorso corretto, un passo per riga (sempre mostrato in fondo).</summary>
        public List<string> correctPath = new List<string>();

        /// <summary>Numero massimo di righe del debrief prima di filtrare.</summary>
        public const int MAX_LINES = 10;

        /// <summary>Legge la tabella dal JSON.</summary>
        /// <param name="json">Asset JSON.</param>
        /// <returns>La tabella, vuota se il JSON manca.</returns>
        public static DebriefPhraseTable Load(TextAsset json)
        {
            if (json == null) return new DebriefPhraseTable();
            return JsonUtility.FromJson<DebriefPhraseTable>(json.text) ?? new DebriefPhraseTable();
        }

        /// <summary>Cerca la frase per ID.</summary>
        /// <param name="id">ID azione.</param>
        public Phrase Find(string id) => phrases.FirstOrDefault(p => p.id == id);

        /// <summary>
        /// Costruisce le righe del debrief ordinate per tempo. Oltre 10 righe: prima tutti gli errori critici e i mancati,
        /// poi le scelte rischiose e infine le corrette, fino a 10.
        /// </summary>
        /// <param name="log">Registro delle scelte.</param>
        /// <param name="includeWhatYouCouldDo">true = aggiunge "Cosa potevi fare" alle righe non corrette.</param>
        /// <returns>Righe con prefisso DebriefLinePrefix.</returns>
        public List<string> BuildLines(ChoiceLog log, bool includeWhatYouCouldDo)
        {
            var selected = log.Entries.ToList();
            if (selected.Count > MAX_LINES)
            {
                selected = selected
                    .OrderBy(e => e.Outcome == ChoiceOutcome.CriticalError || e.Outcome == ChoiceOutcome.Missed ? 0 : e.Outcome == ChoiceOutcome.Risky ? 1 : 2)
                    .ThenBy(e => e.Time)
                    .Take(MAX_LINES)
                    .ToList();
            }

            var lines = new List<string>();
            foreach (var e in selected.OrderBy(e => e.Time))
            {
                var p = Find(e.ActionId);
                string text = p != null ? Format(p.phrase, e) : $"{e.ActionId} al {ChoiceLog.FormatTime(e.Time)}";
                if (includeWhatYouCouldDo && p != null && e.Outcome != ChoiceOutcome.Correct && !string.IsNullOrEmpty(p.whatYouCouldDo))
                    text += " Cosa potevi fare: " + p.whatYouCouldDo;
                lines.Add(Prefix(e.Outcome) + text);
            }
            return lines;
        }

        /// <summary>Prefisso della riga per esito.</summary>
        private static string Prefix(ChoiceOutcome outcome) => outcome switch
        {
            ChoiceOutcome.Correct => DebriefLinePrefix.GOOD,
            ChoiceOutcome.CriticalError => DebriefLinePrefix.CRITICAL,
            _ => DebriefLinePrefix.BAD
        };

        /// <summary>Sostituisce i segnaposto con tempo e contesto della riga.</summary>
        private static string Format(string template, ChoiceLogEntry e)
        {
            string text = template.Replace("{t}", "minuto " + ChoiceLog.FormatTime(e.Time));
            foreach (var kv in e.Context) text = text.Replace("{" + kv.Key + "}", kv.Value);
            return text.Replace("{s}", "?").Replace("{tipo}", "?").Replace("{scelta}", "?").Replace("{esito}", "?");
        }
    }
}
