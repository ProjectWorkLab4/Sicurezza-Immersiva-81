using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// Grafica invisibile che riceve i raggi: un quadrato trasparente che copre il suo RectTransform. Serve da area di presa
    /// perché UIWorldRaycaster scarta le grafiche senza mesh o già "tagliate" perché trasparenti (cullTransparentMesh = false).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class SettingsHitArea : MaskableGraphic
    {
        /// <summary>
        /// Disegna un solo quadrato trasparente sull'intero rettangolo; disattiva il taglio delle mesh trasparenti.
        /// </summary>
        /// <param name="vh">Helper dei vertici di uGUI.</param>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            var transparent = new Color32(0, 0, 0, 0);
            vh.AddVert(new Vector3(rect.xMin, rect.yMin), transparent, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMin, rect.yMax), transparent, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax, rect.yMax), transparent, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax, rect.yMin), transparent, Vector2.zero);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
            if (canvasRenderer != null) canvasRenderer.cullTransparentMesh = false;
        }
    }
}
