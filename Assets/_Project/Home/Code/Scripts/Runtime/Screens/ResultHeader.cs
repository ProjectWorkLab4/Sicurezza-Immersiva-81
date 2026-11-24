using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Intestazione comune di PANEL_Debrief e PANEL_ScoreReplay: titolo dello scenario nel PANEL_Menu ("Debrief · 01 · Fire Starter"),
    /// LBL_LevelBadge Dark e LBL_Outcome sul vetro (pillola bianca + icona colorata + testo nero, CLAUDE.md §4.9) nel posto a destra.
    /// Uso: <c>header.Show(result, mission)</c> a ogni comparsa della schermata. Nessun testo fisso: chiave del titolo e tabella testi sono parametri.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Screens/Result Header")]
    public class ResultHeader : MonoBehaviour
    {
        [Header("Dati")]
        [Tooltip("Tabella testi SO_UIStrings_It.")]
        [SerializeField] private UIStrings strings;
        [Tooltip("Chiave del titolo con segnaposto {0} = titolo dello scenario (ui.debrief.title / ui.score.title).")]
        [SerializeField] private string titleKey = "ui.debrief.title";
        [Tooltip("Spazio tra badge del livello ed esito (space.m).")]
        [SerializeField] private float gap = 12f;

        [Header("Riferimenti (assegnati dal builder)")]
        [SerializeField] private MenuPanel menu;
        [SerializeField] private LevelBadge levelBadge;
        [SerializeField] private OutcomeLabel outcomeLabel;

        /// <summary>
        /// Scrive titolo, livello ed esito e dispone i due elementi a destra (esito all'estremo, badge alla sua sinistra).
        /// </summary>
        /// <param name="result">Risultato mostrato (non null).</param>
        /// <param name="mission">Scenario (può essere null: titolo vuoto).</param>
        public void Show(MissionResult result, MissionDefinition mission)
        {
            if (menu != null && strings != null)
                menu.SetTitleText(strings.Format(titleKey, mission != null ? mission.GetFullTitle() : string.Empty));
            if (outcomeLabel != null) outcomeLabel.SetOutcome(result.Outcome);
            if (levelBadge != null) levelBadge.SetLevel(result.Level);
            Layout();
        }

        /// <summary>
        /// Posiziona esito (pivot a destra, centrato in verticale) e badge del livello alla sua sinistra.
        /// </summary>
        private void Layout()
        {
            float x = 0f;
            if (outcomeLabel != null)
            {
                var rect = (RectTransform)outcomeLabel.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                x = rect.rect.width + gap;
            }
            if (levelBadge != null)
            {
                var rect = (RectTransform)levelBadge.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
                rect.anchoredPosition = new Vector2(-x, 0f);
            }
        }

        /// <summary>
        /// Imposta i riferimenti. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(UIStrings newStrings, string newTitleKey, float newGap, MenuPanel newMenu, LevelBadge newBadge, OutcomeLabel newOutcome)
        {
            strings = newStrings;
            titleKey = newTitleKey;
            gap = newGap;
            menu = newMenu;
            levelBadge = newBadge;
            outcomeLabel = newOutcome;
        }
    }
}
