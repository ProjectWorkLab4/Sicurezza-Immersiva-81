using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// Grafica uGUI di un pannello arrotondato: disegna uno sprite 9-slice (es. TEX_PanelRounded, bianco) colorato con
    /// <see cref="Graphic.color"/> e, se attivo, il riflesso diagonale dei pannelli vetro (glassPanel.sheen) cotto nei
    /// colori dei vertici. Il riflesso è già composto sopra il riempimento in ogni vertice ("over", come in Figma),
    /// quindi riempimento + riflesso costano UN solo strato, nessuna texture in più e nessuna maschera: la forma
    /// arrotondata viene dall'alfa dello sprite. Con il riflesso attivo il centro viene suddiviso in celle per
    /// seguire il gradiente; la mesh si ricostruisce solo quando cambiano dimensione, colore o parametri.
    /// </summary>
    [AddComponentMenu("SI81/UI/Rounded Panel Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public class RoundedPanelGraphic : MaskableGraphic
    {
        /// <summary>Lato massimo di una cella della mesh con il riflesso attivo, in unità Canvas (errore del gradiente invisibile).</summary>
        public const float SHEEN_CELL_SIZE = 32f;

        [Tooltip("Sprite 9-slice bianco con gli angoli arrotondati (es. TEX_PanelRounded). La forma è nel canale alfa.")]
        [SerializeField] private Sprite sprite;
        [Tooltip("Attiva il riflesso diagonale dei pannelli vetro (valori dal tema, impostati da chi usa il componente).")]
        [SerializeField] private bool sheenEnabled;
        [Tooltip("Parametri del riflesso (copiati da SO_UITheme.Sheen: non modificarli a mano).")]
        [SerializeField] private UITheme.GlassSheen sheen = new UITheme.GlassSheen();

        /// <summary>Sprite 9-slice arrotondato.</summary>
        public Sprite Sprite
        {
            get => sprite;
            set
            {
                if (sprite == value) return;
                sprite = value;
                SetAllDirty();
            }
        }

        /// <summary>true se il riflesso diagonale è attivo.</summary>
        public bool SheenEnabled => sheenEnabled;

        /// <summary>Texture disegnata: quella dello sprite, o bianca se manca.</summary>
        public override Texture mainTexture => sprite != null ? sprite.texture : s_WhiteTexture;

        /// <summary>
        /// Attiva il riflesso con i valori del tema (copia: modifiche successive al tema richiedono una nuova chiamata).
        /// </summary>
        /// <param name="themeSheen">Riflesso di SO_UITheme.</param>
        public void SetSheen(UITheme.GlassSheen themeSheen)
        {
            sheenEnabled = themeSheen != null;
            if (themeSheen != null)
            {
                sheen = new UITheme.GlassSheen
                {
                    StartColor = themeSheen.StartColor,
                    EndColor = themeSheen.EndColor,
                    EndPosition = themeSheen.EndPosition,
                    HandleStart = themeSheen.HandleStart,
                    HandleEnd = themeSheen.HandleEnd
                };
            }
            SetVerticesDirty();
        }

        /// <summary>
        /// Disattiva il riflesso (pannello a tinta unita, mesh 9-slice minima).
        /// </summary>
        public void DisableSheen()
        {
            sheenEnabled = false;
            SetVerticesDirty();
        }

        /// <summary>
        /// Costruisce la mesh: griglia 9-slice (bordi dello sprite in unità Canvas, ridotti se il pannello è più piccolo)
        /// con il centro suddiviso quando il riflesso è attivo; colore di ogni vertice = riflesso composto sul riempimento.
        /// </summary>
        /// <param name="vh">Helper dei vertici di uGUI.</param>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f) return;

            // UV esterni/interni e bordi (sinistra, basso, destra, alto) dello sprite.
            Vector4 outer = new Vector4(0f, 0f, 1f, 1f);
            Vector4 inner = outer;
            Vector4 border = Vector4.zero;
            if (sprite != null)
            {
                outer = DataUtility.GetOuterUV(sprite);
                inner = DataUtility.GetInnerUV(sprite);
                float referencePpu = canvas != null ? canvas.referencePixelsPerUnit : PanelSprites.CANVAS_REFERENCE_PIXELS_PER_UNIT;
                float pixelsPerUnit = sprite.pixelsPerUnit / referencePpu;
                border = sprite.border / pixelsPerUnit;
            }

            // Pannello più piccolo dei bordi: si riducono in proporzione (come Image.Type.Sliced).
            float scaleX = border.x + border.z > rect.width ? rect.width / (border.x + border.z) : 1f;
            float scaleY = border.y + border.w > rect.height ? rect.height / (border.y + border.w) : 1f;
            border.x *= scaleX;
            border.z *= scaleX;
            border.y *= scaleY;
            border.w *= scaleY;

            int columns = BuildAxis(rect.xMin, rect.xMax, border.x, border.z, outer.x, inner.x, inner.z, outer.z, out float[] xs, out float[] us);
            int rows = BuildAxis(rect.yMin, rect.yMax, border.y, border.w, outer.y, inner.y, inner.w, outer.w, out float[] ys, out float[] vs);

            Color fill = color;
            for (int j = 0; j <= rows; j++)
            {
                // v dall'alto (0) al basso (1), come nelle maniglie Figma.
                float vNorm = (rect.yMax - ys[j]) / rect.height;
                for (int i = 0; i <= columns; i++)
                {
                    float uNorm = (xs[i] - rect.xMin) / rect.width;
                    Color c = sheenEnabled ? Over(sheen.Evaluate(uNorm, vNorm), fill) : fill;
                    vh.AddVert(new Vector3(xs[i], ys[j]), c, new Vector2(us[i], vs[j]));
                }
            }

            int stride = columns + 1;
            for (int j = 0; j < rows; j++)
            {
                for (int i = 0; i < columns; i++)
                {
                    int a = j * stride + i;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;
                    vh.AddTriangle(a, c, d);
                    vh.AddTriangle(d, b, a);
                }
            }
        }

        /// <summary>
        /// Calcola le coordinate (posizione e UV) delle linee della griglia su un asse: bordo iniziale, centro
        /// (suddiviso se il riflesso è attivo), bordo finale.
        /// </summary>
        /// <returns>Numero di celle sull'asse.</returns>
        private int BuildAxis(float min, float max, float borderStart, float borderEnd,
            float uvOuterStart, float uvInnerStart, float uvInnerEnd, float uvOuterEnd, out float[] positions, out float[] uvs)
        {
            float centerStart = min + borderStart;
            float centerEnd = max - borderEnd;
            int centerCells = sheenEnabled ? Mathf.Max(1, Mathf.CeilToInt((centerEnd - centerStart) / SHEEN_CELL_SIZE)) : 1;
            int cells = centerCells + 2;
            positions = new float[cells + 1];
            uvs = new float[cells + 1];

            positions[0] = min;
            uvs[0] = uvOuterStart;
            for (int k = 0; k <= centerCells; k++)
            {
                float t = k / (float)centerCells;
                positions[k + 1] = Mathf.Lerp(centerStart, centerEnd, t);
                uvs[k + 1] = Mathf.Lerp(uvInnerStart, uvInnerEnd, t);
            }
            positions[cells] = max;
            uvs[cells] = uvOuterEnd;
            return cells;
        }

        /// <summary>
        /// Composizione "over" (alpha non premoltiplicato): colore <paramref name="top"/> sopra <paramref name="bottom"/>.
        /// Disegnare il risultato equivale a disegnare i due strati uno sopra l'altro.
        /// </summary>
        private static Color Over(Color top, Color bottom)
        {
            float alpha = top.a + bottom.a * (1f - top.a);
            if (alpha <= 0f) return new Color(bottom.r, bottom.g, bottom.b, 0f);
            float weightBottom = bottom.a * (1f - top.a);
            return new Color(
                (top.r * top.a + bottom.r * weightBottom) / alpha,
                (top.g * top.a + bottom.g * weightBottom) / alpha,
                (top.b * top.a + bottom.b * weightBottom) / alpha,
                alpha);
        }
    }
}
