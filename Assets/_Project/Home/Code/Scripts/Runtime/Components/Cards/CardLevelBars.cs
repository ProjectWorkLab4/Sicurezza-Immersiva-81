using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Icona 20 x 20 a 3 barre dei livelli completati di BTN_ScenarioCard (stessa geometria di LBL_LevelBadge):
    /// barre piene = livelli completati, vuote = solo contorno. Solo disegno, colori passati da chi la usa. Nessun raycast.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class CardLevelBars : MonoBehaviour
    {
        /// <summary>Barre (x, y dall'alto, larghezza, altezza) nel riquadro 20 x 20 (Figma UI_LevelBadge IMG_LevelBars).</summary>
        private static readonly Rect[] BAR_RECTS =
        {
            new Rect(2f, 12f, 4f, 7f),
            new Rect(8f, 7f, 4f, 12f),
            new Rect(14f, 2f, 4f, 17f)
        };
        /// <summary>Raggio degli angoli delle barre (Figma).</summary>
        private const float BAR_RADIUS = 1f;

        [Tooltip("Le 3 barre (RoundedRectGraphic), assegnate dal builder.")]
        [SerializeField] private RoundedRectGraphic[] bars = new RoundedRectGraphic[DifficultyStyle.BAR_COUNT];

        /// <summary>
        /// Posiziona le barre nel riquadro 20 x 20. Chiamato dal builder.
        /// </summary>
        public void Layout()
        {
            for (int i = 0; i < bars.Length && i < BAR_RECTS.Length; i++)
            {
                if (bars[i] == null) continue;
                CardTextUtil.PlaceTopLeft(bars[i].rectTransform, BAR_RECTS[i].x, BAR_RECTS[i].y, BAR_RECTS[i].width, BAR_RECTS[i].height);
                bars[i].Radius = BAR_RADIUS;
                bars[i].raycastTarget = false;
            }
        }

        /// <summary>
        /// Aggiorna le barre: le prime <paramref name="filled"/> piene, le altre solo contorno.
        /// </summary>
        /// <param name="filled">Barre piene (0–3, limitato).</param>
        /// <param name="onColor">Colore delle barre piene.</param>
        /// <param name="offColor">Colore del contorno delle barre vuote.</param>
        /// <param name="stroke">Spessore del contorno delle barre vuote (SO_DifficultyStyle.BarStroke).</param>
        public void Set(int filled, Color onColor, Color offColor, float stroke)
        {
            filled = Mathf.Clamp(filled, 0, DifficultyStyle.BAR_COUNT);
            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i] == null) continue;
                bool isFilled = i < filled;
                bars[i].Stroke = isFilled ? 0f : stroke;
                bars[i].color = isFilled ? onColor : offColor;
            }
        }

        /// <summary>
        /// Collega le barre. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(RoundedRectGraphic[] newBars)
        {
            bars = newBars;
        }
    }
}
