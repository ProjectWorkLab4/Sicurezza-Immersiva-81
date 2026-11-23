using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// Raycaster unico dei Canvas World Space SI81 (sostituisce TrackedDeviceRaycaster + GraphicRaycaster).
    /// Il TrackedDeviceRaycaster di Input System colpisce TUTTE le grafiche del Canvas, anche quelle con
    /// raycastTarget = false (ombre, riflessi, icone e testi dentro i pulsanti), e ordina solo per profondità:
    /// un'ombra decorativa poteva così "rubare" il raggio del controller (es. l'ombra di PANEL_Menu sopra PANEL_Side).
    /// Questo componente:
    /// - per i puntatori tracciati (controller) interseca il raggio con il piano di ogni grafica RAYCASTABILE
    ///   (raycastTarget = true, attiva, non nascosta), rispetta CanvasGroup.blocksRaycasts e i filtri (Graphic.Raycast),
    ///   scarta le grafiche viste da dietro e oltre la distanza massima, e restituisce i risultati con profondità e
    ///   distanza (l'EventSystem li ordina: profondità nello stesso Canvas, distanza tra Canvas diversi);
    /// - per mouse/penna (Editor) usa il comportamento standard di GraphicRaycaster.
    /// Funziona perché, senza nessun TrackedDeviceRaycaster in scena, InputSystemUIInputModule passa anche i puntatori
    /// tracciati a EventSystem.RaycastAll. Quindi: nessun TrackedDeviceRaycaster nei Canvas SI81 (lo tolgono i builder).
    /// L'interazione (CanvasGroup.interactable) resta gestita dai Selectable, come in uGUI standard.
    /// </summary>
    [AddComponentMenu("SI81/UI/UI World Raycaster")]
    [DisallowMultipleComponent]
    public class UIWorldRaycaster : GraphicRaycaster
    {
        [Tooltip("Distanza massima del raggio del controller in metri (il builder la imposta da SO_UITheme.Pointer.RayMaxLength).")]
        [SerializeField] private float maxDistance = 4f;

        // Canvas dello stesso GameObject (in cache).
        private Canvas ownCanvas;
        // Angoli del RectTransform riusati (nessuna allocazione per raycast).
        private readonly Vector3[] corners = new Vector3[4];
        // Risultati temporanei riusati.
        private readonly List<RaycastResult> hits = new List<RaycastResult>();

        /// <summary>Distanza massima del raggio del controller, in metri.</summary>
        public float MaxDistance
        {
            get => maxDistance;
            set => maxDistance = value;
        }

        /// <summary>Canvas del raycaster.</summary>
        private Canvas OwnCanvas
        {
            get
            {
                if (ownCanvas == null) ownCanvas = GetComponent<Canvas>();
                return ownCanvas;
            }
        }

        /// <summary>
        /// Raycast di uGUI: per i controller usa il raggio 3D filtrato; per mouse/penna il GraphicRaycaster standard.
        /// </summary>
        /// <param name="eventData">Dati del puntatore (ExtendedPointerEventData per Input System).</param>
        /// <param name="resultAppendList">Lista a cui aggiungere i risultati.</param>
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (eventData is ExtendedPointerEventData extended && extended.pointerType == UIPointerType.Tracked)
            {
                var ray = new Ray(extended.trackedDevicePosition, extended.trackedDeviceOrientation * Vector3.forward);
                RaycastTracked(ray, resultAppendList);
                return;
            }
            base.Raycast(eventData, resultAppendList);
        }

        /// <summary>
        /// Interseca un raggio nel mondo con le grafiche raycastabili del Canvas e aggiunge i risultati validi
        /// (ordinati per profondità decrescente). Pubblico per le prove (raggio di test) e per altri puntatori SI81.
        /// </summary>
        /// <param name="ray">Raggio in coordinate mondo.</param>
        /// <param name="resultAppendList">Lista a cui aggiungere i risultati.</param>
        public void RaycastTracked(Ray ray, List<RaycastResult> resultAppendList)
        {
            Canvas canvas = OwnCanvas;
            Camera camera = eventCamera;
            if (canvas == null || camera == null) return;

            hits.Clear();
            IList<Graphic> graphics = GraphicRegistry.GetRaycastableGraphicsForCanvas(canvas);
            for (int i = 0; i < graphics.Count; i++)
            {
                Graphic graphic = graphics[i];
                // Solo grafiche disegnate, attive e che vogliono i raggi (decorazioni escluse).
                if (graphic == null || !graphic.raycastTarget || graphic.depth == -1 || graphic.canvasRenderer.cull) continue;
                if (!IntersectRect(graphic.rectTransform, ray, out Vector3 worldPosition, out float distance)) continue;
                if (distance > maxDistance) continue;
                // Grafica vista da dietro: ignorata (come TrackedDeviceRaycaster con ignoreReversedGraphics).
                if (ignoreReversedGraphics && Vector3.Dot(ray.direction, graphic.transform.forward) <= 0f) continue;

                Vector2 screenPosition = camera.WorldToScreenPoint(worldPosition);
                // Filtri di uGUI: CanvasGroup.blocksRaycasts, soglia alfa delle Image, maschere.
                if (!graphic.Raycast(screenPosition, camera)) continue;

                hits.Add(new RaycastResult
                {
                    gameObject = graphic.gameObject,
                    module = this,
                    distance = distance,
                    worldPosition = worldPosition,
                    worldNormal = -graphic.transform.forward,
                    screenPosition = screenPosition,
                    depth = graphic.depth,
                    sortingLayer = canvas.sortingLayerID,
                    sortingOrder = canvas.sortingOrder,
                    displayIndex = canvas.targetDisplay
                });
            }

            // Profondità decrescente (la grafica disegnata sopra per prima), a parità la più vicina.
            hits.Sort((a, b) => a.depth != b.depth ? b.depth.CompareTo(a.depth) : a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Count; i++)
            {
                RaycastResult result = hits[i];
                result.index = resultAppendList.Count;
                resultAppendList.Add(result);
            }
            hits.Clear();
        }

        /// <summary>
        /// Intersezione tra un raggio e il rettangolo (anche ruotato) di un RectTransform.
        /// </summary>
        /// <param name="rect">RectTransform della grafica.</param>
        /// <param name="ray">Raggio in coordinate mondo.</param>
        /// <param name="worldPosition">Punto colpito.</param>
        /// <param name="distance">Distanza dall'origine del raggio.</param>
        /// <returns>true se il raggio colpisce il rettangolo davanti all'origine.</returns>
        private bool IntersectRect(RectTransform rect, Ray ray, out Vector3 worldPosition, out float distance)
        {
            rect.GetWorldCorners(corners);
            var plane = new Plane(corners[0], corners[1], corners[2]);
            worldPosition = Vector3.zero;
            distance = 0f;
            if (!plane.Raycast(ray, out float enter)) return false;

            Vector3 point = ray.GetPoint(enter);
            // Coordinate del punto lungo i lati inferiore (0→3) e sinistro (0→1), normalizzate 0–1.
            Vector3 bottom = corners[3] - corners[0];
            Vector3 left = corners[1] - corners[0];
            float u = Vector3.Dot(point - corners[0], bottom) / Mathf.Max(bottom.sqrMagnitude, 1e-12f);
            float v = Vector3.Dot(point - corners[0], left) / Mathf.Max(left.sqrMagnitude, 1e-12f);
            if (u < 0f || u > 1f || v < 0f || v > 1f) return false;

            worldPosition = point;
            distance = enter;
            return true;
        }

        /// <summary>
        /// Imposta la distanza massima. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(float newMaxDistance)
        {
            maxDistance = newMaxDistance;
        }
    }
}
