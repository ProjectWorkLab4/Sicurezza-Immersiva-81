using System.Collections.Generic;
using UnityEngine;

namespace SI81.Ludovica.TempCore
{
    /// <summary>
    /// [TEMP] Mano del rig temporaneo: trova l'oggetto vicino (o sotto il mirino in modalità desktop),
    /// gestisce afferra/lascia, usa, ispeziona e i contatti.
    /// </summary>
    public class TempHand : MonoBehaviour
    {
        [Tooltip("true = mano sinistra.")]
        [SerializeField] private bool isLeft;
        [Tooltip("Raggio di contatto della mano (m).")]
        [SerializeField] private float touchRadius = 0.12f;
        [Tooltip("Distanza massima del mirino in modalità desktop (m).")]
        [SerializeField] private float desktopReach = 3f;

        private readonly Collider[] overlap = new Collider[16];
        private readonly HashSet<TempInteractable> touching = new HashSet<TempInteractable>();
        private readonly HashSet<TempInteractable> touchingNow = new HashSet<TempInteractable>();
        private TempInteractable hovered;
        private TempInteractable held;
        private TempInteractable used;
        private Transform heldOriginalParent;
        private UnityEngine.SceneManagement.Scene heldOriginalScene;

        /// <summary>true = mano sinistra.</summary>
        public bool IsLeft => isLeft;
        /// <summary>Oggetto tenuto (null se nessuno).</summary>
        public TempInteractable Held => held;
        /// <summary>Oggetto puntato o vicino.</summary>
        public TempInteractable Hovered => hovered;
        /// <summary>Direzione "in avanti" della mano (o della testa in desktop).</summary>
        public Vector3 Forward => transform.forward;
        /// <summary>true se il grilletto è premuto in questo momento.</summary>
        public bool UseHeld { get; private set; }

        /// <summary>Aggiorna la mano con lo stato dei tasti di questo frame.</summary>
        /// <param name="input">Stato dei tasti.</param>
        /// <param name="desktopRay">Raggio del mirino (solo desktop, altrimenti null).</param>
        public void Tick(HandButtons input, Ray? desktopRay)
        {
            UpdateHover(desktopRay);

            if (input.GripDown)
            {
                if (held != null) Release();
                else if (hovered != null) TryGrab(hovered);
            }
            else if (input.GripUp && held != null && !input.GripIsToggle)
            {
                Release();
            }

            if (input.TriggerDown)
            {
                used = held != null ? held : hovered;
                if (used != null)
                {
                    used.OnContact(this);
                    used.OnUseStart(this);
                }
                UseHeld = true;
            }
            if (input.TriggerUp)
            {
                if (used != null) used.OnUseEnd(this);
                used = null;
                UseHeld = false;
            }

            if (input.PrimaryDown)
            {
                var target = held != null ? held : hovered;
                if (target != null)
                {
                    target.OnContact(this);
                    target.OnInspect(this);
                }
            }
        }

        /// <summary>Forza il rilascio dell'oggetto tenuto (es. oggetto distrutto o riavvio).</summary>
        public void Release()
        {
            if (held == null) return;
            var item = held;
            held = null;
            item.transform.SetParent(heldOriginalParent, true);
            // Senza genitore l'oggetto resterebbe nella scena del rig: lo riporta nella scena dello scenario.
            if (heldOriginalParent == null && heldOriginalScene.IsValid() && heldOriginalScene.isLoaded)
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(item.gameObject, heldOriginalScene);
            heldOriginalParent = null;
            SetCollidersEnabled(item, true);
            item.OnRelease(this);
        }

        /// <summary>Prova ad afferrare l'oggetto.</summary>
        private void TryGrab(TempInteractable item)
        {
            item.OnContact(this);
            if (!item.IsGrabbable || item.HeldBy != null) return;
            held = item;
            heldOriginalParent = item.transform.parent;
            heldOriginalScene = item.gameObject.scene;
            SetCollidersEnabled(item, false);
            item.transform.SetParent(transform, true);
            item.GetHoldPose(out var localPos, out var localRot);
            item.transform.localPosition = localPos;
            item.transform.localRotation = localRot;
            item.OnGrab(this);
        }

        /// <summary>Trova l'oggetto vicino alla mano (VR) o sotto il mirino (desktop) e aggiorna i contatti.</summary>
        private void UpdateHover(Ray? desktopRay)
        {
            hovered = null;
            touchingNow.Clear();

            if (desktopRay.HasValue)
            {
                if (Physics.Raycast(desktopRay.Value, out var hit, desktopReach, ~0, QueryTriggerInteraction.Ignore))
                {
                    var it = hit.collider.GetComponentInParent<TempInteractable>();
                    if (it != null && it != held) hovered = it;
                }
            }
            else
            {
                int n = Physics.OverlapSphereNonAlloc(transform.position, touchRadius, overlap, ~0, QueryTriggerInteraction.Ignore);
                float best = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    var it = overlap[i].GetComponentInParent<TempInteractable>();
                    if (it == null || it == held) continue;
                    touchingNow.Add(it);
                    float d = (overlap[i].ClosestPoint(transform.position) - transform.position).sqrMagnitude;
                    if (d < best) { best = d; hovered = it; }
                }
            }

            foreach (var it in touchingNow)
                if (!touching.Contains(it)) it.OnContact(this);
            touching.Clear();
            foreach (var it in touchingNow) touching.Add(it);
        }

        /// <summary>Accende o spegne i collider dell'oggetto (spenti mentre è in mano, per non bloccare mirino e spruzzo).</summary>
        private static void SetCollidersEnabled(TempInteractable item, bool on)
        {
            foreach (var c in item.GetComponentsInChildren<Collider>(true)) c.enabled = on;
        }

        /// <summary>Disegna il raggio di contatto nella scena.</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, touchRadius);
        }
    }

    /// <summary>[TEMP] Stato dei tasti di una mano in un frame.</summary>
    public struct HandButtons
    {
        /// <summary>Grip premuto in questo frame.</summary>
        public bool GripDown;
        /// <summary>Grip rilasciato in questo frame.</summary>
        public bool GripUp;
        /// <summary>true se il grip funziona a interruttore (desktop).</summary>
        public bool GripIsToggle;
        /// <summary>Grilletto premuto in questo frame.</summary>
        public bool TriggerDown;
        /// <summary>Grilletto rilasciato in questo frame.</summary>
        public bool TriggerUp;
        /// <summary>A/X premuto in questo frame.</summary>
        public bool PrimaryDown;
    }
}
