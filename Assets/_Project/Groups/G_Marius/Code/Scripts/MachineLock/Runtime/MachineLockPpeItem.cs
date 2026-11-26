using SI81.Input;
using SI81.Interaction;
using SI81.UI;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// PPE slot filled by one cabinet item (GDD §9.6, ADR-0004). Every slot holds at most one equipped
    /// item, whatever the item is: distractor items occupy their slot too but provide no protection.
    /// </summary>
    public enum PpeSlot
    {
        /// <summary>Hands: EN 407 heat gloves (correct) or disposable latex gloves (distractor).</summary>
        Gloves,
        /// <summary>Body: chemical splash apron (correct).</summary>
        Body,
        /// <summary>Face: EN 166 full face shield (correct) or surgical mask (distractor).</summary>
        Face,
        /// <summary>Feet: S3 reinforced safety boots (correct).</summary>
        Feet
    }

    /// <summary>
    /// One selectable PPE item on the cabinet shelf (GDD §8.2, §9.6, ADR-0002, ADR-0004).
    /// Selecting the item asks the cabinet to equip it; the cabinet owns the slot state while this
    /// component reflects it visually and via hover Action Feedback.
    /// When equipped, the item's visual mesh hides and its shelf status indicator switches to
    /// <c>"&lt;Nome DPI&gt; indossato"</c>, while the bay hover collider stays enabled (with
    /// <c>Selectable = false</c>) so hovering the bay still confirms the worn state.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock PPE Item")]
    public sealed class MachineLockPpeItem : MonoBehaviour
    {
        [Header("Item")]
        [Tooltip("Slot this item fills once equipped.")]
        [SerializeField] private PpeSlot slot = PpeSlot.Gloves;
        [Tooltip("true when the item protects the player. Distractor items still fill the slot.")]
        [SerializeField] private bool isCorrect = true;
        [Tooltip("Italian item name used in hover feedback, shelf status and equip feedback.")]
        [SerializeField] private string displayName = string.Empty;

        [Header("Wiring")]
        [Tooltip("Interactable on the item bay. Selecting it equips the item; hovering reports its name and state.")]
        [SerializeField] private Interactable interactable;
        [Tooltip("Cabinet that owns the item and resolves the equip.")]
        [SerializeField] private MachineLockPpeCabinet cabinet;

        [Header("Shelf visuals")]
        [Tooltip("Visual root of the PPE mesh itself (hidden while worn). If null, hides all child renderers except the equipped badge.")]
        [SerializeField] private GameObject itemVisualRoot;
        [Tooltip("Physical shelf badge/status plaque shown when this item is currently worn (\"<Nome DPI> indossato\").")]
        [SerializeField] private GameObject equippedBadgeVisual;

        private readonly MachineLockHoverFeedback hoverFeedback = new MachineLockHoverFeedback();
        private Interactable wiredInteractable;
        private bool cabinetOpen;

        /// <summary>Slot this item fills once equipped.</summary>
        public PpeSlot Slot => slot;
        /// <summary>true when the item protects the player.</summary>
        public bool IsCorrect => isCorrect;
        /// <summary>Italian item name used in the feedback text.</summary>
        public string DisplayName => displayName;
        /// <summary>true while the item is worn and therefore hidden from the cabinet shelf.</summary>
        public bool IsEquipped { get; private set; }
        /// <summary>Most recent hover Action Feedback string emitted for this item.</summary>
        public string LastHoverFeedback => hoverFeedback.LastFeedback;
        /// <summary>true when the shelf equipped status indicator is active.</summary>
        public bool IsEquippedShelfStatusVisible => equippedBadgeVisual != null ? equippedBadgeVisual.activeSelf : IsEquipped;

        /// <summary>
        /// Assigns the item data from the builder or a test and rewires the interaction events.
        /// Safe to call before or after <see cref="Awake"/>.
        /// </summary>
        public void Configure(MachineLockPpeCabinet ownerCabinet, PpeSlot ppeSlot, bool correct, string itemDisplayName)
        {
            cabinet = ownerCabinet;
            slot = ppeSlot;
            isCorrect = correct;
            displayName = itemDisplayName ?? string.Empty;
            EnsureInteractable();
            Wire();
            cabinetOpen = cabinet != null && cabinet.IsOpen;
            ApplyShelfVisuals();
            ApplySelectable();
        }

        /// <summary>
        /// Assigns the optional visual mesh root and shelf equipped-status badge.
        /// </summary>
        public void ConfigureShelfVisuals(GameObject visualRoot, GameObject equippedBadge)
        {
            itemVisualRoot = visualRoot;
            equippedBadgeVisual = equippedBadge;
            ApplyShelfVisuals();
        }

        /// <summary>
        /// Returns the localized Italian slot name for this item.
        /// </summary>
        public string GetSlotItalianName()
        {
            switch (slot)
            {
                case PpeSlot.Gloves: return MachineLockTexts.PpeSlotGloves;
                case PpeSlot.Body: return MachineLockTexts.PpeSlotBody;
                case PpeSlot.Face: return MachineLockTexts.PpeSlotFace;
                case PpeSlot.Feet: return MachineLockTexts.PpeSlotFeet;
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Builds the hover Action Feedback line: Italian name + slot before equipping, or
        /// <c>"&lt;Nome DPI&gt; indossato"</c> while worn.
        /// </summary>
        public string GetHoverFeedbackText()
        {
            if (IsEquipped) return string.Format(MachineLockTexts.PpeEquippedFormat, displayName);
            return string.Format(MachineLockTexts.PpeHoverFormat, displayName, GetSlotItalianName());
        }

        /// <summary>
        /// Emits the hover Action Feedback for this PPE item immediately, subject to global cooldown.
        /// Regular VR ray hovers go through the continuous dwell timer in <see cref="OnHoverEntered"/>.
        /// </summary>
        public void EmitHoverFeedback()
        {
            if (!cabinetOpen && !IsEquipped) return;
            hoverFeedback.TryEmitNow(GetHoverFeedbackText(), ResolveHoverKind());
        }

        private FeedbackKind ResolveHoverKind()
        {
            return IsEquipped
                ? (isCorrect ? FeedbackKind.Positive : FeedbackKind.Negative)
                : FeedbackKind.Neutral;
        }

        /// <summary>
        /// Updates whether the cabinet is open. Items are selectable only while the cabinet is open
        /// and the item is not already worn.
        /// </summary>
        public void SetCabinetSelectable(bool open)
        {
            cabinetOpen = open;
            ApplySelectable();
        }

        /// <summary>
        /// Applies the equipped state: hides or restores the PPE mesh renderers, toggles the shelf
        /// equipped badge, and keeps the bay hover collider enabled so hovering confirms
        /// <c>"&lt;Nome DPI&gt; indossato"</c>. Driven by <see cref="MachineLockPpeCabinet.Equip"/>.
        /// </summary>
        public void SetEquipped(bool equipped, bool cabinetIsOpen)
        {
            cabinetOpen = cabinetIsOpen;
            IsEquipped = equipped;
            ApplyShelfVisuals();
            ApplySelectable();
        }

        private void Awake()
        {
            ResolveCabinet();
            EnsureInteractable();
            ApplyShelfVisuals();
        }

        private void OnEnable()
        {
            EnsureInteractable();
            Wire();
            ApplyShelfVisuals();
            ApplySelectable();
        }

        private void OnDisable()
        {
            hoverFeedback.CancelHover();
            Unwire();
        }

        private void Update()
        {
            hoverFeedback.Tick();
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
            wiredInteractable.HoverEntered += OnHoverEntered;
            wiredInteractable.HoverExited += OnHoverExited;
        }

        private void Unwire()
        {
            if (wiredInteractable == null) return;
            wiredInteractable.SelectEntered -= OnSelectEntered;
            wiredInteractable.HoverEntered -= OnHoverEntered;
            wiredInteractable.HoverExited -= OnHoverExited;
            wiredInteractable = null;
        }

        private void OnSelectEntered(Hand hand)
        {
            hoverFeedback.CancelHover();
            ResolveCabinet();
            if (cabinet != null) cabinet.Equip(this);
        }

        private void OnHoverEntered(Hand hand)
        {
            if (!cabinetOpen && !IsEquipped) return;
            hoverFeedback.BeginHover(GetHoverFeedbackText(), ResolveHoverKind());
        }

        private void OnHoverExited(Hand hand)
        {
            hoverFeedback.CancelHover();
        }

        private void ApplySelectable()
        {
            EnsureInteractable();
            if (interactable != null) interactable.Selectable = cabinetOpen && !IsEquipped;
        }

        private void ApplyShelfVisuals()
        {
            bool showItem = !IsEquipped;
            if (itemVisualRoot != null)
            {
                Renderer[] visualRenderers = itemVisualRoot.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < visualRenderers.Length; i++)
                {
                    if (visualRenderers[i] != null) visualRenderers[i].enabled = showItem;
                }
            }
            else
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer r = renderers[i];
                    if (r == null) continue;
                    if (equippedBadgeVisual != null && r.transform.IsChildOf(equippedBadgeVisual.transform)) continue;
                    r.enabled = showItem;
                }
            }

            if (equippedBadgeVisual != null && equippedBadgeVisual.activeSelf != IsEquipped)
            {
                equippedBadgeVisual.SetActive(IsEquipped);
            }
        }
    }
}
