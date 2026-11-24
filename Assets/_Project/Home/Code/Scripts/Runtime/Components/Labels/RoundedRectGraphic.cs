using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// Rettangolo arrotondato disegnato come mesh (senza sprite): pieno oppure solo contorno interno. Serve per i piccoli
    /// elementi grafici delle etichette: le barre dell'icona livello (raggio 1, contorno 1,5 se vuote) e il contorno della
    /// pista di LBL_DimensionBar (contorno 1, raggio pillola). Le forme grandi usano invece gli sprite TEX_ con
    /// <see cref="RoundedPanelGraphic"/>. Il colore è <see cref="Graphic.color"/> (dal tema, mai nel prefab).
    /// </summary>
    [AddComponentMenu("SI81/UI/Rounded Rect Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public class RoundedRectGraphic : MaskableGraphic
    {
        /// <summary>Segmenti per ogni angolo arrotondato.</summary>
        private const int CORNER_SEGMENTS = 6;

        [Tooltip("Raggio degli angoli in unità Canvas (limitato a metà del lato corto: valori grandi = pillola).")]
        [SerializeField] private float radius = 1f;
        [Tooltip("Spessore del contorno interno in unità Canvas. 0 = forma piena.")]
        [SerializeField] private float stroke;

        /// <summary>Raggio degli angoli.</summary>
        public float Radius
        {
            get => radius;
            set
            {
                if (Mathf.Approximately(radius, value)) return;
                radius = value;
                SetVerticesDirty();
            }
        }

        /// <summary>Spessore del contorno (0 = pieno).</summary>
        public float Stroke
        {
            get => stroke;
            set
            {
                if (Mathf.Approximately(stroke, value)) return;
                stroke = value;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Costruisce la mesh: ventaglio di triangoli se pieno, anello tra contorno esterno e interno se con contorno.
        /// </summary>
        /// <param name="vh">Helper dei vertici di uGUI.</param>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f) return;

            float maxRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
            float outerRadius = Mathf.Clamp(radius, 0f, maxRadius);
            bool ring = stroke > 0f && stroke < maxRadius;

            var outer = new List<Vector2>();
            AddPath(outer, rect, outerRadius);

            if (!ring)
            {
                // Pieno: ventaglio dal centro.
                vh.AddVert(rect.center, color, Vector2.zero);
                for (int i = 0; i < outer.Count; i++) vh.AddVert(outer[i], color, Vector2.zero);
                for (int i = 0; i < outer.Count; i++) vh.AddTriangle(0, 1 + i, 1 + (i + 1) % outer.Count);
                return;
            }

            // Contorno: stesso numero di punti sul rettangolo rimpicciolito dello spessore.
            Rect innerRect = new Rect(rect.x + stroke, rect.y + stroke, rect.width - 2f * stroke, rect.height - 2f * stroke);
            var inner = new List<Vector2>();
            AddPath(inner, innerRect, Mathf.Max(0f, outerRadius - stroke));
            for (int i = 0; i < outer.Count; i++) vh.AddVert(outer[i], color, Vector2.zero);
            for (int i = 0; i < inner.Count; i++) vh.AddVert(inner[i], color, Vector2.zero);
            int n = outer.Count;
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                vh.AddTriangle(i, next, n + next);
                vh.AddTriangle(i, n + next, n + i);
            }
        }

        /// <summary>
        /// Aggiunge i punti del contorno di un rettangolo arrotondato, in senso antiorario partendo dall'angolo in alto a destra.
        /// </summary>
        /// <param name="points">Elenco a cui aggiungere i punti.</param>
        /// <param name="rect">Rettangolo.</param>
        /// <param name="cornerRadius">Raggio degli angoli (già limitato).</param>
        private static void AddPath(List<Vector2> points, Rect rect, float cornerRadius)
        {
            // Centri degli angoli: alto-destra, alto-sinistra, basso-sinistra, basso-destra.
            Vector2[] centers =
            {
                new Vector2(rect.xMax - cornerRadius, rect.yMax - cornerRadius),
                new Vector2(rect.xMin + cornerRadius, rect.yMax - cornerRadius),
                new Vector2(rect.xMin + cornerRadius, rect.yMin + cornerRadius),
                new Vector2(rect.xMax - cornerRadius, rect.yMin + cornerRadius)
            };
            for (int corner = 0; corner < 4; corner++)
            {
                for (int k = 0; k <= CORNER_SEGMENTS; k++)
                {
                    float angle = (corner + k / (float)CORNER_SEGMENTS) * Mathf.PI * 0.5f;
                    points.Add(centers[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * cornerRadius);
                }
            }
        }
    }
}
