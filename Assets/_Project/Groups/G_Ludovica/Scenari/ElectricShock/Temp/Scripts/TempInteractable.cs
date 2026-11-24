using UnityEngine;

namespace SI81.Ludovica.TempCore
{
    /// <summary>
    /// [TEMP] Base degli oggetti interattivi finché non arriva il VR Interaction System comune.
    /// Verbi: afferrare (grip), usare/attivare (grilletto), ispezionare (A/X), contatto (mano vicina).
    /// </summary>
    public class TempInteractable : MonoBehaviour
    {
        [Tooltip("Nome mostrato nei messaggi di Console.")]
        [SerializeField] private string displayName = "Oggetto";
        [Tooltip("Renderer da evidenziare (Guided). Vuoto = nessuna evidenziazione.")]
        [SerializeField] private Renderer[] highlightRenderers;

        private MaterialPropertyBlock block;
        private bool highlighted;

        /// <summary>Nome leggibile dell'oggetto.</summary>
        public string DisplayName => displayName;
        /// <summary>true se l'oggetto si può afferrare.</summary>
        public virtual bool IsGrabbable => false;
        /// <summary>Mano che tiene l'oggetto (null se libero).</summary>
        public TempHand HeldBy { get; private set; }

        /// <summary>Chiamato quando una mano tocca l'oggetto o inizia un'interazione con esso.</summary>
        /// <param name="hand">Mano.</param>
        public virtual void OnContact(TempHand hand) { }

        /// <summary>Chiamato quando l'oggetto viene afferrato.</summary>
        /// <param name="hand">Mano.</param>
        public virtual void OnGrab(TempHand hand) { HeldBy = hand; }

        /// <summary>Chiamato quando l'oggetto viene lasciato.</summary>
        /// <param name="hand">Mano.</param>
        public virtual void OnRelease(TempHand hand) { HeldBy = null; }

        /// <summary>Grilletto premuto (oggetto tenuto o toccato).</summary>
        /// <param name="hand">Mano.</param>
        public virtual void OnUseStart(TempHand hand) { }

        /// <summary>Grilletto rilasciato.</summary>
        /// <param name="hand">Mano.</param>
        public virtual void OnUseEnd(TempHand hand) { }

        /// <summary>Ispezione (A/X).</summary>
        /// <param name="hand">Mano.</param>
        public virtual void OnInspect(TempHand hand) { }

        /// <summary>Posa locale nella mano quando viene afferrato.</summary>
        /// <param name="localPosition">Posizione locale.</param>
        /// <param name="localRotation">Rotazione locale.</param>
        public virtual void GetHoldPose(out Vector3 localPosition, out Quaternion localRotation)
        {
            localPosition = Vector3.zero;
            localRotation = Quaternion.identity;
        }

        /// <summary>Accende o spegne l'evidenziazione (emissione) dei renderer.</summary>
        /// <param name="on">true = evidenziato.</param>
        public void SetHighlight(bool on)
        {
            if (highlightRenderers == null || highlighted == on) return;
            highlighted = on;
            if (block == null) block = new MaterialPropertyBlock();
            foreach (var r in highlightRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", on ? new Color(0.9f, 0.8f, 0.1f) * 1.5f : Color.black);
                r.SetPropertyBlock(block);
                foreach (var m in r.sharedMaterials) if (m != null) m.EnableKeyword("_EMISSION");
            }
        }

        /// <summary>Appoggia l'oggetto dritto sul pavimento nella posizione attuale.</summary>
        protected void DropUpright()
        {
            Vector3 p = transform.position;
            transform.SetPositionAndRotation(new Vector3(p.x, 0f, p.z), Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
        }
    }
}
