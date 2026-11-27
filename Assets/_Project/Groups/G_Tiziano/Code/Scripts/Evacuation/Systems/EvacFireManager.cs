using System.Collections.Generic;
using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.Evacuation.Core;
using SI81.Tiziano.Evacuation.Data;
using UDebug = UnityEngine.Debug;

namespace SI81.Tiziano.Evacuation.Systems
{
    /// <summary>
    /// Attiva 5 focolai su 10 in Crisis, scelti col seed del Director e mai bloccanti
    /// sulle rotte ROUTE_*. In Guided/Operational non accende nulla. IResettable.
    /// </summary>
    public sealed class EvacFireManager : MonoBehaviour, IResettable
    {
        private const int MaxAttempts = 50;

        [SerializeField] private EvacuationDirector director;
        [SerializeField] private EvacFireSlot[] slots = new EvacFireSlot[0];
        [SerializeField] private EvacFireLaneValidator laneValidator;
        [SerializeField] private bool showGizmos;

        private readonly List<EvacFireSlot> active = new List<EvacFireSlot>();
        private bool lastReduced;
        private int rejectedCombinations;

        /// <summary>Focolai attivi nella partita corrente.</summary>
        public IReadOnlyList<EvacFireSlot> ActiveFires => active;
        /// <summary>Combinazioni scartate durante l'ultima estrazione.</summary>
        public int RejectedCombinations => rejectedCombinations;
        /// <summary>True se i gizmo di debug sono visibili.</summary>
        public bool ShowGizmos => showGizmos;

        /// <summary>Returns the active fire slot ids in draw order (stable signature of the current seed).</summary>
        public string LayoutSignature()
        {
            var parts = new string[active.Count];
            for (int i = 0; i < active.Count; i++)
                parts[i] = active[i] != null ? active[i].SlotId : "null";
            return string.Join(",", parts);
        }

        private EvacuationConfig Config => director != null ? director.Config : null;

        private void Awake()
        {
            if (director == null)
                director = FindFirstObjectByType<EvacuationDirector>();
            if (slots == null || slots.Length == 0)
            {
                EvacFireSlot[] found = FindObjectsByType<EvacFireSlot>(FindObjectsSortMode.None);
                System.Array.Sort(found, (a, b) => string.CompareOrdinal(a.SlotId, b.SlotId));
                slots = found;
            }
            if (laneValidator == null)
                laneValidator = FindFirstObjectByType<EvacFireLaneValidator>();
        }

        private void OnEnable()
        {
            if (director != null)
                director.RegisterResettable(this);
        }

        private void OnDisable()
        {
            if (director != null)
                director.UnregisterResettable(this);
        }

        private void Update()
        {
            if (director == null || active.Count == 0 || director.ReducedEffects == lastReduced)
                return;
            lastReduced = director.ReducedEffects;
            ApplyVisuals();
        }

        /// <summary>IResettable: spegne tutto e rieffe l'estrazione solo in Crisis.</summary>
        public void ResetState()
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null)
                    slots[i].ResetState();
            active.Clear();
            rejectedCombinations = 0;
            lastReduced = director != null && director.ReducedEffects;

            if (Config == null || director.Difficulty != EvacDifficulty.Crisis || !Config.FireEnabled)
                return;

            int count = Mathf.Clamp(Config.ActiveFireCount, 0, slots.Length);
            if (count <= 0)
                return;

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                Pick(count, director.Rng);
                if (laneValidator == null || laneValidator.IsLaneFree(active, Config.RouteLaneWidth))
                {
                    ApplyVisuals();
                    director.ReportFireLayout(LayoutSignature());
                    UDebug.Log($"[EV] Fire: {active.Count}/{slots.Length} active, rejected {rejectedCombinations} (seed {director.Seed}), layout [{director.ReplayFireSlots}].");
                    return;
                }
                rejectedCombinations++;
                Clear();
            }

            Pick(count, director.Rng);
            ApplyVisuals();
            director.ReportFireLayout(LayoutSignature());
            UDebug.LogWarning($"[EV] Fire: nessuna combinazione libera in {MaxAttempts} tentativi (seed {director.Seed}); controlla gli FIRESLOT.");
        }

        /// <summary>Forza un contatto fuoco per i test (passa dal Director).</summary>
        public void ForceContact()
        {
            if (director != null)
                director.ReportFireContact();
        }

        /// <summary>Stampa gli slot attivi e combina le ultime estrazioni.</summary>
        public void PrintStatus()
        {
            var ids = new List<string>();
            for (int i = 0; i < active.Count; i++)
                ids.Add(active[i] != null ? active[i].SlotId : "null");
            UDebug.Log($"[EV] Fire: {difficulty()} active=[{string.Join(", ", ids.ToArray())}] rejected={rejectedCombinations} reduced={(director != null && director.ReducedEffects)}");
        }

        /// <summary>Valida N seed: restituisce quante estrazioni sono state scartate.</summary>
        public int ValidateSeeds(int seeds)
        {
            if (Config == null || slots.Length == 0)
                return 0;
            int rejected = 0;
            int count = Mathf.Clamp(Config.ActiveFireCount, 0, slots.Length);
            for (int seed = 1; seed <= Mathf.Max(0, seeds); seed++)
            {
                var rng = new System.Random(seed);
                Pick(count, rng);
                bool valid = laneValidator == null || laneValidator.IsLaneFree(active, Config.RouteLaneWidth);
                if (!valid)
                    rejected++;
                Clear();
            }
            UDebug.Log($"[EV] Fire lane: {seeds - rejected}/{seeds} seed validi, {rejected} scartati.");
            return rejected;
        }

        /// <summary>Attiva/disattiva i gizmo degli slot attivi.</summary>
        public void ToggleGizmos()
        {
            showGizmos = !showGizmos;
            UDebug.Log($"[EV] Fire gizmos {showGizmos}.");
        }

        private string difficulty() => director != null ? director.Difficulty.ToString() : "none";

        private void Pick(int count, System.Random rng)
        {
            Clear();
            var available = new List<int>();
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null)
                    available.Add(i);
            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int index = rng.Next(available.Count);
                active.Add(slots[available[index]]);
                available.RemoveAt(index);
            }
            for (int i = 0; i < active.Count; i++)
                if (active[i] != null)
                    active[i].SetActive(true, 1f, director != null && director.ReducedEffects);
        }

        private void ApplyVisuals()
        {
            for (int i = 0; i < active.Count; i++)
                if (active[i] != null)
                    active[i].SetActive(true, 1f, lastReduced);
        }

        private void Clear()
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null)
                    slots[i].SetActive(false, 0f, false);
            active.Clear();
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || active == null)
                return;
            Gizmos.color = new Color(1f, 0.45f, 0f, 0.8f);
            for (int i = 0; i < active.Count; i++)
                if (active[i] != null)
                    Gizmos.DrawWireSphere(active[i].ContactBounds.center, 0.6f);
        }
    }
}
