using System;
using System.Collections.Generic;
using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.Evacuation.Core;
using SI81.Tiziano.Evacuation.Interaction;
using UDebug = UnityEngine.Debug;

namespace SI81.Tiziano.Evacuation.Systems
{
    /// <summary>
    /// Spawns the 3+2 colleagues on seeded NPCSLOT markers, keeps the collection order and
    /// marks the line as evacuated when the emergency exit is reported. IResettable.
    /// </summary>
    public sealed class NpcRegistry : MonoBehaviour, IResettable
    {
        [SerializeField] private EvacuationDirector director;
        [SerializeField] private EvacNpc npcPrefab;
        [SerializeField] private Transform markersRoot;
        [SerializeField] private NpcTrailFollower trail;
        [SerializeField] private string slotPrefix = "NPCSLOT_R";
        [SerializeField, Min(1)] private int slotsPerRoom = 6;

        private readonly List<EvacNpc> spawned = new List<EvacNpc>();
        private readonly List<EvacNpc> line = new List<EvacNpc>();

        /// <summary>Gets all NPCs spawned in the current run.</summary>
        public List<EvacNpc> Spawned => spawned;
        /// <summary>Gets the collected NPCs in collection order.</summary>
        public List<EvacNpc> Line => line;
        /// <summary>Gets the number of collected NPCs.</summary>
        public int CollectedCount => line.Count;

        /// <summary>Returns the spawned NPC slot ids in spawn order (stable signature of the current seed).</summary>
        public string LayoutSignature()
        {
            var parts = new string[spawned.Count];
            for (int i = 0; i < spawned.Count; i++)
                parts[i] = spawned[i] != null ? spawned[i].name : "null";
            return string.Join(",", parts);
        }

        private void Awake()
        {
            if (director == null)
                director = FindFirstObjectByType<EvacuationDirector>();
            if (trail == null)
                trail = FindFirstObjectByType<NpcTrailFollower>();
            if (markersRoot == null)
            {
                GameObject root = GameObject.Find("ScenarioMarkers");
                if (root != null)
                    markersRoot = root.transform;
            }
        }

        private void OnEnable()
        {
            if (director == null)
                return;
            director.RegisterResettable(this);
            director.ExitReported += HandleExitReported;
        }

        private void OnDisable()
        {
            if (director == null)
                return;
            director.UnregisterResettable(this);
            director.ExitReported -= HandleExitReported;
        }

        private void Start()
        {
            // The Director resets during Awake, before this registry can subscribe: do the first spawn here.
            if (Application.isPlaying)
                Respawn();
        }

        /// <summary>IResettable: destroys the old line/trail and respawns the NPCs with the current seed.</summary>
        public void ResetState()
        {
            DespawnAll();
            line.Clear();
            if (trail != null)
                trail.ResetState();
            Respawn();
        }

        /// <summary>Collects an NPC by its 1-based runtime index. Returns false when it is not Idle.</summary>
        public bool CollectNpc(int number, string source = "debug")
        {
            if (number < 1 || number > spawned.Count)
                return false;
            return Collect(spawned[number - 1], source);
        }

