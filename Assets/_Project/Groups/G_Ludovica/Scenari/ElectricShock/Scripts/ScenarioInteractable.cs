using SI81.Input;
using SI81.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// Base degli oggetti interattivi dello scenario sopra l'interazione condivisa (SI81.Interaction.Interactable +
    /// RayInteractor del rig): inoltra gli eventi del raggio (grilletto = Select, grip = Grab) a metodi dello scenario.
    /// Gli oggetti afferrati non seguono la mano con l'offset del momento della presa (FollowsHand spento): vengono messi
    /// nella mano con una posa fissa, così ad esempio l'ugello dell'estintore punta dove punta il controller.
    /// I metodi Begin/End sono pubblici anche per i test (senza visore).
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class ScenarioInteractable : MonoBehaviour
    {
        [Tooltip("Nome mostrato nei messaggi di Console.")]
        [SerializeField] private string displayName = "Oggetto";
        [Tooltip("Renderer da evidenziare (Guided). Vuoto = nessuna evidenziazione.")]
        [SerializeField] private Renderer[] highlightRenderers;

        private Interactable interactable;
        private Hand? heldHand;
        private Transform originalParent;
        private Material[][] highlightMaterials;
        private bool highlighted;

        /// <summary>Nome leggibile dell'oggetto.</summary>
        public string DisplayName => displayName;
        /// <summary>true se l'oggetto si può afferrare (grip).</summary>
        public virtual bool IsGrabbable => false;
        /// <summary>true se l'oggetto si può usare col grilletto.</summary>
        public virtual bool IsSelectable => true;
        /// <summary>true = afferrato viene messo nella mano (posa fissa); false = resta dov'è (es. trascinare).</summary>
        protected virtual bool AttachToHand => true;
        /// <summary>true mentre l'oggetto è tenuto in mano.</summary>
        public bool IsHeld => heldHand.HasValue;
        /// <summary>Mano che lo tiene (valido solo se <see cref="IsHeld"/>).</summary>
        public Hand HeldHand => heldHand ?? Hand.Right;

        /// <summary>Configura l'Interactable condiviso e si iscrive ai suoi eventi.</summary>
        protected virtual void Awake()
        {
            homeScene = gameObject.scene;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            interactable = GetComponent<Interactable>();
            interactable.Selectable = IsSelectable;
            interactable.Grabbable = IsGrabbable;
            interactable.FollowsHand = false;
            interactable.SelectEntered += BeginSelect;
            interactable.SelectExited += EndSelect;
            interactable.GrabEntered += BeginGrab;
            interactable.GrabExited += EndGrab;
        }

        /// <summary>Si disiscrive e libera i materiali di evidenziazione.</summary>
        protected virtual void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            if (interactable != null)
            {
                interactable.SelectEntered -= BeginSelect;
                interactable.SelectExited -= EndSelect;
                interactable.GrabEntered -= BeginGrab;
                interactable.GrabExited -= EndGrab;
            }
            if (highlightMaterials != null)
                foreach (var set in highlightMaterials)
                    if (set != null) foreach (var m in set) if (m != null) Destroy(m);
        }

        private Scene homeScene;

        /// <summary>
        /// Ricomincia / esci con un oggetto in mano: l'oggetto e' figlio della mano del rig (scena core) e sopravvivrebbe allo
        /// scaricamento del suo scenario, restando attaccato al giocatore nella partita successiva. Si distrugge con la sua scena.
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            if (this != null && scene == homeScene) Destroy(gameObject);
        }

        /// <summary>Grilletto premuto puntando l'oggetto (o simulato dai test).</summary>
        /// <param name="hand">Mano.</param>
        public void BeginSelect(Hand hand)
        {
            OnContact(hand);
            OnSelectStart(hand);
        }

        /// <summary>Grilletto rilasciato.</summary>
        /// <param name="hand">Mano.</param>
        public void EndSelect(Hand hand) => OnSelectEnd(hand);

        /// <summary>Grip premuto puntando l'oggetto (o simulato dai test).</summary>
        /// <param name="hand">Mano.</param>
        public void BeginGrab(Hand hand)
        {
            OnContact(hand);
            if (!IsGrabbable || IsHeld) return;
            heldHand = hand;
            if (AttachToHand)
            {
                var anchor = PlayerLocator.HandAnchor(hand);
                if (anchor != null)
                {
                    originalParent = transform.parent;
                    SetCollidersEnabled(false);
                    transform.SetParent(anchor, true);
                    GetHoldPose(out var pos, out var rot);
                    transform.localPosition = pos;
                    transform.localRotation = rot;
                }
            }
            OnGrabStart(hand);
        }

        /// <summary>Grip rilasciato.</summary>
        /// <param name="hand">Mano.</param>
        public void EndGrab(Hand hand)
        {
            if (!IsHeld || HeldHand != hand) return;
            heldHand = null;
            if (AttachToHand && transform.parent != originalParent)
            {
                transform.SetParent(originalParent, true);
                SetCollidersEnabled(true);
            }
            OnGrabEnd(hand);
        }

        /// <summary>Contatto con l'oggetto (grilletto o grip su di esso): es. folgorazione se sotto tensione.</summary>
        /// <param name="hand">Mano.</param>
        protected virtual void OnContact(Hand hand) { }

        /// <summary>Inizio dell'uso col grilletto.</summary>
        /// <param name="hand">Mano.</param>
        protected virtual void OnSelectStart(Hand hand) { }

        /// <summary>Fine dell'uso col grilletto.</summary>
        /// <param name="hand">Mano.</param>
        protected virtual void OnSelectEnd(Hand hand) { }

        /// <summary>Oggetto afferrato.</summary>
        /// <param name="hand">Mano.</param>
        protected virtual void OnGrabStart(Hand hand) { }

        /// <summary>Oggetto lasciato.</summary>
        /// <param name="hand">Mano.</param>
        protected virtual void OnGrabEnd(Hand hand) { }

        /// <summary>Posa locale nella mano quando viene afferrato.</summary>
        public virtual void GetHoldPose(out Vector3 localPosition, out Quaternion localRotation)
        {
            localPosition = Vector3.zero;
            localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Evidenziazione (Guided): l'oggetto pulsa piano col suo stesso colore (un estintore rosso resta rosso).
        /// Usa copie runtime dei materiali (i materiali dei modelli importati hanno l'emissione spenta e non si modificano
        /// gli asset condivisi).
        /// </summary>
        /// <param name="on">true = evidenziato.</param>
        public void SetHighlight(bool on)
        {
            if (highlightRenderers == null || highlightRenderers.Length == 0 || highlighted == on) return;
            highlighted = on;
            if (highlightMaterials == null)
            {
                highlightMaterials = new Material[highlightRenderers.Length][];
                for (int i = 0; i < highlightRenderers.Length; i++)
                {
                    if (highlightRenderers[i] == null) continue;
                    highlightMaterials[i] = highlightRenderers[i].materials;
                    foreach (var m in highlightMaterials[i]) m.EnableKeyword("_EMISSION");
                }
            }
            if (!on) ApplyGlow(0f);
        }

        /// <summary>Pulsazione dell'evidenziazione (circa 1 al secondo, tra il 10% e il 45% del colore base).</summary>
        protected virtual void Update()
        {
            if (highlighted) ApplyGlow(Mathf.Lerp(0.1f, 0.45f, 0.5f + 0.5f * Mathf.Sin(Time.time * 6f)));
        }

        /// <summary>Emissione = colore base del materiale x intensità.</summary>
        private void ApplyGlow(float intensity)
        {
            if (highlightMaterials == null) return;
            foreach (var set in highlightMaterials)
                if (set != null)
                    foreach (var m in set)
                    {
                        if (m == null) continue;
                        Color baseColor = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                        m.SetColor("_EmissionColor", baseColor * intensity);
                    }
        }

        /// <summary>Appoggia l'oggetto dritto sul pavimento nella posizione attuale.</summary>
        protected void DropUpright()
        {
            Vector3 p = transform.position;
            transform.SetPositionAndRotation(new Vector3(p.x, 0f, p.z), Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
        }

        /// <summary>Accende o spegne i collider (spenti in mano: non bloccano il raggio né il corpo del giocatore).</summary>
        private void SetCollidersEnabled(bool on)
        {
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = on;
        }
    }
}
