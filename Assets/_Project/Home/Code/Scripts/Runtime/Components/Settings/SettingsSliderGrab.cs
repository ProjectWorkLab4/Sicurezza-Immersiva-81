using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SI81.UI
{
    /// <summary>
    /// Area di presa invisibile dello slider (alta 40): riceve grilletto e trascinamento dei raggi dei controller (e il mouse in Editor)
    /// e li passa a <see cref="SettingsSliderRow"/>. Il valore segue il raggio già al primo clic (soglia di trascinamento disattivata).
    /// </summary>
    [RequireComponent(typeof(SettingsHitArea))]
    public class SettingsSliderGrab : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private SettingsSliderRow row;
        // Stato del puntatore.
        private bool hovered;
        private bool pressed;

        /// <summary>
        /// Imposta la riga proprietaria. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(SettingsSliderRow newRow)
        {
            row = newRow;
        }

        /// <summary>
        /// Puntatore sopra l'area: maniglia grande.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            Report();
        }

        /// <summary>
        /// Puntatore fuori dall'area: maniglia normale (se non si sta trascinando).
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            Report();
        }

        /// <summary>
        /// Grilletto premuto: inizia il trascinamento e porta il valore sul raggio.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (row == null || eventData.button != PointerEventData.InputButton.Left) return;
            eventData.useDragThreshold = false;
            pressed = true;
            row.SetFromPointer(eventData);
            Report();
        }

        /// <summary>
        /// Grilletto rilasciato: fine del trascinamento.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            pressed = false;
            Report();
        }

        /// <summary>
        /// Trascinamento: il valore segue il raggio.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (row == null || !pressed) return;
            row.SetFromPointer(eventData);
        }

        /// <summary>
        /// Alla disattivazione azzera lo stato (nessuna presa rimasta appesa).
        /// </summary>
        private void OnDisable()
        {
            hovered = false;
            pressed = false;
        }

        /// <summary>
        /// Comunica alla riga lo stato corrente del puntatore.
        /// </summary>
        private void Report()
        {
            if (row != null) row.SetPointerState(hovered, pressed);
        }
    }
}
