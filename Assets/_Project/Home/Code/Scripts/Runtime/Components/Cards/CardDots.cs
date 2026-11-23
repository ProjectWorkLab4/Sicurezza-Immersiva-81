using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Fila di pallini della complessità di BTN_ScenarioCard (5 pallini 8 x 8, gap 3 = 52 x 8): i primi N sono pieni.
    /// Solo disegno: i colori li passa il componente che la usa (token level.bar.* del tema). Nessun raycast.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class CardDots : MonoBehaviour
    {
        /// <summary>Numero di pallini (complessità massima, brief: 1–5).</summary>
        public const int COUNT = 5;

        [Tooltip("I 5 pallini (RoundedRectGraphic), assegnati dal builder.")]
        [SerializeField] private RoundedRectGraphic[] dots = new RoundedRectGraphic[COUNT];

        /// <summary>
        /// Posiziona i pallini in fila (origine in alto a sinistra). Chiamato dal builder.
        /// </summary>
        /// <param name="size">Diametro di un pallino.</param>
        /// <param name="gap">Spazio tra due pallini.</param>
        public void Layout(float size, float gap)
        {
            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i] == null) continue;
                CardTextUtil.PlaceTopLeft(dots[i].rectTransform, i * (size + gap), 0f, size, size);
                dots[i].Radius = size * 0.5f;
                dots[i].Stroke = 0f;
                dots[i].raycastTarget = false;
            }
        }

        /// <summary>
        /// Colora i pallini: i primi <paramref name="filled"/> con il colore "pieno", gli altri con quello "vuoto".
        /// </summary>
        /// <param name="filled">Pallini pieni (0–5, limitato).</param>
        /// <param name="onColor">Colore dei pallini pieni.</param>
        /// <param name="offColor">Colore dei pallini vuoti.</param>
        public void Set(int filled, Color onColor, Color offColor)
        {
            filled = Mathf.Clamp(filled, 0, COUNT);
            for (int i = 0; i < dots.Length; i++)
                if (dots[i] != null) dots[i].color = i < filled ? onColor : offColor;
        }

        /// <summary>
        /// Collega i pallini. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(RoundedRectGraphic[] newDots)
        {
            dots = newDots;
        }
    }

}
