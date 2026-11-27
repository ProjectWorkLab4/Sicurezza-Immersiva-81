using System.Collections.Generic;
using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Core;
using SI81.Tiziano.ChemicalAlert.Interaction;
using SI81.Tiziano.Evacuation.Core;
using SI81.Tiziano.Evacuation.Interaction;

namespace SI81.Tiziano.Evacuation.Systems
{
    /// <summary>
    /// Guided-only #E69F00 pulse on plans, idle NPCs, the emergency door and its exit sign.
    /// Interactables reuse the Chemical Alert highlight; the sign uses a MaterialPropertyBlock.
    /// </summary>
    public sealed class EvacGuidedHighlights : MonoBehaviour, IResettable
    {
        private static readonly Color HighlightColor = new Color32(230, 159, 0, 255);
        private const float PulseHz = 0.8f;

        [SerializeField] private EvacuationDirector director;
        [SerializeField] private EvacuationPlanInspectable[] plans = new EvacuationPlanInspectable[0];
        [SerializeField] private DoorInteractable[] emergencyDoors = new DoorInteractable[0];
        [SerializeField] private Renderer[] exitSignRenderers = new Renderer[0];
        [SerializeField] private NpcRegistry npcRegistry;

        private readonly Dictionary<Renderer, MaterialPropertyBlock> signBlocks =
            new Dictionary<Renderer, MaterialPropertyBlock>();
        private bool signHighlighted;

        /// <summary>Gets how many objects are currently highlighted.</summary>
        public int HighlightedCount { get; private set; }

        private void Awake()
        {
            if (director == null)
                director = FindFirstObjectByType<EvacuationDirector>();
            if (npcRegistry == null)
                npcRegistry = FindFirstObjectByType<NpcRegistry>();
            if (plans == null || plans.Length == 0)
                plans = FindObjectsByType<EvacuationPlanInspectable>(FindObjectsSortMode.None);
            if (emergencyDoors == null || emergencyDoors.Length == 0)
            {
                GameObject door = GameObject.Find("Door_StairsEmergency");
                if (door != null)
                    emergencyDoors = new[] { door.GetComponent<DoorInteractable>() };
            }
            if (exitSignRenderers == null || exitSignRenderers.Length == 0)
            {
                GameObject sign = GameObject.Find("ExitSign_StairsEmergency");
                if (sign != null)
                    exitSignRenderers = sign.GetComponentsInChildren<Renderer>(true);
            }
        }

        private void OnEnable()
        {
            if (director != null)
                director.RegisterResettable(this);
            ApplyHighlights();
        }

        private void OnDisable()
        {
            ClearSignHighlight();
            if (director != null)
                director.UnregisterResettable(this);
        }

        private void Update()
        {
            ApplyHighlights();
        }

        /// <summary>Refreshes every highlight from the current Director state.</summary>
        public void ApplyHighlights()
        {
            bool enabled = director != null && director.HighlightsEnabled && director.Phase == EvacPhase.Action;
            int count = 0;

            if (plans != null)
            {
                for (int i = 0; i < plans.Length; i++)
                {
                    bool on = enabled && plans[i] != null && !plans[i].IsInspected;
                    SetHighlight(plans[i] != null ? plans[i].Interactable : null, on);
                    if (on)
                        count++;
                }
            }

            if (npcRegistry != null)
            {
                for (int i = 0; i < npcRegistry.Spawned.Count; i++)
                {
                    EvacNpc npc = npcRegistry.Spawned[i];
                    bool on = enabled && npc != null && npc.State == EvacNpcState.Idle;
                    SetHighlight(npc != null ? npc.Interactable : null, on);
                    if (on)
                        count++;
                }
            }

            if (emergencyDoors != null)
            {
                for (int i = 0; i < emergencyDoors.Length; i++)
                {
                    bool on = enabled && emergencyDoors[i] != null && !emergencyDoors[i].IsOpen;
                    SetHighlight(emergencyDoors[i] != null ? emergencyDoors[i].Interactable : null, on);
                    if (on)
                        count++;
                }
            }

            bool signOn = enabled && exitSignRenderers != null && exitSignRenderers.Length > 0;
            ApplySignHighlight(signOn);
            if (signOn)
                count += exitSignRenderers.Length;
            HighlightedCount = count;
        }

        /// <summary>IResettable: clears the sign block and reapplies highlights for the current level.</summary>
        public void ResetState()
        {
            ClearSignHighlight();
            ApplyHighlights();
        }

        private static void SetHighlight(ScenarioInteractable target, bool on)
        {
            if (target != null)
                target.SetGuidedHighlight(on);
        }

        private void ApplySignHighlight(bool on)
        {
            if (!on)
            {
                ClearSignHighlight();
                return;
            }

            if (!signHighlighted)
            {
                signHighlighted = true;
                signBlocks.Clear();
                for (int i = 0; i < exitSignRenderers.Length; i++)
                {
                    Renderer target = exitSignRenderers[i];
                    if (target == null)
                        continue;
                    var original = new MaterialPropertyBlock();
                    target.GetPropertyBlock(original);
                    signBlocks[target] = original;
                }
            }

            Color emission = HighlightColor * (0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * PulseHz * Mathf.PI * 2f));
            foreach (KeyValuePair<Renderer, MaterialPropertyBlock> item in signBlocks)
            {
                if (item.Key == null)
                    continue;
                var block = new MaterialPropertyBlock();
                block.SetColor("_EmissionColor", emission);
                item.Key.SetPropertyBlock(block);
            }
        }

        private void ClearSignHighlight()
        {
            if (!signHighlighted)
                return;
            foreach (KeyValuePair<Renderer, MaterialPropertyBlock> item in signBlocks)
            {
                if (item.Key != null)
                    item.Key.SetPropertyBlock(item.Value);
            }
            signBlocks.Clear();
            signHighlighted = false;
        }
    }
}
