using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Tabella dei testi della UI comune (asset SO_UIStrings_It). Ogni testo ha una chiave
    /// minuscola puntata in inglese (es. "ui.button.start_scenario"). Nessun testo va scritto nei prefab.
    /// I testi specifici di uno scenario (avvisi, obiettivi, decisioni, badge) NON stanno qui ma in SO_Mission_*.
    /// Generato da "SI81/UI/Build Theme" a partire da Editor/Source/strings_it.json, che è l'UNICA fonte di verità:
    /// per cambiare un testo modificare solo strings_it.json e rieseguire SI81/UI/Build Theme; le modifiche fatte
    /// direttamente a questo asset (Inspector) vengono perse alla build successiva.
    /// </summary>
    [CreateAssetMenu(menuName = "SI81/UI/Strings", fileName = "SO_UIStrings_It")]
    public class UIStrings : ScriptableObject
    {
        /// <summary>Formato obbligatorio delle chiavi: minuscolo, snake_case, almeno due segmenti separati da punto.</summary>
        public const string KEY_PATTERN = @"^[a-z0-9_]+(\.[a-z0-9_]+)+$";
        /// <summary>Prefisso del segnaposto visibile per una chiave mancante (solo ASCII, presente nel font).</summary>
        public const string MISSING_PREFIX = "[?";
        /// <summary>Suffisso del segnaposto visibile per una chiave mancante.</summary>
        public const string MISSING_SUFFIX = "]";

        private static readonly Regex KEY_REGEX = new Regex(KEY_PATTERN, RegexOptions.CultureInvariant);

        /// <summary>Una riga della tabella: chiave e testo italiano.</summary>
        [Serializable]
        public class Entry
        {
            /// <summary>Chiave puntata, es. "ui.nav.home".</summary>
            public string Key;
            /// <summary>Testo italiano. Può contenere segnaposto {0}, {1} per <see cref="Format"/>.</summary>
            [TextArea(1, 4)] public string Text;
        }

        [Header("Generato da Editor/Source/strings_it.json: modificare SOLO quel file, poi SI81/UI/Build Theme")]
        [Tooltip("Codice lingua della tabella.")]
        [SerializeField] private string languageCode = "it";
        [Tooltip("Non modificare qui: modificare Editor/Source/strings_it.json e rieseguire SI81/UI/Build Theme. Le modifiche dirette all'asset vengono perse.")]
        [SerializeField] private List<Entry> entries = new List<Entry>();

        [NonSerialized] private Dictionary<string, string> lookup;
        [NonSerialized] private HashSet<string> warnedKeys;

        /// <summary>Codice lingua (es. "it").</summary>
        public string LanguageCode => languageCode;
        /// <summary>Tutte le righe (lettura).</summary>
        public IReadOnlyList<Entry> Entries => entries;
        /// <summary>Numero di chiavi.</summary>
        public int Count => entries.Count;

        /// <summary>
        /// Controlla che una chiave rispetti il formato <see cref="KEY_PATTERN"/>.
        /// </summary>
        /// <param name="key">Chiave da controllare.</param>
        /// <returns>true se la chiave è valida.</returns>
        public static bool IsValidKey(string key)
        {
            return !string.IsNullOrEmpty(key) && KEY_REGEX.IsMatch(key);
        }

        /// <summary>
        /// Restituisce il testo di una chiave. Se manca restituisce un segnaposto visibile "[?chiave]"
        /// e scrive UN solo avviso per chiave.
        /// </summary>
        /// <param name="key">Chiave del testo.</param>
        /// <returns>Testo italiano o segnaposto.</returns>
        public string Get(string key)
        {
            EnsureLookup();
            if (key != null && lookup.TryGetValue(key, out string text)) return text;
            WarnOnce(key, $"Chiave di testo mancante: \"{key}\".");
            return MISSING_PREFIX + key + MISSING_SUFFIX;
        }

        /// <summary>
        /// Indica se la chiave esiste nella tabella.
        /// </summary>
        /// <param name="key">Chiave da cercare.</param>
        /// <returns>true se presente.</returns>
        public bool Contains(string key)
        {
            EnsureLookup();
            return key != null && lookup.ContainsKey(key);
        }

        /// <summary>
        /// Restituisce il testo di una chiave con i segnaposto {0}, {1}... sostituiti dagli argomenti
        /// (string.Format). Se il formato non è valido restituisce il testo grezzo e scrive un avviso.
        /// </summary>
        /// <param name="key">Chiave del testo.</param>
        /// <param name="args">Valori da inserire.</param>
        /// <returns>Testo formattato.</returns>
        public string Format(string key, params object[] args)
        {
            string pattern = Get(key);
            if (args == null || args.Length == 0 || !Contains(key)) return pattern;
            try
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, pattern, args);
            }
            catch (FormatException)
            {
                WarnOnce(key, $"Formato non valido per \"{key}\" con {args.Length} argomenti: \"{pattern}\".");
                return pattern;
            }
        }

        /// <summary>
        /// Controlla l'intera tabella: formato chiavi, duplicati, testi vuoti.
        /// </summary>
        /// <returns>Elenco dei problemi in italiano (vuoto se tutto è corretto).</returns>
        public List<string> Validate()
        {
            var problems = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                if (e == null)
                {
                    problems.Add($"Riga {i}: vuota.");
                    continue;
                }
                if (!IsValidKey(e.Key)) problems.Add($"Riga {i}: chiave non valida \"{e.Key}\" (formato {KEY_PATTERN}).");
                else if (!seen.Add(e.Key)) problems.Add($"Riga {i}: chiave duplicata \"{e.Key}\".");
                if (string.IsNullOrEmpty(e.Text)) problems.Add($"Riga {i}: testo vuoto per \"{e.Key}\".");
            }
            return problems;
        }

        /// <summary>
        /// Sostituisce tutte le righe. Usato solo dal builder "SI81/UI/Build Theme".
        /// </summary>
        internal void SetData(string language, List<Entry> newEntries)
        {
            languageCode = language;
            entries = newEntries;
            lookup = null;
        }

        /// <summary>
        /// Svuota la cache quando l'asset viene caricato.
        /// </summary>
        private void OnEnable()
        {
            lookup = null;
            warnedKeys = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// In Editor: dopo una modifica dall'Inspector controlla formato e duplicati e segnala i problemi.
        /// </summary>
        private void OnValidate()
        {
            lookup = null;
            List<string> problems = Validate();
            if (problems.Count > 0)
                Debug.LogWarning($"[{name}] {problems.Count} problemi nella tabella testi:\n" + string.Join("\n", problems), this);
        }
#endif

        /// <summary>
        /// Costruisce il dizionario chiave → testo se non esiste ancora (la prima chiave duplicata vince).
        /// </summary>
        private void EnsureLookup()
        {
            if (lookup != null) return;
            lookup = new Dictionary<string, string>(entries.Count, StringComparer.Ordinal);
            foreach (Entry e in entries)
                if (e != null && !string.IsNullOrEmpty(e.Key) && !lookup.ContainsKey(e.Key)) lookup.Add(e.Key, e.Text ?? string.Empty);
        }

        /// <summary>
        /// Scrive un avviso una sola volta per chiave.
        /// </summary>
        /// <param name="key">Chiave interessata.</param>
        /// <param name="message">Messaggio in italiano.</param>
        private void WarnOnce(string key, string message)
        {
            if (warnedKeys == null) warnedKeys = new HashSet<string>(StringComparer.Ordinal);
            if (!warnedKeys.Add(key ?? "<null>")) return;
            Debug.LogWarning($"[{name}] {message}", this);
        }
    }
}
