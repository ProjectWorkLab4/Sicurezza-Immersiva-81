using UnityEngine;

namespace SI81.UI
{
    /// <summary>
    /// Aggancia HUD_WristPanel al controller della mano NON dominante (impostazione controls.dominant_hand) con uno
    /// scostamento sul dorso del polso, e lo sposta all'altro controller quando l'impostazione cambia (live). Funziona con
    /// qualsiasi rig (oggi OVRCameraRig): basta passare i due Transform dei controller con <see cref="SetAnchors"/>.
    /// Il pannello diventa figlio del controller (nessun calcolo per fotogramma). Se manca il controller il contenuto si nasconde.
    /// Scostamento e rotazione sono PROVVISORI: da tarare sul visore (vedi NOTES_C1.md).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/UI/Wrist Attach")]
    public class WristAttach : MonoBehaviour
    {
        [Header("Posa sul controller sinistro (la destra è lo specchio)")]
        [Tooltip("Posizione locale rispetto alla posa grip, in metri (sul dorso del polso).")]
        [SerializeField] private Vector3 localPosition = new Vector3(0f, 0.05f, -0.04f);
        [Tooltip("Rotazione locale in gradi: X=90 rivolge il fronte del pannello verso l'alto (+Y del controller), meno l'inclinazione verso il viso.")]
        [SerializeField] private Vector3 localEuler = new Vector3(70f, 0f, 0f);

        [Header("Riferimenti")]
        [Tooltip("Contenuto da nascondere se manca il controller (CANVAS_WristPanel).")]
        [SerializeField] private GameObject content;

        // Controller del rig.
        private Transform leftAnchor;
        private Transform rightAnchor;
        // Genitore originale (MNG_HUD), al quale si torna se manca il controller.
        private Transform homeParent;
        // false = HUD nascosta (pausa, esito): il contenuto resta spento anche con il controller presente.
        private bool hudVisible = true;

        /// <summary>Mano a cui il pannello è agganciato.</summary>
        public HUDHand CurrentHand { get; private set; }

        /// <summary>
        /// Ricorda il genitore originale.
        /// </summary>
        private void Awake()
        {
            homeParent = transform.parent;
        }

        /// <summary>
        /// Ascolta i cambi di impostazione e si aggancia.
        /// </summary>
        private void OnEnable()
        {
            HUDService.SettingsChanged += HandleSettingChanged;
            Refresh();
        }

        /// <summary>
        /// Smette di ascoltare.
        /// </summary>
        private void OnDisable()
        {
            HUDService.SettingsChanged -= HandleSettingChanged;
        }

        /// <summary>
        /// Imposta i controller del rig e riaggancia il pannello.
        /// </summary>
        /// <param name="left">Controller sinistro (può essere null).</param>
        /// <param name="right">Controller destro (può essere null).</param>
        public void SetAnchors(Transform left, Transform right)
        {
            leftAnchor = left;
            rightAnchor = right;
            Refresh();
        }

        /// <summary>
        /// Mostra o nasconde il pannello (HUD in pausa o durante l'esito).
        /// </summary>
        /// <param name="visible">true per mostrare.</param>
        public void SetHudVisible(bool visible)
        {
            hudVisible = visible;
            Refresh();
        }

        /// <summary>
        /// Reagisce al cambio della mano dominante.
        /// </summary>
        /// <param name="id">Id dell'impostazione (null/vuoto = qualsiasi).</param>
        private void HandleSettingChanged(string id)
        {
            if (string.IsNullOrEmpty(id) || id == SettingsCatalog.DOMINANT_HAND) Refresh();
        }

        /// <summary>
        /// Aggancia il pannello al controller della mano non dominante (rispecchiando lo scostamento per la destra).
        /// </summary>
        public void Refresh()
        {
            CurrentHand = HUDService.GetNonDominantHand();
            Transform anchor = CurrentHand == HUDHand.Left ? leftAnchor : rightAnchor;
            if (content != null) content.SetActive(anchor != null && hudVisible);
            if (anchor == null)
            {
                if (homeParent != null && transform.parent != homeParent) transform.SetParent(homeParent, false);
                return;
            }
            float mirror = CurrentHand == HUDHand.Left ? 1f : -1f;
            if (transform.parent != anchor) transform.SetParent(anchor, false);
            transform.localPosition = new Vector3(localPosition.x * mirror, localPosition.y, localPosition.z);
            transform.localRotation = Quaternion.Euler(localEuler.x, localEuler.y * mirror, localEuler.z * mirror);
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Imposta il contenuto da nascondere. Usato solo dai builder Editor.
        /// </summary>
        internal void Configure(GameObject newContent)
        {
            content = newContent;
        }
    }
}
