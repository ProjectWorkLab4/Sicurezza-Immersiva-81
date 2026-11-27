using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// Due cavi visivi che collegano il DAE ai rispettivi elettrodi. Ogni cavo è un LineRenderer
    /// con punti in spazio mondo: segue dispositivo ed elettrodi ogni frame, con una semplice
    /// parabola di cedimento verso il basso adattata alla distanza fra gli estremi. Nessuna
    /// simulazione fisica, nessun cavo collider e nessun limite di distanza.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/Man Down/AED Leads")]
    public sealed class ManDownAedLeads : MonoBehaviour
    {
        [Header("Estremi")]
        [Tooltip("Origine del cavo A sul DAE.")]
        [SerializeField] private Transform daeStartA;
        [Tooltip("Origine del cavo B sul DAE.")]
        [SerializeField] private Transform daeStartB;
        [Tooltip("Destinazione del cavo A sull'elettrodo A.")]
        [SerializeField] private Transform electrodeEndA;
        [Tooltip("Destinazione del cavo B sull'elettrodo B.")]
        [SerializeField] private Transform electrodeEndB;

        [Header("Renderers")]
        [SerializeField] private LineRenderer leadA;
        [SerializeField] private LineRenderer leadB;

        [Header("Configurazione")]
        [Tooltip("Materiale dedicato dei cavi, assegnato anche ai due LineRenderer.")]
        [SerializeField] private Material leadMaterial;
        [Tooltip("Diametro del cavo in metri.")]
        [Min(0.0001f)]
        [SerializeField] private float width = 0.004f;
        [Tooltip("Numero di punti di ogni curva (minimo 2).")]
        [Min(2)]
        [SerializeField] private int pointCount = 16;
        [Tooltip("Cedimento massimo verso il basso in metri.")]
        [Min(0f)]
        [SerializeField] private float maxSag = 0.15f;

        private Vector3 lastStartA;
        private Vector3 lastEndA;
        private Vector3 lastStartB;
        private Vector3 lastEndB;
        private bool initialized;

        private void LateUpdate()
        {
            Refresh();
        }

        /// <summary>
        /// Ricalcola i due cavi. Chiamato automaticamente a ogni frame e utilizzabile dal reset
        /// della missione per riportarli subito alla configurazione iniziale.
        /// </summary>
        [ContextMenu("SI81/AED Leads/Refresh")]
        public void Refresh()
        {
            UpdateLead(leadA, daeStartA, electrodeEndA, ref lastStartA, ref lastEndA);
            UpdateLead(leadB, daeStartB, electrodeEndB, ref lastStartB, ref lastEndB);
            initialized = true;
        }

        private void UpdateLead(LineRenderer line, Transform startTransform, Transform endTransform,
            ref Vector3 lastStart, ref Vector3 lastEnd)
        {
            if (line == null || startTransform == null || endTransform == null) return;

            Vector3 start = startTransform.position;
            Vector3 end = endTransform.position;
            if (initialized && (start - lastStart).sqrMagnitude < 1e-8f &&
                (end - lastEnd).sqrMagnitude < 1e-8f) return;
            lastStart = start;
            lastEnd = end;

            int count = Mathf.Max(2, pointCount);
            if (line.positionCount != count) line.positionCount = count;
            line.startWidth = width;
            line.endWidth = width;
            if (leadMaterial != null && line.sharedMaterial != leadMaterial) line.sharedMaterial = leadMaterial;

            float distance = Vector3.Distance(start, end);
            float sag = Mathf.Min(maxSag, distance * 0.25f);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                Vector3 point = Vector3.Lerp(start, end, t);
                point += Vector3.down * (sag * 4f * t * (1f - t));
                line.SetPosition(i, point);
            }
        }
    }
}
