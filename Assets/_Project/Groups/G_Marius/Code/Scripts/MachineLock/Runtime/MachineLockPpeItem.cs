using SI81.Input;
using SI81.Interaction;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// PPE slot filled by one cabinet item (GDD §9.6). Every slot holds at most one equipped item,
    /// whatever the item is: wrong items occupy their slot too but provide no protection.
    /// </summary>
    public enum PpeSlot
    {
        /// <summary>Hands: heat gloves or disposable latex gloves.</summary>
        Gloves,
        /// <summary>Body: splash apron.</summary>
        Body,
        /// <summary>Face: face shield.</summary>
        Face,
        /// <summary>Respiratory: FFP3 respirator or surgical mask.</summary>
        Respiratory
    }

    /// <summary>
    /// One selectable PPE item on the cabinet shelf (GDD §8.2 and §9.6). Selecting the item asks the
    /// cabinet to equip it; the cabinet owns the slot state, while this component only reflects it.
    /// While equipped the item hides its renderers and colliders, so it disappears from the shelf and
    /// the ray cannot hit it. The object stays active and its components stay enabled, so coroutines and
    /// external references survive an equip/unequip cycle.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock PPE Item")]
    public sealed class MachineLockPpeItem : MonoBehaviour
    {
        [Header("Item")]
        [Tooltip("Slot this item fills once equipped.")]
        [SerializeField] private PpeSlot slot = PpeSlot.Gloves;
        [Tooltip("true when the item protects the player. Wrong items still fill the slot.")]
        [SerializeField] private bool isCorrect = true;
        [Tooltip("Italian item name used in the equip feedback text.")]
        [SerializeField] private string displayName = string.Empty;

        [Header("Wiring")]
        [Tooltip("Interactable on the item mesh. Selecting it equips the item.")]
        [SerializeField] private Interactable interactable;
        [Tooltip("Cabinet that owns the item and resolves the equip.")]
        [SerializeField] private MachineLockPpeCabinet cabinet;

        private Interactable wiredInteractable;
        private bool cabinetOpen;

        /// <summary>Slot this item fills once equipped.</summary>
        public PpeSlot Slot => slot;
        /// <summary>true when the item protects the player.</summary>
        public bool IsCorrect => isCorrect;
        /// <summary>Italian item name used in the feedback text.</summary>
        public string DisplayName => displayName;
        /// <summary>true while the item is worn and therefore hidden from the cabinet.</summary>
        public bool IsEquipped { get; private set; }

        /// <summary>
        /// Assigns the item data from the builder or a test and rewires the interaction events.
        /// Safe to call before or after <see cref="Awake"/>.
        /// </summary>
        /// <param name="ownerCabinet">Cabinet that owns the item.</param>
        /// <param name="ppeSlot">Slot filled by the item.</param>
        /// <param name="correct">true when the item protects the player.</param>
        /// <param name="itemDisplayName">Italian item name used in the feedback text.</param>
        public void Configure(MachineLockPpeCabinet ownerCabinet, PpeSlot ppeSlot, bool correct, string itemDisplayName)
        {
            cabinet = ownerCabinet;
            slot = ppeSlot;
            isCorrect = correct;
            displayName = itemDisplayName ?? string.Empty;
            EnsureInteractable();
            Wire();
            cabinetOpen = cabinet != null && cabinet.IsOpen;
            ApplySelectable();
        }

        /// <summary>
        /// Updates whether the cabinet is open. Items are selectable only while the cabinet is open
        /// and the item is not already worn; calling this before or after <see cref="SetEquipped"/>
        /// always leaves a deterministic state.
        /// </summary>
        /// <param name="open">true when the cabinet door is open.</param>
        public void SetCabinetSelectable(bool open)
        {
            cabinetOpen = open;
            ApplySelectable();
        }

        /// <summary>
        /// Applies the equipped state: hides or restores every renderer and collider of the item and
        /// updates the selectable flag. Driven by <see cref="MachineLockPpeCabinet.Equip"/>.
        /// </summary>
        /// <param name="equipped">true when the item is worn.</param>
        /// <param name="cabinetIsOpen">true when the cabinet door is open.</param>
        public void SetEquipped(bool equipped, bool cabinetIsOpen)
        {
            cabinetOpen = cabinetIsOpen;
            IsEquipped = equipped;
            SetVisualsVisible(!equipped);
            ApplySelectable();
        }

        private void Awake()
        {
            ResolveCabinet();
            EnsureInteractable();
        }

        private void OnEnable()
        {
            EnsureInteractable();
            Wire();
            ApplySelectable();
        }

        private void OnDisable()
        {
            Unwire();
        }

        private void ResolveCabinet()
        {
            if (cabinet == null) cabinet = GetComponentInParent<MachineLockPpeCabinet>();
        }

        private void EnsureInteractable()
        {
            if (interactable != null) return;
            interactable = GetComponent<Interactable>();
            if (interactable == null) interactable = GetComponentInChildren<Interactable>(true);
        }

        private void Wire()
        {
            if (interactable == null || wiredInteractable == interactable) return;
            Unwire();
            wiredInteractable = interactable;
            wiredInteractable.SelectEntered += OnSelectEntered;
        }

        private void Unwire()
        {
            if (wiredInteractable == null) return;
            wiredInteractable.SelectEntered -= OnSelectEntered;
            wiredInteractable = null;
        }

        private void OnSelectEntered(Hand hand)
        {
            ResolveCabinet();
            if (cabinet != null) cabinet.Equip(this);
        }

        private void ApplySelectable()
        {
            EnsureInteractable();
            if (interactable != null) interactable.Selectable = cabinetOpen && !IsEquipped;
        }

        private void SetVisualsVisible(bool visible)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].enabled = visible;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) colliders[i].enabled = visible;
            }
        }
    }
}
