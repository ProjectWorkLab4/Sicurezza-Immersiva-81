using System;
using System.Collections.Generic;
using UnityEngine;
using SI81.Tiziano.ChemicalAlert.Data;
using SI81.Tiziano.ChemicalAlert.Interaction;

namespace SI81.Tiziano.ChemicalAlert.Systems
{
    /// <summary>
    /// Seeds each scenario replay, chooses its substance, validates the fixed three-item PPE cabinet and schedules the second spill.
    /// </summary>
    public sealed class ScenarioReplayController : MonoBehaviour
    {
        private const int VENTILATION_SEED_SALT = 0x4A31;

        [SerializeField] private ChemicalAlertDirector director;
        [SerializeField] private VentilationEscalation ventilationEscalation;
        [SerializeField] private SdsItem sdsItem;
        [SerializeField] private Puddle puddle;
        [SerializeField] private PPEItemObject[] cabinetItems = new PPEItemObject[0];
        [SerializeField] private AudioSource secondSpillAudioSource;

        private System.Random random;
        private int currentSeed;
        private int fixedSeed;
        private int lastRandomVariantIndex = -1;
        private bool fixedSeedEnabled;
        private bool replayPrepared;
        private bool actionTimerRunning;
        private bool secondSpillTriggered;
        private float actionElapsedSeconds;
        private float secondSpillAtSeconds;
        private float secondSpillGrowthMultiplier = 2f;
        private SubstanceVariant currentVariant;

        /// <summary>Gets the seed used by the current replay.</summary>
        public int CurrentSeed => currentSeed;
        /// <summary>Gets whether the current replay seed is fixed by a debug command.</summary>
        public bool IsSeedFixed => fixedSeedEnabled;
        /// <summary>Gets the current randomized substance variant.</summary>
        public SubstanceVariant CurrentVariant => currentVariant;
        /// <summary>Gets the scheduled second-spill time after Action begins.</summary>
        public float SecondSpillAtSeconds => secondSpillAtSeconds;
        /// <summary>Gets whether the second-spill event has fired this replay.</summary>
        public bool SecondSpillTriggered => secondSpillTriggered;

        private void Awake()
        {
            if (director == null)
                director = GetComponent<ChemicalAlertDirector>();
            if (ventilationEscalation == null)
                ventilationEscalation = GetComponent<VentilationEscalation>();
        }

        private void OnEnable()
        {
            if (director != null)
                director.PhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            if (director != null)
                director.PhaseChanged -= HandlePhaseChanged;
        }

        private void Start()
        {
            if (!replayPrepared)
                BeginReplay();
            if (director != null && director.CurrentPhase == ChemicalAlertDirector.Phase.Action)
                HandlePhaseChanged(ChemicalAlertDirector.Phase.Action);
        }

        private void Update()
        {
            if (!actionTimerRunning || secondSpillTriggered ||
                director == null || director.CurrentPhase != ChemicalAlertDirector.Phase.Action ||
                director.Outcome.HasValue)
                return;

            AdvanceDebugTime(Time.deltaTime);
        }

        /// <summary>Locks the seed and restarts the scenario so the replay can be reproduced.</summary>
        public void SetFixedSeed(int seed)
        {
            fixedSeed = seed;
            fixedSeedEnabled = true;
            UnityEngine.Debug.Log($"[CA] Replay seed fixed: {fixedSeed}; restarting scenario.");
            if (director != null)
                director.ResetScenario();
            else
                BeginReplay();
        }

        /// <summary>Returns to random seeds and starts a fresh replay.</summary>
        public void ClearFixedSeed()
        {
            fixedSeedEnabled = false;
            UnityEngine.Debug.Log("[CA] Replay seed unlocked; starting a random replay.");
            if (director != null)
                director.ResetScenario();
            else
                BeginReplay();
        }

        /// <summary>Advances the Action replay timer for fast debug verification.</summary>
        public void AdvanceDebugTime(float seconds)
        {
            if (!actionTimerRunning || secondSpillTriggered || seconds <= 0f ||
                director == null || director.CurrentPhase != ChemicalAlertDirector.Phase.Action)
                return;

            actionElapsedSeconds += seconds;
            if (actionElapsedSeconds >= secondSpillAtSeconds)
                TriggerSecondSpill();
        }

