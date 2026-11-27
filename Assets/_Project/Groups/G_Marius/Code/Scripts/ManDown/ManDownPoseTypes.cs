using System;
using System.Collections.Generic;
using UnityEngine;

namespace SI81.ManDown
{
    /// <summary>
    /// Pose statiche dell'NPC gestite da <see cref="ManDownPoseController"/>. Non sono clip d'animazione:
    /// ogni posa è un insieme di trasformazioni locali delle ossa dello scheletro esistente.
    /// </summary>
    public enum ManDownPose
    {
        /// <summary>Posizione iniziale supina, senza modifiche allo scheletro.</summary>
        Supine,
        /// <summary>Posizione laterale di sicurezza.</summary>
        Recovery,
        /// <summary>Posizione con le gambe sollevate.</summary>
        LegsRaised,
        /// <summary>Posizione prona.</summary>
        Prone,
        /// <summary>Posizione seduta.</summary>
        Seated
    }

    /// <summary>
    /// Trasformazione locale di un singolo osso. Vengono memorizzati solo posizione e rotazione locali:
    /// la scala non viene mai salvata né applicata.
    /// </summary>
    [Serializable]
    public struct ManDownBonePose
    {
        /// <summary>Percorso dell'osso relativo alla radice dello scheletro (per esempio "Hips/Spine/Spine1").</summary>
        public string path;
        /// <summary>Posizione locale dell'osso.</summary>
        public Vector3 localPosition;
        /// <summary>Rotazione locale dell'osso.</summary>
        public Quaternion localRotation;

        /// <summary>Crea la trasformazione locale di un osso.</summary>
        public ManDownBonePose(string path, Vector3 localPosition, Quaternion localRotation)
        {
            this.path = path;
            this.localPosition = localPosition;
            this.localRotation = localRotation;
        }
    }

    /// <summary>
    /// Dati serializzati di una posa: flag di configurazione e trasformazioni locali delle ossa.
    /// Una posa non configurata ha <see cref="configured"/> = false e nessuna trasformazione.
    /// </summary>
    [Serializable]
    public sealed class ManDownPoseData
    {
        /// <summary>true se la posa è stata configurata consapevolmente dall'Editor.</summary>
        public bool configured;
        /// <summary>Trasformazioni locali delle ossa, ordinate per percorso.</summary>
        public List<ManDownBonePose> bones = new List<ManDownBonePose>();
    }

    /// <summary>
    /// Snapshot non serializzato dello scheletro corrente. Usato dall'Editor per catturare una posa
    /// e per trasferirla al controller senza introdurre dipendenze da animazioni o prefab.
    /// </summary>
    public sealed class ManDownPoseSnapshot
    {
        /// <summary>Trasformazioni locali catturate, ordinate per percorso.</summary>
        public readonly List<ManDownBonePose> Bones = new List<ManDownBonePose>();

        /// <summary>Numero di ossa catturate.</summary>
        public int Count => Bones.Count;
    }
}
