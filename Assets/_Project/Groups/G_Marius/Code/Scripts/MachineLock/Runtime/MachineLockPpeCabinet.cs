using System;
using System.Collections.Generic;
using SI81.Input;
using SI81.Interaction;
using SI81.UI;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// PPE cabinet of the Machine Lock scenario (GDD §8.2 and §9.6). It starts closed: the door stays
    /// shut and the items are not selectable. Selecting the cabinet opens the door and makes the items
    /// selectable; selecting an item equips it into its slot, replacing the previous item of that slot
    /// and making it reappear on the shelf. The cabinet owns the slot state and reports it to the
    /// scenario and to the other content systems through <see cref="HasFullPpe"/> and
    /// <see cref="HasWrongPpeEquipped"/>. Feedback is text-only, as approved in GDD §1.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock PPE Cabinet")]
    public sealed class MachineLockPpeCabinet : MonoBehaviour
    {
        [Header("Wiring")]
        [Tooltip("Scenario that receives the PPE feedback and the count changes.")]
        [SerializeField] private MachineLockScenario scenario;
        [Tooltip("Interactable on the cabinet body. Selecting it opens the door.")]
        [SerializeField] private Interactable cabinetInteractable;
        [Tooltip("Cabinet door. Rotated around its local Y when the cabinet opens.")]
        [SerializeField] private Transform doorTransform;
        [Tooltip("Door rotation around the local Y when open, in degrees.")]
        [SerializeField] private float openDoorAngleY = 100f;

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

        /// <summary>true after the door was opened. Opening is idempotent.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Number of slots currently holding a correct, equipped item, from 0 to 4.
        /// Wrong items fill their slot but do not count.
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

        /// <summary>true only when all four correct items are worn.</summary>
        public bool HasFullPpe => CorrectCount == SlotCount;

        /// <summary>
        /// true when any slot holds a worn item that does not protect the player, such as the
        /// disposable latex gloves or the surgical mask.
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
        /// <param name="slot">Slot to query.</param>
        /// <returns>Worn item, or null.</returns>
        public MachineLockPpeItem GetEquipped(PpeSlot slot)
        {
            int index = (int)slot;
            if (index < 0 || index >= equippedBySlot.Length) return null;
            MachineLockPpeItem item = equippedBySlot[index];
            return item != null && item.IsEquipped ? item : null;
        }

        /// <summary>
        /// Opens the door, plays the open clip and makes every item selectable. Subsequent calls do
        /// nothing.
        /// </summary>
        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;

            if (doorTransform != null) doorTransform.localRotation = Quaternion.Euler(0f, openDoorAngleY, 0f);
            PlayOneShot(openClip);
            if (cabinetInteractable != null) cabinetInteractable.Selectable = false;

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
        /// <param name="item">Item to equip.</param>
        /// <returns>true when the item was equipped.</returns>
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
                    item.IsCorrect ? MachineLockTexts.PpeEquipFormat : MachineLockTexts.PpeWrongFormat,
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

        /// <summary>
        /// Adds every child item that is missing from the serialized list, so a builder that wires the
        /// prefab by hand cannot leave an item unreachable.
        /// </summary>
        private void CollectChildItems()
        {
            MachineLockPpeItem[] found = GetComponentsInChildren<MachineLockPpeItem>(true);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null && !items.Contains(found[i])) items.Add(found[i]);
            }
        }

        private void Wire()
        {
            if (cabinetInteractable == null || wiredInteractable == cabinetInteractable) return;
            Unwire();
            wiredInteractable = cabinetInteractable;
            wiredInteractable.SelectEntered += OnCabinetSelected;
        }

        private void Unwire()
        {
            if (wiredInteractable == null) return;
            wiredInteractable.SelectEntered -= OnCabinetSelected;
            wiredInteractable = null;
        }

        private void OnCabinetSelected(Hand hand)
        {
            Open();
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (oneShotAudio != null && clip != null) oneShotAudio.PlayOneShot(clip);
        }
    }
}
