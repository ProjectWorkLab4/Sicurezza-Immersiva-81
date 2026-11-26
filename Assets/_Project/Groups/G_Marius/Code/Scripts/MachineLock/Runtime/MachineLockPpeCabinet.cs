using System;
using System.Collections.Generic;
using SI81.Input;
using SI81.Interaction;
using SI81.UI;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// PPE cabinet of the Machine Lock scenario (GDD §8.2, §9.6, ADR-0004). It starts closed: the
    /// door(s) stay shut and the items are not selectable. Selecting the cabinet opens the non-blocking
    /// doors, disables every door collider so the player can never be trapped, and makes all six items
    /// selectable. Selecting an item equips it into its <see cref="PpeSlot"/>, replacing any previous
    /// item in that slot and returning it to the shelf.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock PPE Cabinet")]
    public sealed class MachineLockPpeCabinet : MonoBehaviour
    {
        private const float HOVER_THROTTLE_SECONDS = 0.25f;

        [Header("Wiring")]
        [Tooltip("Scenario that receives the PPE feedback and the count changes.")]
        [SerializeField] private MachineLockScenario scenario;
        [Tooltip("Interactable on the cabinet door/body. Selecting it opens the doors.")]
        [SerializeField] private Interactable cabinetInteractable;
        [Tooltip("Optional second door interactable for double/sliding doors.")]
        [SerializeField] private Interactable secondaryDoorInteractable;
        [Tooltip("Primary cabinet door (left door when double/sliding).")]
        [SerializeField] private Transform doorTransform;
        [Tooltip("Optional secondary cabinet door (right door when double/sliding).")]
        [SerializeField] private Transform secondaryDoorTransform;
        [Tooltip("When true, doors slide outward along local X instead of swinging into the corridor.")]
        [SerializeField] private bool slideDoorsOpen = true;
        [Tooltip("Local X slide distance for each sliding door when open, in metres.")]
        [SerializeField] private float doorSlideDistance = 0.72f;
        [Tooltip("Swing rotation around local Y when slideDoorsOpen is false, in degrees.")]
        [SerializeField] private float openDoorAngleY = 100f;
        [Tooltip("Door colliders disabled immediately on Open() so the doors never block locomotion.")]
        [SerializeField] private Collider[] doorColliders;

        [Header("Audio")]
        [Tooltip("Source used for the one-shot open and equip clips.")]
        [SerializeField] private AudioSource oneShotAudio;
        [Tooltip("One-shot clip played when an item is equipped.")]
        [SerializeField] private AudioClip equipClip;
        [Tooltip("One-shot clip played when the door opens.")]
        [SerializeField] private AudioClip openClip;

        [Header("Items")]
        [Tooltip("Items on the shelves. Empty at runtime: every child item is collected in Awake.")]
        [SerializeField] private List<MachineLockPpeItem> items = new List<MachineLockPpeItem>();

        private static readonly int SlotCount = Enum.GetValues(typeof(PpeSlot)).Length;
        private readonly MachineLockPpeItem[] equippedBySlot = new MachineLockPpeItem[SlotCount];
        private Interactable wiredInteractable;
        private Interactable wiredSecondaryInteractable;
        private readonly List<Collider> resolvedDoorColliders = new List<Collider>();
        private float lastHoverTime = float.NegativeInfinity;

        /// <summary>true after the door was opened. Opening is idempotent.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Number of slots currently holding a correct, equipped item, from 0 to 4.
        /// Distractor items fill their slot but do not count.
        /// </summary>
        public int CorrectCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < equippedBySlot.Length; i++)
                {
                    MachineLockPpeItem item = equippedBySlot[i];
                    if (item != null && item.IsEquipped && item.IsCorrect) count++;
                }
                return count;
            }
        }

        /// <summary>true only when all four correct items (Gloves, Body, Face, Feet) are worn.</summary>
        public bool HasFullPpe => CorrectCount == SlotCount;

        /// <summary>
        /// true when any slot holds a worn distractor item that does not protect the player,
        /// such as disposable latex gloves or a surgical mask.
        /// </summary>
        public bool HasWrongPpeEquipped
        {
            get
            {
                for (int i = 0; i < equippedBySlot.Length; i++)
                {
                    MachineLockPpeItem item = equippedBySlot[i];
                    if (item != null && item.IsEquipped && !item.IsCorrect) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns the item currently worn in the slot, or null when the slot is empty.
        /// </summary>
        public MachineLockPpeItem GetEquipped(PpeSlot slot)
        {
            int index = (int)slot;
            if (index < 0 || index >= equippedBySlot.Length) return null;
            MachineLockPpeItem item = equippedBySlot[index];
            return item != null && item.IsEquipped ? item : null;
        }

        /// <summary>
        /// Opens the door(s), disables all door colliders so locomotion is never blocked, plays the open
        /// clip, and makes every PPE item selectable. Subsequent calls do nothing.
        /// </summary>
        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;

            ResolveDoorColliders();
            if (slideDoorsOpen)
            {
                if (doorTransform != null)
                    doorTransform.localPosition += Vector3.left * doorSlideDistance;
                if (secondaryDoorTransform != null)
                    secondaryDoorTransform.localPosition += Vector3.right * doorSlideDistance;
            }
            else
            {
                if (doorTransform != null)
                    doorTransform.localRotation = Quaternion.Euler(0f, openDoorAngleY, 0f);
                if (secondaryDoorTransform != null)
                    secondaryDoorTransform.localRotation = Quaternion.Euler(0f, -openDoorAngleY, 0f);
            }

            // Non-blocking ergonomics: disable every door collider immediately so opening the locker
            // can never trap or clip the player's CharacterController or block rays to items inside.
            for (int i = 0; i < resolvedDoorColliders.Count; i++)
            {
                if (resolvedDoorColliders[i] != null) resolvedDoorColliders[i].enabled = false;
            }

            PlayOneShot(openClip);
            if (cabinetInteractable != null) cabinetInteractable.Selectable = false;
            if (secondaryDoorInteractable != null) secondaryDoorInteractable.Selectable = false;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null) items[i].SetCabinetSelectable(true);
            }
        }

        /// <summary>
        /// Equips an item into its slot. The previous item of the same slot, if any, reappears on the
        /// shelf. Shows the text feedback and notifies the scenario. Returns false when the cabinet is
        /// closed, the item is null or the item is already worn.
        /// </summary>
        public bool Equip(MachineLockPpeItem item)
        {
            if (!IsOpen || item == null || item.IsEquipped) return false;

            int index = (int)item.Slot;
            if (index < 0 || index >= equippedBySlot.Length) return false;

            MachineLockPpeItem previous = equippedBySlot[index];
            if (previous != null && previous != item) previous.SetEquipped(false, IsOpen);

            equippedBySlot[index] = item;
            item.SetEquipped(true, IsOpen);
            PlayOneShot(equipClip);

            if (scenario != null)
            {
                string line = string.Format(
                    item.IsCorrect ? MachineLockTexts.PpeEquippedFormat : MachineLockTexts.PpeWrongFormat,
                    item.DisplayName);
                scenario.Feedback(line, item.IsCorrect ? FeedbackKind.Positive : FeedbackKind.Negative);
                scenario.NotifyPpeChanged(CorrectCount, HasFullPpe);
            }

            return true;
        }

        private void Awake()
        {
            if (items == null) items = new List<MachineLockPpeItem>();
            if (cabinetInteractable == null) cabinetInteractable = GetComponent<Interactable>();

            CollectChildItems();
            ResolveDoorColliders();

            IsOpen = false;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null) items[i].SetCabinetSelectable(false);
            }
        }

        private void OnEnable()
        {
            Wire();
        }

        private void OnDisable()
        {
            Unwire();
        }

        private void CollectChildItems()
        {
            MachineLockPpeItem[] found = GetComponentsInChildren<MachineLockPpeItem>(true);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null && !items.Contains(found[i])) items.Add(found[i]);
            }
        }

        private void ResolveDoorColliders()
        {
            resolvedDoorColliders.Clear();
            if (doorColliders != null)
            {
                for (int i = 0; i < doorColliders.Length; i++)
                    AddDoorCollider(doorColliders[i]);
            }
            CollectCollidersUnder(doorTransform);
            CollectCollidersUnder(secondaryDoorTransform);
            if (cabinetInteractable != null && cabinetInteractable.gameObject != gameObject)
                CollectCollidersUnder(cabinetInteractable.transform);
            if (secondaryDoorInteractable != null && secondaryDoorInteractable.gameObject != gameObject)
                CollectCollidersUnder(secondaryDoorInteractable.transform);
        }

        private void CollectCollidersUnder(Transform root)
        {
            if (root == null) return;
            Collider[] found = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < found.Length; i++) AddDoorCollider(found[i]);
        }

        private void AddDoorCollider(Collider col)
        {
            if (col == null || resolvedDoorColliders.Contains(col)) return;
            resolvedDoorColliders.Add(col);
        }

        private void Wire()
        {
            if (cabinetInteractable != null && wiredInteractable != cabinetInteractable)
            {
                if (wiredInteractable != null)
                {
                    wiredInteractable.SelectEntered -= OnCabinetSelected;
                    wiredInteractable.HoverEntered -= OnCabinetHovered;
                }
                wiredInteractable = cabinetInteractable;
                wiredInteractable.SelectEntered += OnCabinetSelected;
                wiredInteractable.HoverEntered += OnCabinetHovered;
            }

            if (secondaryDoorInteractable != null && wiredSecondaryInteractable != secondaryDoorInteractable)
            {
                if (wiredSecondaryInteractable != null)
                {
                    wiredSecondaryInteractable.SelectEntered -= OnCabinetSelected;
                    wiredSecondaryInteractable.HoverEntered -= OnCabinetHovered;
                }
                wiredSecondaryInteractable = secondaryDoorInteractable;
                wiredSecondaryInteractable.SelectEntered += OnCabinetSelected;
                wiredSecondaryInteractable.HoverEntered += OnCabinetHovered;
            }
        }

        private void Unwire()
        {
            if (wiredInteractable != null)
            {
                wiredInteractable.SelectEntered -= OnCabinetSelected;
                wiredInteractable.HoverEntered -= OnCabinetHovered;
                wiredInteractable = null;
            }
            if (wiredSecondaryInteractable != null)
            {
                wiredSecondaryInteractable.SelectEntered -= OnCabinetSelected;
                wiredSecondaryInteractable.HoverEntered -= OnCabinetHovered;
                wiredSecondaryInteractable = null;
            }
        }

        private void OnCabinetSelected(Hand hand)
        {
            Open();
        }

        private void OnCabinetHovered(Hand hand)
        {
            if (IsOpen) return;
            float now = Time.unscaledTime;
            if (!float.IsNegativeInfinity(lastHoverTime) && now - lastHoverTime < HOVER_THROTTLE_SECONDS) return;
            lastHoverTime = now;
            if (HUDService.IsReady)
                HUDService.ShowActionFeedback(MachineLockTexts.HoverPpeCabinetClosed, FeedbackKind.Neutral);
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (oneShotAudio != null && clip != null) oneShotAudio.PlayOneShot(clip);
        }
    }
}
