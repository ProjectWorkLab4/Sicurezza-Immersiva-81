using System;
using SI81.Input;
using UnityEngine;
using UnityEngine.Events;

namespace SI81.Interaction
{
    /// <summary>
    /// Oggetto interagibile condiviso: espone hover, select e presa per mano con eventi C# e UnityEvent per l'inspector.
    /// Vive sull'oggetto che il raggio deve colpire; il collider può stare sullo stesso GameObject o su un figlio
    /// (la ricerca risale la gerarchia). Non legge input, non disegna raggi e non referenzia la UI o il Meta SDK:
    /// lo consuma il <see cref="RayInteractor"/>, che consegna le azioni dell'Input Service.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Interaction/Interactable")]
    public sealed class Interactable : MonoBehaviour
    {
        [Header("Cosa consente")]
        [Tooltip("L'oggetto può essere selezionato con il grilletto.")]
        [SerializeField] private bool canSelect = true;
        [Tooltip("L'oggetto può essere afferrato con l'impugnatura.")]
        [SerializeField] private bool canGrab = true;
        [Tooltip("Durante la presa l'oggetto segue la mano. Disattiva se la presa è animata da uno script.")]
        [SerializeField] private bool followsHand = true;

        [Header("Eventi inspector")]
        [Tooltip("Il raggio di una mano è entrato sull'oggetto.")]
        [SerializeField] private HandEvent hoverEntered = new HandEvent();
        [Tooltip("Il raggio di una mano è uscito dall'oggetto.")]
        [SerializeField] private HandEvent hoverExited = new HandEvent();
        [Tooltip("Selezione con il grilletto iniziata.")]
        [SerializeField] private HandEvent selectEntered = new HandEvent();
        [Tooltip("Selezione con il grilletto conclusa.")]
        [SerializeField] private HandEvent selectExited = new HandEvent();
        [Tooltip("Presa con l'impugnatura iniziata.")]
        [SerializeField] private HandEvent grabEntered = new HandEvent();
        [Tooltip("Presa con l'impugnatura conclusa.")]
        [SerializeField] private HandEvent grabExited = new HandEvent();

        private readonly bool[] hoveredBy = new bool[2];
        private readonly bool[] selectedBy = new bool[2];
        private Hand grabbedBy = Hand.Any;

        /// <summary>Hover iniziato da una mano.</summary>
        public event Action<Hand> HoverEntered;
        /// <summary>Hover concluso da una mano.</summary>
        public event Action<Hand> HoverExited;
        /// <summary>Selezione iniziata da una mano.</summary>
        public event Action<Hand> SelectEntered;
        /// <summary>Selezione conclusa da una mano.</summary>
        public event Action<Hand> SelectExited;
        /// <summary>Presa iniziata da una mano.</summary>
        public event Action<Hand> GrabEntered;
        /// <summary>Presa conclusa da una mano.</summary>
        public event Action<Hand> GrabExited;

        /// <summary>true se il grilletto può selezionare l'oggetto.</summary>
        public bool Selectable
        {
            get => canSelect;
            set => canSelect = value;
        }

        /// <summary>true se l'impugnatura può afferrare l'oggetto.</summary>
        public bool Grabbable
        {
            get => canGrab;
            set => canGrab = value;
        }

        /// <summary>true se durante la presa l'oggetto segue la mano.</summary>
        public bool FollowsHand
        {
            get => followsHand;
            set => followsHand = value;
        }

        /// <summary>true se almeno una mano lo sta puntando.</summary>
        public bool IsHovered => hoveredBy[0] || hoveredBy[1];
        /// <summary>true se almeno una mano lo sta selezionando.</summary>
        public bool IsSelected => selectedBy[0] || selectedBy[1];
        /// <summary>true se una mano lo sta afferrando.</summary>
        public bool IsGrabbed => grabbedBy.IsSingle();
        /// <summary>Mano che lo sta afferrando, oppure <see cref="Hand.Any"/>.</summary>
        public Hand GrabbedBy => grabbedBy;

        /// <summary>
        /// true se la mano indicata lo sta puntando (Any = una qualsiasi).
        /// </summary>
        /// <param name="hand">Mano da controllare.</param>
        public bool IsHoveredBy(Hand hand)
        {
            if (hand == Hand.Any) return IsHovered;
            return hand.IsSingle() && hoveredBy[hand.Index()];
        }

        /// <summary>
        /// true se la mano indicata lo sta selezionando (Any = una qualsiasi).
        /// </summary>
        /// <param name="hand">Mano da controllare.</param>
        public bool IsSelectedBy(Hand hand)
        {
            if (hand == Hand.Any) return IsSelected;
            return hand.IsSingle() && selectedBy[hand.Index()];
        }