        /// <summary>Collects the first NPC still Idle. Returns false when none is available.</summary>
        public bool CollectNext(string source = "debug")
        {
            for (int i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] != null && spawned[i].State == EvacNpcState.Idle)
                    return Collect(spawned[i], source);
            }
            return false;
        }

        /// <summary>Collects every NPC still Idle and returns how many joined the line.</summary>
        public int CollectAll(string source = "debug")
        {
            int collected = 0;
            for (int i = 0; i < spawned.Count; i++)
            {
                if (CollectNext(source))
                    collected++;
            }
            return collected;
        }

        /// <summary>Collects an NPC, reports it to the Director and raises the stable dialogue key.</summary>
        public bool Collect(EvacNpc npc, string source)
        {
            if (npc == null)
                return false;
            if (npc.State != EvacNpcState.Idle)
            {
                if (director != null)
                    director.ReportNpcDialogue(EvacNpc.DialogueAlready);
                return false;
            }
            if (director == null || director.Phase != EvacPhase.Action)
            {
                UDebug.Log($"[EV] NPC collection ignored ({source}): scenario not in Action.");
                return false;
            }

            line.Add(npc);
            npc.BeginFollowing(line.Count - 1);
            director.ReportNpcCollected(npc.Room);
            director.ReportNpcDialogue(EvacNpc.DialogueFollow);
            UDebug.Log($"[EV] NPC collected: {npc.name} room {npc.Room} order {npc.Order} total {line.Count} ({source}).");
            return true;
        }

        /// <summary>Prints the spawned NPCs, the line and the trail through the logger.</summary>
        public void PrintStatus(Action<string> logger)
        {
            if (logger == null)
                return;
            logger($"[EV] NPC registry: spawned {spawned.Count}, collected {line.Count} (R1 {CountRoom(1)}/{RoomTotal(1)}, R2 {CountRoom(2)}/{RoomTotal(2)}).");
            for (int i = 0; i < line.Count; i++)
            {
                EvacNpc npc = line[i];
                if (npc != null)
                    logger($"[EV]   line #{i}: {npc.name} room {npc.Room} {npc.State}");
            }
            if (trail != null)
                trail.PrintTrail(logger);
        }

        private int CountRoom(int room)
        {
            int count = 0;
            for (int i = 0; i < line.Count; i++)
            {
                if (line[i] != null && line[i].Room == room)
                    count++;
            }
            return count;
        }

        private int RoomTotal(int room)
        {
            return director == null || director.Config == null ? 0 : room == 1 ? director.Config.NpcCountRoom1 : director.Config.NpcCountRoom2;
        }

        private void HandleExitReported(EvacExit kind)
        {
            if (kind != EvacExit.Emergency)
                return;
            int evacuated = 0;
            for (int i = 0; i < line.Count; i++)
            {
                if (line[i] != null && line[i].State == EvacNpcState.Following)
                {
                    line[i].MarkEvacuated();
                    evacuated++;
                }
            }
            UDebug.Log($"[EV] NPC exit: {evacuated}/{line.Count} following colleagues evacuated.");
        }

        private void DespawnAll()
        {
            for (int i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] != null)
                    Destroy(spawned[i].gameObject);
            }
            spawned.Clear();
        }

        private void Respawn()
        {
            DespawnAll();
            line.Clear();
            if (npcPrefab == null || director == null || director.Config == null)
            {
                UDebug.LogError("[EV] NpcRegistry: prefab, Director or config not assigned.", this);
                return;
            }

            SpawnRoom(1, director.Config.NpcCountRoom1);
            SpawnRoom(2, director.Config.NpcCountRoom2);
            director.ReportNpcLayout(LayoutSignature());
            UDebug.Log($"[EV] NPC spawned {spawned.Count}/{director.Config.NpcCountTotal}: R1 {director.Config.NpcCountRoom1}, R2 {director.Config.NpcCountRoom2}, seed {director.Seed}, layout [{director.ReplayNpcSlots}].");
        }

        private void SpawnRoom(int room, int count)
        {
            var indices = new List<int>();
            for (int i = 1; i <= slotsPerRoom; i++)
                indices.Add(i);
            for (int i = indices.Count - 1; i > 0; i--)
            {
                int swap = director.Rng.Next(i + 1);
                int temp = indices[i];
                indices[i] = indices[swap];
                indices[swap] = temp;
            }

            for (int i = 0; i < count && i < indices.Count; i++)
            {
                string markerName = slotPrefix + room.ToString() + "_" + indices[i].ToString("00");
                Transform marker = markersRoot != null ? markersRoot.Find(markerName) : null;
                if (marker == null)
                {
                    GameObject found = GameObject.Find(markerName);
                    if (found != null)
                        marker = found.transform;
                }
                if (marker == null)
                {
                    UDebug.LogWarning($"[EV] NPC marker '{markerName}' not found.", this);
                    continue;
                }

                EvacNpc npc = Instantiate(npcPrefab, marker.position, marker.rotation);
                npc.Configure(this, room, $"EvacNpc_R{room}_{indices[i]:00}");
                spawned.Add(npc);
            }
        }
    }
}
