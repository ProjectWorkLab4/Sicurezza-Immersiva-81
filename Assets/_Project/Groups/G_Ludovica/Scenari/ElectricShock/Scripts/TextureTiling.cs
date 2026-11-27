using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// Ripetizione della texture in metri reali su pavimento e pareti fatti con cubi scalati: un solo materiale per tutte
    /// le pareti, la ripetizione per oggetto va in un MaterialPropertyBlock (_BaseMap_ST di URP Lit, usato anche dalla normal map).
    /// Funziona anche nell'Editor.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class TextureTiling : MonoBehaviour
    {
        /// <summary>Quali assi della scala usare per la ripetizione.</summary>
        public enum Mode
        {
            /// <summary>Pavimento: faccia superiore (X e Z).</summary>
            Floor,
            /// <summary>Parete: lato lungo (X o Z) e altezza (Y).</summary>
            Wall
        }

        private static readonly int BASE_MAP_ST = Shader.PropertyToID("_BaseMap_ST");

        [Tooltip("Pavimento o parete.")]
        [SerializeField] private Mode mode = Mode.Wall;
        [Tooltip("Metri coperti da una ripetizione della texture.")]
        [SerializeField] private float metersPerTile = 2f;

        /// <summary>Imposta modo e dimensione della ripetizione (usato dal builder).</summary>
        /// <param name="newMode">Pavimento o parete.</param>
        /// <param name="meters">Metri per ripetizione.</param>
        public void Configure(Mode newMode, float meters)
        {
            mode = newMode;
            metersPerTile = Mathf.Max(0.1f, meters);
            Apply();
        }

        /// <summary>Applica la ripetizione all'attivazione.</summary>
        private void OnEnable() => Apply();

        /// <summary>Riapplica quando si cambiano i valori nell'Inspector.</summary>
        private void OnValidate() => Apply();

        /// <summary>Calcola la ripetizione dalla scala dell'oggetto e la scrive nel MaterialPropertyBlock.</summary>
        public void Apply()
        {
            var r = GetComponent<Renderer>();
            if (r == null) return;
            Vector3 s = transform.lossyScale;
            Vector2 tiles = mode == Mode.Floor ? new Vector2(s.x, s.z) : new Vector2(Mathf.Max(s.x, s.z), s.y);
            tiles /= Mathf.Max(0.1f, metersPerTile);
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetVector(BASE_MAP_ST, new Vector4(tiles.x, tiles.y, 0f, 0f));
            r.SetPropertyBlock(block);
        }
    }
}