        /// <summary>
        /// true se la mano indicata lo sta afferrando.
        /// </summary>
        /// <param name="hand">Mano da controllare.</param>
        public bool IsGrabbedBy(Hand hand)
        {
            return hand.IsSingle() && grabbedBy == hand;
        }

        /// <summary>
        /// true se un raggio può agganciare l'oggetto in questo stato.
        /// </summary>
        /// <param name="hand">Mano singola.</param>
        public bool CanHover(Hand hand)
        {
            return isActiveAndEnabled && hand.IsSingle();
        }

        /// <summary>
        /// true se la mano può selezionare l'oggetto.
        /// </summary>
        /// <param name="hand">Mano singola.</param>
        public bool CanSelect(Hand hand)
        {
            return canSelect && CanHover(hand);
        }

        /// <summary>
        /// true se la mano può afferrare l'oggetto. Una sola mano alla volta: due mani sono fuori da questo incremento.
        /// </summary>
        /// <param name="hand">Mano singola.</param>
        public bool CanGrab(Hand hand)
        {
            return canGrab && !IsGrabbed && CanHover(hand);
        }

        /// <summary>
        /// Registra l'ingresso dell'hover e notifica. Chiamato dal <see cref="RayInteractor"/>.
        /// </summary>
        /// <param name="hand">Mano singola.</param>
        internal void HoverEnter(Hand hand)
        {
            if (!hand.IsSingle()) return;
            int index = hand.Index();
            if (hoveredBy[index]) return;
            hoveredBy[index] = true;
            HoverEntered?.Invoke(hand);
            hoverEntered.Invoke(hand);
        }

        /// <summary>
        /// Registra l'uscita dell'hover e notifica. Chiamato dal <see cref="RayInteractor"/>.
        /// </summary>
        /// <param name="hand">Mano singola.</param>
        internal void HoverExit(Hand hand)
        {
            if (!hand.IsSingle()) return;
            int index = hand.Index();
            if (!hoveredBy[index]) return;
            hoveredBy[index] = false;
            HoverExited?.Invoke(hand);
            hoverExited.Invoke(hand);
        }

        /// <summary>
        /// Registra l'inizio della selezione e notifica. Chiamato dal <see cref="RayInteractor"/>.
        /// </summary>
        /// <param name="hand">Mano singola.</param>
        internal void SelectEnter(Hand hand)
        {
            if (!hand.IsSingle()) return;
            int index = hand.Index();
            if (selectedBy[index]) return;
            selectedBy[index] = true;
            SelectEntered?.Invoke(hand);
            selectEntered.Invoke(hand);
        }

        /// <summary>
        /// Registra la fine della selezione e notifica. Chiamato dal <see cref="RayInteractor"/>.
        /// </summary>
        /// <param name="hand">Mano singola.</param>
        internal void SelectExit(Hand hand)
        {
            if (!hand.IsSingle()) return;
            int index = hand.Index();
            if (!selectedBy[index]) return;
            selectedBy[index] = false;
            SelectExited?.Invoke(hand);
            selectExited.Invoke(hand);
        }

        /// <summary>
        /// Registra l'inizio della presa e notifica. Chiamato dal <see cref="RayInteractor"/>.
        /// </summary>
        /// <param name="hand">Mano singola.</param>
        internal void GrabEnter(Hand hand)
        {
            if (!hand.IsSingle() || grabbedBy.IsSingle()) return;
            grabbedBy = hand;
            GrabEntered?.Invoke(hand);
            grabEntered.Invoke(hand);
        }

        /// <summary>
        /// Registra la fine della presa e notifica. Chiamato dal <see cref="RayInteractor"/>.
        /// </summary>
        /// <param name="hand">Mano singola.</param>
        internal void GrabExit(Hand hand)
        {
            if (grabbedBy != hand) return;
            grabbedBy = Hand.Any;
            GrabExited?.Invoke(hand);
            grabExited.Invoke(hand);
        }

        /// <summary>
        /// Azzera lo stato quando l'oggetto viene disattivato, così una riattivazione non mostra hover/prese fantasma.
        /// </summary>
        private void OnDisable()
        {
            hoveredBy[0] = hoveredBy[1] = false;
            selectedBy[0] = selectedBy[1] = false;
            grabbedBy = Hand.Any;
        }

        /// <summary>
        /// UnityEvent con la mano che ha prodotto l'evento, serializzabile nell'inspector.
        /// </summary>
        [Serializable]
        public sealed class HandEvent : UnityEvent<Hand>
        {
        }
    }
}
