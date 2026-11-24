using System;
using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.ChemicalAlert.Data;
using SI81.Tiziano.ChemicalAlert.Systems;

namespace SI81.Tiziano.ChemicalAlert.Interaction
{
    /// <summary>
    /// Grabbable and inspectable safety data sheet item.
    /// </summary>
    public sealed class SdsItem : MonoBehaviour, IResettable
    {
        [SerializeField] private ScenarioInteractable interactable;
        [SerializeField] private ChemicalAlertDirector director;
        [SerializeField] private Transform resetAnchor;
        [SerializeField] private SubstanceVariant currentVariant;

        private Transform initialParent;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool hasBeenRead;

        /// <summary>Raised once when the SDS is inspected near the head.</summary>
        public event Action<SubstanceVariant> SdsRead;
        /// <summary>Gets the currently exposed substance variant.</summary>
        public SubstanceVariant CurrentVariant => currentVariant;
        /// <summary>Gets whether this SDS has already been read in the current reset cycle.</summary>
        public bool HasBeenRead => hasBeenRead;

        /// <summary>Assigns the SDS variant selected for the current replay.</summary>
        public void SetVariant(SubstanceVariant variant)
        {
            if (variant == null)
            {
                UnityEngine.Debug.LogError($"[CA] {name}: cannot assign a null SubstanceVariant.", this);
                return;
            }

            currentVariant = variant;
            hasBeenRead = false;
        }

        private void Awake()
        {
            if (interactable == null)
                interactable = GetComponent<ScenarioInteractable>();

            initialParent = transform.parent;
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private void OnEnable()
        {
            if (interactable != null)
            {
                interactable.Inspected += HandleInspected;
                interactable.Activated += HandleActivated;
            }
            if (director != null)
                director.PhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.Inspected -= HandleInspected;
                interactable.Activated -= HandleActivated;
            }
            if (director != null)
                director.PhaseChanged -= HandlePhaseChanged;
        }

        /// <summary>Resets read state, parent, transform and rigidbody motion.</summary>
        public void ResetState()
        {
            hasBeenRead = false;
            transform.SetParent(initialParent, true);
            Transform anchor = resetAnchor == null ? null : resetAnchor;
            transform.SetPositionAndRotation(
                anchor == null ? initialPosition : anchor.position,
                anchor == null ? initialRotation : anchor.rotation);

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = false;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private void HandleInspected(ScenarioInteractable source)
        {
            if (hasBeenRead)
                return;

            hasBeenRead = true;
            SdsRead?.Invoke(currentVariant);
            if (director != null)
            {
                director.RecordAction("sds.inspect", true, Dimension.Awareness, "chemical.sds.read");
                director.NotifySdsRead(currentVariant);
            }
            UnityEngine.Debug.Log(
                $"[CA] SdsRead: {name}; variant={(currentVariant == null ? "None" : currentVariant.VariantId)}");
        }

        private void HandleActivated(ScenarioInteractable source)
        {
            if (director == null)
                return;

            if (hasBeenRead)
                director.ToggleSdsPanel(currentVariant);
            else
                HandleInspected(source);
        }

        private void HandlePhaseChanged(ChemicalAlertDirector.Phase phase)
        {
            if (phase == ChemicalAlertDirector.Phase.Briefing)
                ResetState();
        }

        private void OnValidate()
        {
            if (currentVariant == null)
                UnityEngine.Debug.LogWarning($"[CA] {name}: current SubstanceVariant is not assigned.", this);
        }
    }
}
