using System.Collections.Generic;
using UnityEngine;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// [TEMP] Suoni segnaposto generati da codice (nessun file audio): ronzio del generatore, crepitio del cavo,
    /// fuoco, sibilo del serbatoio, esplosione. Da sostituire con gli SFX del Common Core (gruppo Environment + Audio + VFX):
    /// basta assegnare un clip all'AudioSource e questo generatore non viene più usato.
    /// </summary>
    public static class PlaceholderAudio
    {
        /// <summary>Tipo di suono segnaposto.</summary>
        public enum Kind
        {
            /// <summary>Ronzio del motore (loop).</summary>
            EngineHum,
            /// <summary>Crepitio elettrico (loop).</summary>
            ElectricCrackle,
            /// <summary>Fuoco (loop).</summary>
            FireRoar,
            /// <summary>Sibilo del serbatoio (loop).</summary>
            TankHiss,
            /// <summary>Esplosione (una volta).</summary>
            Explosion
        }

        private const int RATE = 22050;
        private static readonly Dictionary<Kind, AudioClip> cache = new Dictionary<Kind, AudioClip>();

        /// <summary>Restituisce (e crea alla prima richiesta) il clip del tipo indicato.</summary>
        /// <param name="kind">Tipo di suono.</param>
        public static AudioClip Get(Kind kind)
        {
            if (cache.TryGetValue(kind, out var clip) && clip != null) return clip;
            clip = kind switch
            {
                Kind.EngineHum => Make("SFX_Placeholder_EngineHum", 2f, EngineHum),
                Kind.ElectricCrackle => Make("SFX_Placeholder_Crackle", 2f, Crackle),
                Kind.FireRoar => Make("SFX_Placeholder_Fire", 3f, FireRoar),
                Kind.TankHiss => Make("SFX_Placeholder_Hiss", 2f, Hiss),
                _ => Make("SFX_Placeholder_Explosion", 2.5f, Explosion)
            };
            cache[kind] = clip;
            return clip;
        }

        /// <summary>Assegna il clip segnaposto se la sorgente non ne ha uno.</summary>
        /// <param name="source">Sorgente audio (può essere null).</param>
        /// <param name="kind">Tipo di suono.</param>
        public static void EnsureClip(AudioSource source, Kind kind)
        {
            if (source != null && source.clip == null) source.clip = Get(kind);
        }

        /// <summary>Crea un clip mono campione per campione.</summary>
        private static AudioClip Make(string name, float seconds, System.Func<int, float, System.Random, float> sample)
        {
            int n = Mathf.RoundToInt(seconds * RATE);
            var data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(sample(i, (float)i / RATE, rng), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Ronzio a 50 Hz con armoniche (si ripete senza scatti: 2 s = cicli interi).</summary>
        private static float EngineHum(int i, float t, System.Random r)
        {
            float w = 2f * Mathf.PI * 50f * t;
            return 0.35f * Mathf.Sin(w) + 0.2f * Mathf.Sin(2f * w) + 0.1f * Mathf.Sin(3f * w + 0.5f) + 0.03f * (float)(r.NextDouble() * 2 - 1);
        }

        /// <summary>Scariche brevi e irregolari.</summary>
        private static float Crackle(int i, float t, System.Random r)
        {
            return r.NextDouble() < 0.004 ? (float)(r.NextDouble() * 2 - 1) * 0.9f : (float)(r.NextDouble() * 2 - 1) * 0.02f;
        }

        private static float brown;

        /// <summary>Rumore "marrone" (cupo) con pulsazioni lente.</summary>
        private static float FireRoar(int i, float t, System.Random r)
        {
            brown = Mathf.Clamp(brown + (float)(r.NextDouble() * 2 - 1) * 0.04f, -1f, 1f) * 0.995f;
            float flicker = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 1.5f * t);
            return brown * 1.6f * flicker + (r.NextDouble() < 0.001 ? 0.4f : 0f);
        }

        /// <summary>Rumore bianco tenue e acuto.</summary>
        private static float Hiss(int i, float t, System.Random r) => (float)(r.NextDouble() * 2 - 1) * 0.35f;

        /// <summary>Colpo cupo che si spegne in 2,5 s.</summary>
        private static float Explosion(int i, float t, System.Random r)
        {
            brown = Mathf.Clamp(brown + (float)(r.NextDouble() * 2 - 1) * 0.15f, -1f, 1f) * 0.99f;
            return brown * 2f * Mathf.Exp(-t * 2.2f);
        }
    }
}