        /// <summary>Prints current replay seed, variant, cabinet consistency and event schedule.</summary>
        public void PrintState(Action<string> logger)
        {
            if (logger == null)
                return;

            logger($"[CA] Replay state: seed={currentSeed}; fixed={fixedSeedEnabled}; variant={(currentVariant == null ? "None" : currentVariant.VariantId)}; secondSpill={secondSpillTriggered}; scheduledAt={secondSpillAtSeconds:0.00}s; elapsed={actionElapsedSeconds:0.00}s");
        }

        private void HandlePhaseChanged(ChemicalAlertDirector.Phase phase)
        {
            if (phase == ChemicalAlertDirector.Phase.Briefing)
            {
                BeginReplay();
                return;
            }

            if (phase == ChemicalAlertDirector.Phase.Action)
            {
                actionElapsedSeconds = 0f;
                secondSpillTriggered = false;
                actionTimerRunning = replayPrepared &&
                    director != null &&
                    director.CurrentConfig != null &&
                    director.CurrentConfig.SecondSpillEnabled;
                UnityEngine.Debug.Log($"[CA] SecondSpill scheduled: enabled={actionTimerRunning}; at={secondSpillAtSeconds:0.00}s; seed={currentSeed}.");
            }
            else
            {
                actionTimerRunning = false;
            }
        }

        private void BeginReplay()
        {
            ChemicalAlertConfig config = director == null ? null : director.CurrentConfig;
            if (config == null)
            {
                UnityEngine.Debug.LogError("[CA] ReplayController: ChemicalAlertConfig is missing.", this);
                return;
            }

            currentSeed = fixedSeedEnabled ? fixedSeed : GenerateSeed();
            random = new System.Random(currentSeed);
            if (ventilationEscalation != null)
                ventilationEscalation.SetRandomSeed(currentSeed ^ VENTILATION_SEED_SALT);

            SubstanceVariant[] variants = config.SubstanceVariants;
            if (!TryChooseVariant(variants, out int variantIndex))
            {
                UnityEngine.Debug.LogError("[CA] ReplayController: no valid SubstanceVariant configured.", this);
                return;
            }

            currentVariant = variants[variantIndex];
            if (!fixedSeedEnabled)
                lastRandomVariantIndex = variantIndex;

            if (sdsItem == null || puddle == null)
            {
                UnityEngine.Debug.LogError("[CA] ReplayController: SDS or Puddle reference is missing.", this);
                return;
            }

            sdsItem.SetVariant(currentVariant);
            puddle.SetVariant(currentVariant);
            ValidateCabinet(config.PpeShufflePool, currentVariant);
            director.ReapplyDifficultyBehavior();

            secondSpillTriggered = false;
            actionTimerRunning = false;
            actionElapsedSeconds = 0f;
            secondSpillGrowthMultiplier = config.SecondSpillGrowthMultiplier;
            secondSpillAtSeconds = ChooseSecondSpillTime(config);
            replayPrepared = true;
            UnityEngine.Debug.Log($"[CA] ReplayStart: seed={currentSeed}; fixed={fixedSeedEnabled}; variant={currentVariant.VariantId}; SDS={currentVariant.SdsTitleKey}; requiredPPE={string.Join(",", GetRequiredPpeIds(currentVariant))}; secondSpillWindow={config.SecondSpillWindowStartSeconds:0.00}-{config.SecondSpillWindowEndSeconds:0.00}s; scheduledAt={secondSpillAtSeconds:0.00}s.");
        }

        private int GenerateSeed()
        {
            return Guid.NewGuid().GetHashCode();
        }

