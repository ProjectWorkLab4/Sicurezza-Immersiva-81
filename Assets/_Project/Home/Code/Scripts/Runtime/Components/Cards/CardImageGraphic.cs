using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// Immagine della card con i soli angoli SUPERIORI arrotondati (la miniatura di BTN_ScenarioCard sta in cima alla card
    /// bianca, Figma "clip"). Disegna una miniatura (sprite riempito "cover") oppure, se manca, il gradiente segnaposto
    /// lineare a 147,28 gradi con il colore finale al 71,4% (Figma 63:268). Una sola mesh, nessuna maschera né stencil
    /// (costo minimo su Quest 3). Il colore del Graphic fa da tinta (alpha compreso). Decorativa: raycastTarget = false.
    /// </summary>
    [AddComponentMenu("SI81/UI/Card Image Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public class CardImageGraphic : MaskableGraphic
    {
        /// <summary>Angolo del gradiente segnaposto, in gradi CSS (Figma 147,28).</summary>
        public const float GRADIENT_ANGLE = 147.28f;
        /// <summary>Posizione del colore finale lungo il gradiente (Figma 71,4%).</summary>
        public const float GRADIENT_END_STOP = 0.714f;
        /// <summary>Segmenti di ciascun angolo arrotondato.</summary>
        private const int CORNER_SEGMENTS = 6;

        [Tooltip("Miniatura dello scenario. Vuota = gradiente segnaposto.")]
        [SerializeField] private Sprite image;
        [Tooltip("Colore iniziale del gradiente segnaposto.")]
        [SerializeField] private Color gradientStart = Color.white;
        [Tooltip("Colore finale del gradiente segnaposto.")]
        [SerializeField] private Color gradientEnd = Color.white;
        [Tooltip("Raggio degli angoli superiori, in unità Canvas (radius.card).")]
        [SerializeField] private float topRadius = 12f;

        /// <summary>Texture disegnata: quella della miniatura, o bianca per il gradiente.</summary>
        public override Texture mainTexture => image != null ? image.texture : s_WhiteTexture;

        /// <summary>Miniatura corrente (null = gradiente).</summary>
        public Sprite Image => image;

        /// <summary>
        /// Imposta la miniatura (o null per il gradiente) e i colori del gradiente segnaposto.
        /// </summary>
        /// <param name="newImage">Miniatura; null = gradiente.</param>
        /// <param name="start">Colore iniziale del gradiente.</param>
        /// <param name="end">Colore finale del gradiente.</param>
        public void SetContent(Sprite newImage, Color start, Color end)
        {
            bool textureChanged = image != newImage;
            image = newImage;
            gradientStart = start;
            gradientEnd = end;
            if (textureChanged) SetAllDirty();
            else SetVerticesDirty();
        }

        /// <summary>
        /// Imposta il raggio degli angoli superiori (usato dal builder).
        /// </summary>
        /// <param name="radius">Raggio in unità Canvas.</param>
        public void SetTopRadius(float radius)
        {
            topRadius = radius;
            SetVerticesDirty();
        }

        /// <summary>
        /// Costruisce la mesh: poligono con i due angoli in alto arrotondati, a ventaglio dal centro.
        /// Colore dei vertici = tinta × (bianco con miniatura, gradiente senza).
        /// </summary>
        /// <param name="vh">Helper dei vertici di uGUI.</param>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f) return;

            float radius = Mathf.Clamp(topRadius, 0f, Mathf.Min(rect.width * 0.5f, rect.height));

            // Contorno in senso antiorario: basso-sinistra, basso-destra, arco alto-destra, arco alto-sinistra.
            var outline = new List<Vector2>(2 + (CORNER_SEGMENTS + 1) * 2)
            {
                new Vector2(rect.xMin, rect.yMin),
                new Vector2(rect.xMax, rect.yMin)
            };
            AddArc(outline, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f);
            AddArc(outline, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f);

            // Il centro del ventaglio è il centro del rettangolo.
            AddVertex(vh, rect.center, rect);
            for (int i = 0; i < outline.Count; i++) AddVertex(vh, outline[i], rect);
            for (int i = 0; i < outline.Count; i++)
            {
                int next = (i + 1) % outline.Count;
                vh.AddTriangle(0, 1 + i, 1 + next);
            }
        }

        /// <summary>
        /// Aggiunge i punti di un arco di cerchio (angoli in gradi, antiorario).
        /// </summary>
        private static void AddArc(List<Vector2> points, Vector2 center, float radius, float fromDegrees, float toDegrees)
        {
            for (int i = 0; i <= CORNER_SEGMENTS; i++)
            {
                float angle = Mathf.Lerp(fromDegrees, toDegrees, i / (float)CORNER_SEGMENTS) * Mathf.Deg2Rad;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        /// <summary>
        /// Aggiunge un vertice con UV ("cover" della miniatura) e colore (gradiente o tinta) nella posizione data.
        /// </summary>
        private void AddVertex(VertexHelper vh, Vector2 position, Rect rect)
        {
            float n = (position.x - rect.xMin) / rect.width;
            float m = (position.y - rect.yMin) / rect.height;
            Vector2 uv = Vector2.zero;
            Color vertexColor;
            if (image != null)
            {
                // "Cover": si ritaglia la miniatura al centro per riempire il rettangolo senza deformarla.
                float spriteAspect = image.rect.width / Mathf.Max(1f, image.rect.height);
                float rectAspect = rect.width / rect.height;
                float visibleU = rectAspect > spriteAspect ? 1f : rectAspect / spriteAspect;
                float visibleV = rectAspect > spriteAspect ? spriteAspect / rectAspect : 1f;
                Vector4 outer = DataUtility.GetOuterUV(image);
                uv = new Vector2(Mathf.Lerp(outer.x, outer.z, 0.5f + (n - 0.5f) * visibleU),
                    Mathf.Lerp(outer.y, outer.w, 0.5f + (m - 0.5f) * visibleV));
                vertexColor = color;
            }
            else
            {
                vertexColor = color * EvaluateGradient(position - rect.center, rect.size);
            }
            vh.AddVert(new Vector3(position.x, position.y), vertexColor, uv);
        }

        /// <summary>
        /// Colore del gradiente lineare CSS (angolo <see cref="GRADIENT_ANGLE"/>) in un punto relativo al centro.
        /// </summary>
        private Color EvaluateGradient(Vector2 fromCenter, Vector2 size)
        {
            float angle = GRADIENT_ANGLE * Mathf.Deg2Rad;
            // Direzione CSS (0° = in alto, oraria) in coordinate Unity con Y verso l'alto.
            Vector2 direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            float length = Mathf.Abs(size.x * Mathf.Sin(angle)) + Mathf.Abs(size.y * Mathf.Cos(angle));
            float t = Vector2.Dot(fromCenter, direction) / Mathf.Max(0.0001f, length) + 0.5f;
            return Color.Lerp(gradientStart, gradientEnd, Mathf.Clamp01(t / GRADIENT_END_STOP));
        }
    }
}
