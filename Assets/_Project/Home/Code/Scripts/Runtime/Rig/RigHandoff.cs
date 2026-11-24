using UnityEngine;

namespace SI81.Rig
{
    /// <summary>
    /// Riferimenti che il Player Rig consegna alla UI e alla HUD: testa, origine del tracciamento, mano sinistra e mano
    /// destra. È l'unico contratto di hand-off: la UI non conosce il prefab né la gerarchia del rig.
    /// </summary>
    public readonly struct RigHandoff
    {
        /// <summary>Testa del giocatore (camera).</summary>
        public Transform Head { get; }
        /// <summary>Origine del tracciamento (genitore di testa e controller).</summary>
        public Transform TrackingOrigin { get; }
        /// <summary>Ancora del controller sinistro.</summary>
        public Transform LeftHand { get; }
        /// <summary>Ancora del controller destro.</summary>
        public Transform RightHand { get; }

        /// <summary>
        /// Crea l'hand-off.
        /// </summary>
        /// <param name="head">Testa.</param>
        /// <param name="trackingOrigin">Origine del tracciamento.</param>
        /// <param name="leftHand">Mano sinistra.</param>
        /// <param name="rightHand">Mano destra.</param>
        public RigHandoff(Transform head, Transform trackingOrigin, Transform leftHand, Transform rightHand)
        {
            Head = head;
            TrackingOrigin = trackingOrigin;
            LeftHand = leftHand;
            RightHand = rightHand;
        }

        /// <summary>
        /// true se tutti e quattro i riferimenti sono assegnati.
        /// </summary>
        public bool IsComplete => Head != null && TrackingOrigin != null && LeftHand != null && RightHand != null;
    }
}
