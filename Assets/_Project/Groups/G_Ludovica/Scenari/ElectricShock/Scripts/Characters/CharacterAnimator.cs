using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SI81.Ludovica.ElectricShock
{
    /// <summary>
    /// Animazioni dei personaggi (modello Anim_Folgorazione_Worker, rig Generic) senza Animator Controller:
    /// riproduce una clip alla volta con un PlayableGraph. Le clip del modello non sono in loop e spostano il corpo
    /// rispetto al pivot: il loop e la correzione di posizione per clip (calcolata dal builder) sono fatti qui,
    /// così l'FBX condiviso resta com'è. Se non è forzata una clip, segue il movimento dell'NPC (fermo / corsa).
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        /// <summary>Una clip con il suo nome breve, il loop e la correzione della posizione del modello.</summary>
        [Serializable]
        public class Entry
        {
            /// <summary>Nome breve (es. "Idle", "Run", "Hurt").</summary>
            public string Key;
            /// <summary>Clip del modello.</summary>
            public AnimationClip Clip;
            /// <summary>true = ricomincia alla fine; false = resta sull'ultimo fotogramma.</summary>
            public bool Loop;
            /// <summary>Posizione locale del modello durante la clip (piedi o corpo appoggiati a terra, centrati).</summary>
            public Vector3 ModelOffset;
        }

        [Tooltip("Animator sul modello (senza controller).")]
        [SerializeField] private Animator animator;
        [Tooltip("Trasform del modello da riposizionare per ogni clip.")]
        [SerializeField] private Transform model;
        [Tooltip("Movimento dell'NPC (facoltativo): fermo = clip di riposo, in movimento = clip di corsa.")]
        [SerializeField] private NpcMover mover;
        [Tooltip("Clip disponibili.")]
        [SerializeField] private Entry[] entries;
        [Tooltip("Clip quando l'NPC è fermo.")]
        [SerializeField] private string idleKey = "Idle";
        [Tooltip("Clip quando l'NPC si muove.")]
        [SerializeField] private string moveKey = "Run";
        [Tooltip("Velocità della clip di corsa rispetto alla camminata degli NPC (la corsa è più veloce della camminata).")]
        [SerializeField] private float moveClipSpeed = 0.6f;
        [Tooltip("Clip forzata all'avvio (vuoto = segue il movimento).")]
        [SerializeField] private string startKey = "";
        [Tooltip("Durata della dissolvenza tra una clip e la successiva (s).")]
        [SerializeField] private float blendSeconds = 0.2f;
        [Tooltip("Distanza tra l'osso più basso e il pavimento (spessore di suola o schiena), in metri.")]
        [SerializeField] private float groundClearance = 0.06f;

        private PlayableGraph graph;
        private AnimationClipPlayable playable;
        private AnimationClipPlayable fromPlayable;
        private AnimationMixerPlayable mixer;
        private AnimationPlayableOutput output;
        private float blend = 1f;
        private Entry current;
        private string forcedKey;
        private string afterOnceKey;
        private bool onceActive;
        private Transform hips;
        private Transform[] bones;

        /// <summary>Nome della clip in riproduzione.</summary>
        public string CurrentKey => current != null ? current.Key : "";

        /// <summary>Crea il grafo e avvia la clip iniziale.</summary>
        private void Start()
        {
            if (animator == null) return;
            if (model != null)
            {
                // Solo le ossa dello scheletro (le mesh stanno all'origine del modello e falserebbero il calcolo).
                hips = Array.Find(model.GetComponentsInChildren<Transform>(true), t => t.name.EndsWith("Hips"));
                if (hips != null) bones = hips.GetComponentsInChildren<Transform>(true);
            }
            graph = PlayableGraph.Create(name + "_Anim");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            output = AnimationPlayableOutput.Create(graph, "Anim", animator);
            mixer = AnimationMixerPlayable.Create(graph, 2);
            output.SetSourcePlayable(mixer);
            if (!string.IsNullOrEmpty(startKey)) Force(startKey);
            else PlayInternal(idleKey);
        }

        /// <summary>Libera il grafo.</summary>
        private void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }

        /// <summary>Forza una clip finché non si chiama <see cref="Release"/> (es. ferito, soccorso).</summary>
        /// <param name="key">Nome breve della clip.</param>
        public void Force(string key)
        {
            forcedKey = key;
            onceActive = false;
            PlayInternal(key);
        }

        /// <summary>Riproduce una clip una volta, poi passa alla clip indicata (vuoto = segue il movimento).</summary>
        /// <param name="key">Clip da riprodurre una volta.</param>
        /// <param name="then">Clip successiva forzata, o vuoto.</param>
        public void PlayOnce(string key, string then = "")
        {
            forcedKey = null;
            onceActive = true;
            afterOnceKey = then;
            PlayInternal(key);
        }

        /// <summary>Cambia la clip usata quando l'NPC è fermo (es. da "lavora" a "in piedi").</summary>
        /// <param name="key">Nome breve della clip.</param>
        public void SetIdleKey(string key) => idleKey = key;

        /// <summary>Toglie la clip forzata: torna a seguire il movimento.</summary>
        public void Release()
        {
            forcedKey = null;
            onceActive = false;
        }

        /// <summary>Loop, fine delle clip singole e scelta fermo / corsa.</summary>
        private void Update()
        {
            if (!graph.IsValid() || current == null) return;
            UpdateBlend();
            double length = current.Clip != null ? current.Clip.length : 0d;
            bool finished = length > 0d && playable.GetTime() >= length;

            if (finished)
            {
                if (onceActive)
                {
                    onceActive = false;
                    if (!string.IsNullOrEmpty(afterOnceKey)) Force(afterOnceKey);
                    else PlayInternal(LocomotionKey());
                    return;
                }
                if (current.Loop) playable.SetTime(0d);
                else { playable.SetTime(length); playable.SetSpeed(0d); }
            }

            if (forcedKey == null && !onceActive)
            {
                string want = LocomotionKey();
                if (want != current.Key) PlayInternal(want);
            }
        }

        /// <summary>
        /// Le clip contengono spostamento della radice (la corsa avanza di 2 m per ciclo, alcune clip spostano il corpo di metri):
        /// dopo l'animazione il modello viene riportato con le anche sopra la radice e l'osso più basso sul pavimento.
        /// Lo spostamento vero lo fa NpcMover.
        /// </summary>
        private void LateUpdate()
        {
            if (model == null || hips == null || bones == null || current == null) return;
            Vector3 rootPos = transform.position;
            Vector3 h = hips.position;
            float minY = float.MaxValue;
            for (int i = 0; i < bones.Length; i++)
            {
                float y = bones[i].position.y;
                if (y < minY) minY = y;
            }
            if (minY == float.MaxValue) return;
            model.position += new Vector3(rootPos.x - h.x, rootPos.y + groundClearance - minY, rootPos.z - h.z);
        }

        /// <summary>Dissolvenza dalla clip precedente (ingresso 0) alla nuova (ingresso 1).</summary>
        private void UpdateBlend()
        {
            if (blend >= 1f) return;
            blend = blendSeconds > 0f ? Mathf.Min(1f, blend + Time.deltaTime / blendSeconds) : 1f;
            mixer.SetInputWeight(0, 1f - blend);
            mixer.SetInputWeight(1, blend);
            if (blend >= 1f && fromPlayable.IsValid())
            {
                graph.Disconnect(mixer, 0);
                fromPlayable.Destroy();
            }
        }

        /// <summary>Clip di riposo o di corsa secondo il movimento dell'NPC.</summary>
        private string LocomotionKey() => mover != null && mover.IsMoving ? moveKey : idleKey;

        /// <summary>Avvia la clip con quel nome e riposiziona il modello.</summary>
        private void PlayInternal(string key)
        {
            var entry = Array.Find(entries, e => e.Key == key);
            if (entry == null || entry.Clip == null || !graph.IsValid()) return;
            // La clip corrente diventa quella "da cui" sfumare; quella ancora più vecchia si elimina.
            if (fromPlayable.IsValid())
            {
                graph.Disconnect(mixer, 0);
                fromPlayable.Destroy();
            }
            if (playable.IsValid())
            {
                graph.Disconnect(mixer, 1);
                fromPlayable = playable;
                graph.Connect(fromPlayable, 0, mixer, 0);
            }
            playable = AnimationClipPlayable.Create(graph, entry.Clip);
            playable.SetApplyFootIK(false);
            playable.SetSpeed(key == moveKey ? moveClipSpeed : 1d);
            graph.Connect(playable, 0, mixer, 1);
            blend = fromPlayable.IsValid() ? 0f : 1f;
            mixer.SetInputWeight(0, 1f - blend);
            mixer.SetInputWeight(1, blend);
            graph.Play();
            current = entry;
            if (model != null) model.localPosition = entry.ModelOffset;
        }
    }
}
