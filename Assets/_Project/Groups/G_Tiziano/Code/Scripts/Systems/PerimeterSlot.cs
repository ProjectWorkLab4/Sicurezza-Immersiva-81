using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Interaction;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// One proximity slot around the spill perimeter.
    /// </summary>
    public sealed class PerimeterSlot : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float snapDistance = 0.35f;

        private SpillSausage placedItem;

        /// <summary>Gets whether this slot is occupied.</summary>
        public bool IsOccupied => placedItem != null;
        /// <summary>Gets the item occupying this slot.</summary>
        public SpillSausage PlacedItem => placedItem;
        /// <summary>Gets the snap distance.</summary>
        public float SnapDistance => snapDistance;

        /// <summary>Places a sausage in this slot.</summary>
        public void Place(SpillSausage sausage)
        {
            if (sausage == null)
                return;

            placedItem = sausage;
            sausage.AttachToSlot(this);
        }

        /// <summary>Clears this slot without destroying its item.</summary>
        public void Clear()
        {
            if (placedItem != null)
                placedItem.ClearSlot(this);
            placedItem = null;
        }
    }
}
