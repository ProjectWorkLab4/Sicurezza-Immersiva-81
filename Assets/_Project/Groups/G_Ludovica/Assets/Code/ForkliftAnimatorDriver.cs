using UnityEngine;

namespace G_Ludovica.ForkliftChaos
{
    /// <summary>
    /// Pilota l'Animator del muletto in base al movimento reale del mezzo: quando la radice si sposta, le ruote girano.
    /// Non conosce lo scenario né i waypoint, quindi funziona sia per il muletto fermo sia per quello in manovra.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Scenari/Forklift Chaos/Animator Driver")]
    public sealed class ForkliftAnimatorDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [Tooltip("Radice del mezzo di cui misurare lo spostamento. Vuoto = genitore di questo modello.")]
        [SerializeField] private Transform target;
        [Tooltip("Spostamento minimo per frame (metri) per considerare il mezzo in movimento.")]
        [SerializeField] private float movementThreshold = 0.0005f;
        [Tooltip("Velocità di riproduzione della clip ruote quando il mezzo si muove.")]
        [SerializeField] private float movingSpeed = 1f;

        private Vector3 lastPosition;

        /// <summary>Collega Animator e radice del mezzo.</summary>
        public void Configure(Animator newAnimator, Transform newTarget)
        {
            animator = newAnimator;
            target = newTarget;
            lastPosition = target != null ? target.position : transform.position;
            if (animator != null) animator.speed = 0f;
        }

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (target == null) target = transform.parent;
            lastPosition = target != null ? target.position : transform.position;
            if (animator != null) animator.speed = 0f;
        }

        private void Update()
        {
            if (animator == null) return;
            Vector3 position = target != null ? target.position : transform.position;
            bool moving = (position - lastPosition).sqrMagnitude > movementThreshold * movementThreshold;
            animator.speed = moving ? movingSpeed : 0f;
            lastPosition = position;
        }
    }
}