        private bool TryChooseVariant(SubstanceVariant[] variants, out int variantIndex)
        {
            variantIndex = -1;
            if (variants == null || variants.Length == 0)
                return false;

            List<int> validIndices = new List<int>();
            for (int i = 0; i < variants.Length; i++)
            {
                if (variants[i] != null)
                    validIndices.Add(i);
            }
            if (validIndices.Count == 0)
                return false;

            int choice = random.Next(validIndices.Count);
            if (!fixedSeedEnabled && validIndices.Count > 1 &&
                validIndices[choice] == lastRandomVariantIndex)
                choice = (choice + random.Next(1, validIndices.Count)) % validIndices.Count;

            variantIndex = validIndices[choice];
            return true;
        }

        private void ValidateCabinet(PPEItem[] configuredPool, SubstanceVariant variant)
        {
            if (configuredPool == null || cabinetItems == null ||
                configuredPool.Length != 3 || cabinetItems.Length != 3)
            {
                UnityEngine.Debug.LogError($"[CA] ReplayController: expected 3 PPE types and 3 physical copies; pool={(configuredPool == null ? 0 : configuredPool.Length)}, copies={(cabinetItems == null ? 0 : cabinetItems.Length)}.", this);
                return;
            }

            Dictionary<PPEItem, int> cabinetCounts = new Dictionary<PPEItem, int>();
            foreach (PPEItem item in configuredPool)
            {
                if (item == null)
                {
                    UnityEngine.Debug.LogError("[CA] ReplayController: PPE pool contains a null item.", this);
                    return;
                }
                cabinetCounts[item] = 0;
            }

            foreach (PPEItemObject copy in cabinetItems)
            {
                if (copy == null || copy.ItemData == null || !cabinetCounts.ContainsKey(copy.ItemData))
                {
                    UnityEngine.Debug.LogError("[CA] ReplayController: cabinet copy is missing or has a PPE type outside the 3-item pool.", this);
                    return;
                }
                cabinetCounts[copy.ItemData]++;
            }

            foreach (KeyValuePair<PPEItem, int> count in cabinetCounts)
            {
                if (count.Value != 1)
                {
                    UnityEngine.Debug.LogError($"[CA] ReplayController: expected one cabinet copy of '{count.Key.ItemId}', found {count.Value}.", this);
                    return;
                }
            }

            foreach (PPEItem required in variant.RequiredPpe)
            {
                if (required != null && !cabinetCounts.ContainsKey(required))
                    UnityEngine.Debug.LogError($"[CA] ReplayController: required PPE '{required.ItemId}' is missing from the 3-item cabinet.", this);
            }

            UnityEngine.Debug.Log("[CA] PPE cabinet validated: 3 required types, 1 interactive copy each on the middle shelf; no decoys.");
        }

        private float ChooseSecondSpillTime(ChemicalAlertConfig config)
        {
            float minimum = Mathf.Max(0f, config.SecondSpillWindowStartSeconds);
            float maximum = Mathf.Max(minimum, config.SecondSpillWindowEndSeconds);
            return minimum + (float)random.NextDouble() * (maximum - minimum);
        }

        private void TriggerSecondSpill()
        {
            if (director == null || !director.RegisterSecondSpill())
            {
                actionTimerRunning = false;
                return;
            }

            secondSpillTriggered = true;
            actionTimerRunning = false;
            bool expanded = puddle != null && puddle.TriggerSecondSpill(secondSpillGrowthMultiplier);
            AudioClip signal = director.CurrentConfig == null ? null : director.CurrentConfig.SecondSpillAudio;
            if (signal != null && secondSpillAudioSource != null)
                secondSpillAudioSource.PlayOneShot(signal);

            UnityEngine.Debug.Log($"[CA] SecondSpill: seed={currentSeed}; elapsed={actionElapsedSeconds:0.00}s; puddleExpanded={expanded}; variant={(currentVariant == null ? "None" : currentVariant.VariantId)}.");
        }

        private static string[] GetRequiredPpeIds(SubstanceVariant variant)
        {
            if (variant == null || variant.RequiredPpe == null)
                return new string[0];

            List<string> ids = new List<string>();
            foreach (PPEItem item in variant.RequiredPpe)
            {
                if (item != null)
                    ids.Add(item.ItemId);
            }
            return ids.ToArray();
        }
    }
}
