using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Regole comuni degli sprite generati TEX_ (angoli arrotondati e ombre 9-slice) condivise tra il builder Editor
    /// (SI81/UI/Build Sprites) e i componenti runtime: densità in pixel, estensione della sfocatura e calcolo dei
    /// bordi 9-slice dell'ombra. Tenerle in un solo posto garantisce che lo sprite generato e il RectTransform
    /// che lo disegna usino gli stessi numeri.
    /// </summary>
    public static class PanelSprites
    {
        /// <summary>Pixel di texture per unità Canvas degli sprite arrotondati (bordi netti: ~1,8 px display per unità a 1,5 m).</summary>
        public const float ROUNDED_PIXELS_PER_UNIT = 2f;
        /// <summary>Pixel di texture per unità Canvas degli sprite ombra (sfocati: bastano pochi pixel).</summary>
        public const float SHADOW_PIXELS_PER_UNIT = 0.5f;
        /// <summary>Estensione dell'ombra oltre la forma, in deviazioni standard (oltre 2,5 σ resta &lt; 0,2% di alpha).</summary>
        public const float SHADOW_EXTENT_SIGMAS = 2.5f;
        /// <summary>Rientro della "bucatura" dell'ombra sotto il pannello, in unità Canvas (evita un filo chiaro sul bordo).</summary>
        public const float SHADOW_KNOCKOUT_INSET = 1f;
        /// <summary>Pixel per unità di riferimento del Canvas (Canvas.referencePixelsPerUnit, default Unity senza CanvasScaler).</summary>
        public const float CANVAS_REFERENCE_PIXELS_PER_UNIT = 100f;

        /// <summary>Misure in pixel di uno sprite ombra 9-slice.</summary>
        public struct ShadowLayout
        {
            /// <summary>Spazio tra il bordo della texture e la forma (uguale sui 4 lati), in pixel.</summary>
            public int PaddingPx;
            /// <summary>Bordo 9-slice sinistro in pixel.</summary>
            public int Left;
            /// <summary>Bordo 9-slice destro in pixel.</summary>
            public int Right;
            /// <summary>Bordo 9-slice superiore in pixel.</summary>
            public int Top;
            /// <summary>Bordo 9-slice inferiore in pixel.</summary>
            public int Bottom;
            /// <summary>Larghezza della texture in pixel (multiplo di 4).</summary>
            public int Width;
            /// <summary>Altezza della texture in pixel (multiplo di 4).</summary>
            public int Height;
            /// <summary>Spazio tra il bordo dello sprite e la forma, in unità Canvas (da usare come espansione del RectTransform).</summary>
            public float PaddingUnits => PaddingPx / SHADOW_PIXELS_PER_UNIT;
        }

        /// <summary>
        /// Converte il "blur" di un effetto Figma nella deviazione standard della gaussiana (Figma/CSS box-shadow: σ = blur / 2).
        /// </summary>
        /// <param name="figmaBlur">Valore blur Figma in unità Canvas.</param>
        /// <returns>Deviazione standard in unità Canvas.</returns>
        public static float BlurToSigma(float figmaBlur)
        {
            return figmaBlur * 0.5f;
        }

        /// <summary>
        /// Calcola le misure dello sprite ombra 9-slice per una forma arrotondata e un'ombra del tema.
        /// I bordi sono abbastanza larghi perché il profilo sia costante nelle fasce stirate (forma + sfocatura + spostamento).
        /// Lo "spread" Figma non è gestito (vale 0 in tutti i token attuali).
        /// </summary>
        /// <param name="radius">Raggio degli angoli della forma, in unità Canvas.</param>
        /// <param name="shadow">Ombra del tema (offset con Y verso l'alto, blur Figma).</param>
        /// <returns>Misure in pixel della texture e dei bordi.</returns>
        public static ShadowLayout GetShadowLayout(float radius, UITheme.ShadowToken shadow)
        {
            float ppu = SHADOW_PIXELS_PER_UNIT;
            int extent = Mathf.CeilToInt(BlurToSigma(shadow.Blur) * SHADOW_EXTENT_SIGMAS * ppu);
            int radiusPx = Mathf.CeilToInt(radius * ppu);
            int offsetX = Mathf.CeilToInt(Mathf.Abs(shadow.Offset.x) * ppu) * (shadow.Offset.x < 0f ? -1 : 1);
            int offsetY = Mathf.CeilToInt(Mathf.Abs(shadow.Offset.y) * ppu) * (shadow.Offset.y < 0f ? -1 : 1);
            int padding = extent + Mathf.Max(Mathf.Abs(offsetX), Mathf.Abs(offsetY));

            var layout = new ShadowLayout
            {
                PaddingPx = padding,
                // Un'ombra spostata verso destra/basso allarga i bordi sinistro/superiore (Y Unity verso l'alto).
                Left = padding + radiusPx + Mathf.Max(extent + offsetX, 0),
                Right = padding + radiusPx + Mathf.Max(extent - offsetX, 0),
                Top = padding + radiusPx + Mathf.Max(extent - offsetY, 0),
                Bottom = padding + radiusPx + Mathf.Max(extent + offsetY, 0)
            };
            // Almeno 2 pixel stirabili al centro, dimensione arrotondata a multipli di 4 (blocchi di compressione).
            layout.Width = RoundUp4(layout.Left + layout.Right + 2);
            layout.Height = RoundUp4(layout.Top + layout.Bottom + 2);
            return layout;
        }

        /// <summary>
        /// Variante "compatta" per pannelli bassi (es. PANEL_Side, 161): la texture contiene l'ombra di una forma alta
        /// esattamente <paramref name="minShapeHeight"/> e la riga stirata è il centro della forma spostata, così i bordi
        /// superiore + inferiore (in unità) stanno in un pannello alto almeno <paramref name="minShapeHeight"/> senza
        /// compressione. Esatta all'altezza minima; più in alto si stira il centro di quell'ombra (fasce laterali al più
        /// ~5% più chiare con blur 60 e minimo 120). In orizzontale i bordi restano quelli di <see cref="GetShadowLayout(float, UITheme.ShadowToken)"/>.
        /// L'espansione del RectTransform (<see cref="ShadowLayout.PaddingUnits"/>) è identica alla versione normale.
        /// </summary>
        /// <param name="radius">Raggio degli angoli della forma, in unità Canvas.</param>
        /// <param name="shadow">Ombra del tema (offset con Y verso l'alto, blur Figma).</param>
        /// <param name="minShapeHeight">Altezza minima del pannello in unità Canvas (token size.shadow.compact-min-height).</param>
        /// <returns>Misure in pixel della texture e dei bordi.</returns>
        public static ShadowLayout GetShadowLayout(float radius, UITheme.ShadowToken shadow, float minShapeHeight)
        {
            ShadowLayout layout = GetShadowLayout(radius, shadow);
            if (minShapeHeight <= 0f) return layout;
            float ppu = SHADOW_PIXELS_PER_UNIT;
            int offsetY = Mathf.CeilToInt(Mathf.Abs(shadow.Offset.y) * ppu) * (shadow.Offset.y < 0f ? -1 : 1);
            int radiusPx = Mathf.CeilToInt(radius * ppu);

            // Altezza della forma in pixel: almeno due raggi (angoli completi), al massimo quella dell'altezza minima.
            int shapePx = Mathf.Max(Mathf.FloorToInt(minShapeHeight * ppu), radiusPx * 2 + 2);
            layout.Height = RoundUp4(layout.PaddingPx * 2 + shapePx);
            // Riga stirata (1 px) al centro della forma SPOSTATA (Y della texture verso l'alto): lì il profilo delle
            // fasce laterali è al massimo. Top/Bottom contati dal bordo della texture.
            int stretchRow = Mathf.Clamp(layout.Height / 2 + offsetY, layout.PaddingPx + radiusPx + 1, layout.Height - layout.PaddingPx - radiusPx - 2);
            layout.Bottom = stretchRow;
            layout.Top = layout.Height - stretchRow - 1;
            return layout;
        }

        /// <summary>
        /// Lato in pixel della texture di uno sprite arrotondato: potenza di 2 che contiene due bordi e un centro stirabile.
        /// </summary>
        /// <param name="radius">Raggio degli angoli in unità Canvas.</param>
        /// <param name="borderPx">Bordo 9-slice risultante, in pixel.</param>
        /// <returns>Lato della texture quadrata in pixel.</returns>
        public static int GetRoundedTextureSize(float radius, out int borderPx)
        {
            borderPx = Mathf.Max(1, Mathf.CeilToInt(radius * ROUNDED_PIXELS_PER_UNIT));
            return Mathf.NextPowerOfTwo(borderPx * 2 + 2);
        }

        /// <summary>
        /// Arrotonda per eccesso al multiplo di 4.
        /// </summary>
        private static int RoundUp4(int value)
        {
            return (value + 3) / 4 * 4;
        }
    }
}
