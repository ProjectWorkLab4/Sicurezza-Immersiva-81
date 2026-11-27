using UnityEngine;
using SI81.Tiziano.Evacuation.Systems;
using UDebug = UnityEngine.Debug;

namespace SI81.Tiziano.Evacuation.Systems
{
    /// <summary>
    /// Corpo trigger che segue la testa del giocatore (solo XZ): ingresso in un focolaio attivo
    /// = contatto, con cooldown dalla config. Non legge input e non crea UI.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class EvacPlayerFireSensor : MonoBehaviour
    {
        [SerializeField] private EvacuationDirector director;
        [SerializeField] private float headHeight = 1.6f;

        private float lastContactTime = -1000f;
        private Transform head;

        private void Awake()
        {
            if (director == null)
                director = FindFirstObjectByType<EvacuationDirector>();
            var body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var capsule = GetComponent<CapsuleCollider>();
            capsule.isTrigger = true;
        }

        private void Update()
        {
            if (head == null)
                head = Camera.main != null ? Camera.main.transform : null;
            if (head == null)
                return;
            Vector3 p = transform.position;
            transform.position = new Vector3(head.position.x, headHeight, head.position.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            EvacFireSlot slot = other.GetComponentInParent<EvacFireSlot>();
            if (slot == null || !slot.IsActive || director == null)
                return;

            float cooldown = director.Config != null ? director.Config.ContactCooldownSeconds : 2f;
            if (Time.time - lastContactTime < cooldown)
                return;

            lastContactTime = Time.time;
            UDebug.Log($"[EV] Fire contact in {slot.SlotId}.");
            director.ReportFireContact();
        }
    }
}
