using System.Collections.Generic;
using UnityEngine;

namespace SI81.UI
{
    /// <summary>Tipo di etichetta nel mondo che un <see cref="LabelAnchor"/> richiede.</summary>
    public enum LabelKind
    {
        /// <summary>HUD_RiskLabel: rischio sull'oggetto (solo livelli con ShowRiskLabels, max 3 insieme).</summary>
        Risk = 0,
        /// <summary>HUD_InteractionPrompt: cosa si può fare con l'oggetto (tutti i livelli).</summary>
        Interaction = 1,
        /// <summary>HUD_ContextHelp: aiuto sull'oggetto (solo livelli con ShowContextHelp).</summary>
        ContextHelp = 2
    }

    /// <summary>
    /// Ancora di un'etichetta nel mondo: il team di scenario la mette su un oggetto di scena (un solo componente, nessuna UI da
    /// costruire a mano). <see cref="WorldLabelManager"/> mostra l'istanza giusta di HUD_RiskLabel / HUD_InteractionPrompt /
    /// HUD_ContextHelp quando l'ancora è tra 0,75 e 4 m dalla testa e il livello lo consente (LevelConfig).
    /// Testo: scrivere direttamente il testo (dato dello scenario) OPPURE una chiave di SO_UIStrings_It.
    /// Uso: aggiungere il componente, scegliere il tipo, scrivere titolo e dettaglio, regolare l'offset sopra l'oggetto.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Label Anchor")]
    public class LabelAnchor : MonoBehaviour
    {
        // Registro statico di tutte le ancore attive (nessuna ricerca in scena a runtime, nessuna allocazione).
        private static readonly List<LabelAnchor> ACTIVE = new List<LabelAnchor>(32);

        [Header("Tipo")]
        [Tooltip("Risk = rischio (Guided/Operational), Interaction = azione possibile, ContextHelp = aiuto sull'oggetto (Guided).")]
        [SerializeField] private LabelKind kind = LabelKind.Risk;
        [Tooltip("true = l'etichetta compare solo se il livello evidenzia gli oggetti importanti (LevelConfig.ShowHighlights).")]
        [SerializeField] private bool requiresHighlight;

        [Header("Testo (dato dello scenario)")]
        [Tooltip("Titolo, es. \"Ciabatta surriscaldata\" (Risk), \"Estintore CO2\" (Interaction, ContextHelp). Usato se la chiave è vuota.")]
        [SerializeField] private string title;
        [Tooltip("Chiave SO_UIStrings_It del titolo (alternativa al testo scritto).")]
        [SerializeField] private string titleKey;
        [Tooltip("Dettaglio: istruzione (Risk) o testo di aiuto su 2 righe (ContextHelp). Non usato da Interaction.")]
        [SerializeField] private string detail;
        [Tooltip("Chiave SO_UIStrings_It del dettaglio (alternativa al testo scritto).")]
        [SerializeField] private string detailKey;

        [Header("Interaction")]
        [Tooltip("Chiave del tasto mostrato nella pillola (vuoto = hud.prompt.button.grip).")]
        [SerializeField] private string buttonKey;
        [Tooltip("Chiave del verbo dell'azione (vuoto = hud.prompt.action.grab).")]
        [SerializeField] private string actionKey;

        [Header("Posizione e icona")]
        [Tooltip("Spostamento rispetto all'oggetto, nello spazio locale dell'oggetto (metri). Es. (0, 0.3, 0) sopra l'oggetto.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.3f, 0f);
        [Tooltip("Icona ICN_ dell'etichetta (vuoto = icona predefinita del tipo: avviso, mano, aiuto).")]
        [SerializeField] private Sprite icon;

        // true se lo scenario ha nascosto l'etichetta (es. oggetto afferrato).
        private bool suppressed;

        /// <summary>Tutte le ancore attive in scena (sola lettura, ordine non significativo).</summary>
        public static IReadOnlyList<LabelAnchor> All => ACTIVE;

        /// <summary>Tipo di etichetta richiesto.</summary>
        public LabelKind Kind => kind;
        /// <summary>true se serve un livello con evidenziazione.</summary>
        public bool RequiresHighlight => requiresHighlight;
        /// <summary>Titolo scritto a mano (vuoto se si usa la chiave).</summary>
        public string Title => title;
        /// <summary>Chiave del titolo (può essere vuota).</summary>
        public string TitleKey => titleKey;
        /// <summary>Dettaglio scritto a mano.</summary>
        public string Detail => detail;
        /// <summary>Chiave del dettaglio (può essere vuota).</summary>
        public string DetailKey => detailKey;
        /// <summary>Chiave del tasto (Interaction), vuota = predefinita.</summary>
        public string ButtonKey => buttonKey;
        /// <summary>Chiave del verbo (Interaction), vuota = predefinita.</summary>
        public string ActionKey => actionKey;
        /// <summary>Icona scelta dallo scenario (può essere null).</summary>
        public Sprite Icon => icon;

        /// <summary>
        /// true = l'etichetta è nascosta dallo scenario (es. oggetto afferrato: l'invito ad afferrare sparisce).
        /// </summary>
        public bool Suppressed
        {
            get => suppressed;
            set => suppressed = value;
        }

        /// <summary>Punto del mondo a cui si aggancia l'etichetta (posizione dell'oggetto + offset locale).</summary>
        public Vector3 WorldPoint => transform.TransformPoint(offset);

        /// <summary>
        /// Registra l'ancora quando si attiva.
        /// </summary>
        private void OnEnable()
        {
            if (!ACTIVE.Contains(this)) ACTIVE.Add(this);
        }

        /// <summary>
        /// Toglie l'ancora dal registro quando si disattiva o viene distrutta.
        /// </summary>
        private void OnDisable()
        {
            ACTIVE.Remove(this);
        }

        /// <summary>
        /// Azzera il registro all'avvio di ogni sessione di gioco (utile con il Domain Reload disattivato).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            ACTIVE.Clear();
        }

        /// <summary>
        /// Testo del titolo: dalla chiave se presente, altrimenti il testo scritto.
        /// </summary>
        /// <param name="strings">Tabella testi (può essere null se si usa solo testo scritto).</param>
        /// <returns>Il titolo da mostrare (mai null).</returns>
        public string ResolveTitle(UIStrings strings)
        {
            return Resolve(strings, titleKey, title);
        }

        /// <summary>
        /// Testo del dettaglio: dalla chiave se presente, altrimenti il testo scritto.
        /// </summary>
        /// <param name="strings">Tabella testi (può essere null).</param>
        /// <returns>Il dettaglio da mostrare (mai null).</returns>
        public string ResolveDetail(UIStrings strings)
        {
            return Resolve(strings, detailKey, detail);
        }

        /// <summary>
        /// Sceglie tra chiave (se esiste la tabella) e testo scritto.
        /// </summary>
        private static string Resolve(UIStrings strings, string key, string text)
        {
            if (strings != null && !string.IsNullOrEmpty(key)) return strings.Get(key);
            return text ?? string.Empty;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Disegna l'ancora in Scene view: pallino nel punto di aggancio e linea fino all'oggetto.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = kind == LabelKind.Risk ? Color.yellow : (kind == LabelKind.Interaction ? Color.green : Color.cyan);
            Vector3 point = WorldPoint;
            Gizmos.DrawLine(transform.position, point);
            Gizmos.DrawWireSphere(point, 0.04f);
        }
#endif
    }
}
